using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using distels.DTO;
using AutoMapper;
using System.Text.Json;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using distels.Repositories;
using distels.Services;

namespace distels.Controllers
{
   

    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TarjetasController> _logger;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
         private readonly IPdfService _pdfService;
        public TarjetasController(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<TarjetasController> logger,
            IEmailService emailService,
            IWebHostEnvironment env,
            IPdfService pdfService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
            _env = env;
            _pdfService = pdfService; // Agregar esta línea

        }

        // ============================================================
        // GENERACIÓN AUTOMÁTICA DE TARJETAS CON CUENTAS
        // ============================================================

        [HttpPost("generar-automatico")]
        public async Task<ActionResult> GenerarTarjetasAutomatico([FromBody] GenerarTarjetasAutomaticoDTO request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation($"Iniciando generación automática: Plan {request.IdPlan}, Cantidad: {request.Cantidad}, Vendedor: {request.IdVendedor}");

                // 1. VALIDACIONES BÁSICAS
                if (request.Cantidad < 1 || request.Cantidad > 1000)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La cantidad debe estar entre 1 y 1000"
                    });
                }

                // 2. OBTENER PLAN Y SERVICIO
                var plan = await _context.Planes
                    .Include(p => p.Servicio)
                    .FirstOrDefaultAsync(p => p.IdPlan == request.IdPlan);

                if (plan == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Plan no encontrado"
                    });
                }

                var servicio = plan.Servicio;

                // 3. VALIDAR VENDEDOR (si se proporciona)
                Cliente? vendedor = null;
                if (request.IdVendedor.HasValue && request.IdVendedor.Value > 0)
                {
                    vendedor = await _context.Clientes
                        .FirstOrDefaultAsync(c => c.Id == request.IdVendedor.Value);

                    if (vendedor == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Vendedor no encontrado"
                        });
                    }

                    _logger.LogInformation($"Asignando tarjetas al vendedor: {vendedor.Nombre} (ID: {vendedor.Id})");
                }

                // 4. DETERMINAR CONFIGURACIÓN BASADA EN max_perfiles
                int perfilesPorCuenta;
                bool generarPIN;
                string tipoConfiguracion;

                if (servicio.MaxPerfiles == 0)
                {
                    // NETFLIX STYLE: 1 cuenta, 1 perfil principal, SIN PIN
                    perfilesPorCuenta = 1;
                    generarPIN = false;
                    tipoConfiguracion = "NETFLIX-STYLE (1 cuenta, perfil principal, sin PIN)";
                }
                else
                {
                    // SERVICIOS CON MÚLTIPLES PERFILES: 1 cuenta, N perfiles, CON PIN
                    perfilesPorCuenta = servicio.MaxPerfiles;
                    generarPIN = true;
                    tipoConfiguracion = $"1 cuenta = {servicio.MaxPerfiles} perfiles (con PIN)";
                }

                _logger.LogInformation($"Servicio: {servicio.Nombre}, Config: {tipoConfiguracion}, Vendedor asignado: {(vendedor != null ? "Sí" : "No")}");

                // 5. GENERAR LOTE ÚNICO
                string lote = string.IsNullOrEmpty(request.PrefijoLote)
                    ? $"{servicio.Codigo}-{DateTime.Now:yyyyMMddHHmmss}"
                    : request.PrefijoLote;

                var tarjetasGeneradas = new List<Tarjeta>();
                var todasCuentasCreadas = new List<Cuenta>();
                var todasPerfilesCreados = new List<Perfil>();

                // 6. GENERAR CADA TARJETA
                for (int i = 1; i <= request.Cantidad; i++)
                {
                    try
                    {
                        // 6.1 CREAR CUENTA AUTOMÁTICAMENTE
                        string usuarioBase = servicio.Codigo.ToLower();
                        string fecha = DateTime.Now.ToString("yyMMdd");
                        string randomNum = new Random().Next(1000, 9999).ToString();

                        // Formato de usuario según el tipo de servicio
                        string usuario = servicio.MaxPerfiles == 0
                            ? $"{usuarioBase}.{fecha}{randomNum}@gmail.com"  // Netflix style
                            : $"{usuarioBase}{fecha}{randomNum}@cuenta.com"; // Otros servicios

                        string contrasena = GenerarContrasenaAleatoria(8);

                        var cuenta = new Cuenta
                        {
                            IdServicio = servicio.IdServicio,
                            Usuario = usuario,
                            Contrasena = contrasena,
                            Estado = "OCUPADA",
                            FechaCreacion = DateTime.Now
                        };

                        // Guardar cuenta primero para obtener ID
                        _context.Cuentas.Add(cuenta);
                        await _context.SaveChangesAsync();

                        // 6.2 CREAR PERFILES SEGÚN CONFIGURACIÓN
                        var perfilesCuenta = new List<Perfil>();

                        for (int p = 1; p <= perfilesPorCuenta; p++)
                        {
                            string nombrePerfil = servicio.MaxPerfiles == 0
                                ? "Perfil Principal"
                                : $"Perfil {p}";

                            string pin = generarPIN ? GenerarPIN(4) : null;

                            string estadoPerfil;
                            if (perfilesPorCuenta == 1)
                            {
                                estadoPerfil = "OCUPADO";
                            }
                            else
                            {
                                estadoPerfil = (p == 1) ? "OCUPADO" : "DISPONIBLE";
                            }

                            var perfil = new Perfil
                            {
                                IdCuenta = cuenta.IdCuenta,
                                Nombre = nombrePerfil,
                                Pin = pin,
                                Estado = estadoPerfil,
                                FechaCreacion = DateTime.Now
                            };

                            perfilesCuenta.Add(perfil);
                            _context.Perfiles.Add(perfil);
                        }

                        await _context.SaveChangesAsync();

                        // 6.3 CREAR TARJETA (asociada al primer perfil)
                        string codigo = GenerarCodigoUnico(15);
                        string serie = $"{servicio.Codigo}-{DateTime.Now:yyyyMMdd}-{i.ToString("D4")}";

                        var tarjeta = new Tarjeta
                        {
                            Codigo = codigo,
                            Serie = serie,
                            Lote = lote,
                            IdPlan = plan.IdPlan,
                            IdPerfil = perfilesCuenta.First().IdPerfil,
                            IdVendedor = vendedor?.Id, // ASIGNAR VENDEDOR SI EXISTE
                            FechaCreacion = DateTime.Now,
                            FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(plan.DuracionDias)),
                            Estado = "ASIGNADA"
                        };

                        _context.Tarjetas.Add(tarjeta);
                        await _context.SaveChangesAsync();

                        // Cargar relaciones para la respuesta
                        tarjeta.Perfil = perfilesCuenta.First();
                        tarjeta.Perfil.Cuenta = cuenta;
                        if (vendedor != null)
                        {
                            tarjeta.Vendedor = vendedor;
                        }

                        todasCuentasCreadas.Add(cuenta);
                        todasPerfilesCreados.AddRange(perfilesCuenta);
                        tarjetasGeneradas.Add(tarjeta);

                        _logger.LogDebug($"Generada tarjeta {i}/{request.Cantidad}: {codigo}, Vendedor: {(vendedor?.Nombre ?? "Ninguno")}");

                        if (i % 10 == 0) await Task.Delay(10);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al generar tarjeta {i}/{request.Cantidad}");
                    }
                }

                // 7. CONFIRMAR TRANSACCIÓN
                await transaction.CommitAsync();

                _logger.LogInformation($"✅ Generación completada: {tarjetasGeneradas.Count} tarjetas en lote {lote}, Vendedor: {(vendedor?.Nombre ?? "Sin asignar")}");

                // 8. PREPARAR RESPUESTA
                var detalles = tarjetasGeneradas.Select((t, index) => new
                {
                    Numero = index + 1,
                    Codigo = t.Codigo,
                    Serie = t.Serie,
                    Lote = t.Lote,
                    Usuario = t.Perfil?.Cuenta?.Usuario,
                    Contrasena = t.Perfil?.Cuenta?.Contrasena,
                    Perfil = t.Perfil?.Nombre,
                    Pin = t.Perfil?.Pin,
                    Estado = t.Estado,
                    Vendedor = t.Vendedor != null ? new
                    {
                        Id = t.Vendedor.Id,
                        Nombre = t.Vendedor.Nombre,
                        Tipo = t.Vendedor.TipoCliente
                    } : null,
                    FechaVencimiento = t.FechaVencimiento?.ToString("yyyy-MM-dd"),
                    FechaCreacion = t.FechaCreacion.ToString()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = $"✅ ¡Generación automática exitosa!",
                    tarjetasGeneradas = tarjetasGeneradas.Count,
                    lote = lote,
                    servicio = new
                    {
                        id = servicio.IdServicio,
                        nombre = servicio.Nombre,
                        codigo = servicio.Codigo,
                        maxPerfiles = servicio.MaxPerfiles,
                        configuracion = tipoConfiguracion,
                        generaPIN = generarPIN
                    },
                    vendedorAsignado = vendedor != null ? new
                    {
                        id = vendedor.Id,
                        nombre = vendedor.Nombre,
                        tipo = vendedor.TipoCliente
                    } : null,
                    plan = new
                    {
                        id = plan.IdPlan,
                        nombre = plan.Nombre,
                        duracionDias = plan.DuracionDias,
                        precioVenta = plan.PrecioVenta,
                        precioCompra = plan.PrecioCompra
                    },
                    cuentasCreadas = todasCuentasCreadas.Count,
                    perfilesCreados = todasPerfilesCreados.Count,
                    detalles = detalles,
                    resumen = new
                    {
                        primeraTarjeta = detalles.FirstOrDefault(),
                        ultimaTarjeta = detalles.LastOrDefault(),
                        rangoCodigos = $"{detalles.FirstOrDefault()?.Codigo} - {detalles.LastOrDefault()?.Codigo}",
                        fechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error en generación automática de tarjetas");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno al generar tarjetas",
                    error = ex.Message,
                    detalles = ex.InnerException?.Message
                });
            }
        }
        [HttpGet("tipo/{tipo}")]
        public async Task<ActionResult> GetClientesPorTipo(string tipo)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.TipoCliente == tipo && c.Estado == "activo")
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = clientes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo clientes tipo {tipo}");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
        // POST: api/Tarjetas/generar-manual
        [HttpPost("generar-manual")]
        public async Task<ActionResult> GenerarTarjetasManual([FromBody] GenerarTarjetasManualDTO request)
        {
            try
            {
                if (request.Cuentas == null || !request.Cuentas.Any())
                {
                    return BadRequest(new { message = "Se requieren datos de cuentas para generación manual" });
                }

                var plan = await _context.Planes
                    .Include(p => p.Servicio)
                    .FirstOrDefaultAsync(p => p.IdPlan == request.IdPlan);

                if (plan == null)
                {
                    return NotFound(new { message = "Plan no encontrado" });
                }

                var servicio = plan.Servicio;
                string lote = request.PrefijoLote ?? $"{servicio.Codigo}-MANUAL-{DateTime.Now:yyyyMMddHHmmss}";

                var resultados = new List<object>();

                foreach (var cuentaData in request.Cuentas)
                {
                    try
                    {
                        // Crear cuenta
                        var cuenta = new Cuenta
                        {
                            IdServicio = servicio.IdServicio,
                            Usuario = cuentaData.Usuario,
                            Contrasena = cuentaData.Contrasena,
                            Estado = "OCUPADA",
                            FechaCreacion = DateTime.Now
                        };

                        _context.Cuentas.Add(cuenta);
                        await _context.SaveChangesAsync();

                        // Crear perfiles
                        int perfilesACrear = Math.Min(servicio.MaxPerfiles, cuentaData.Perfiles ?? 1);
                        var perfiles = new List<Perfil>();

                        for (int i = 1; i <= perfilesACrear; i++)
                        {
                            var perfil = new Perfil
                            {
                                IdCuenta = cuenta.IdCuenta,
                                Nombre = cuentaData.NombrePerfil ?? $"Perfil {i}",
                                Pin = cuentaData.Pin,
                                Estado = i == 1 ? "OCUPADO" : "DISPONIBLE",
                                FechaCreacion = DateTime.Now
                            };
                            perfiles.Add(perfil);
                            _context.Perfiles.Add(perfil);
                        }

                        await _context.SaveChangesAsync();

                        // Crear tarjeta
                        string codigo = GenerarCodigoUnico(15);
                        var tarjeta = new Tarjeta
                        {
                            Codigo = codigo,
                            Serie = $"{servicio.Codigo}-MANUAL-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                            Lote = lote,
                            IdPlan = plan.IdPlan,
                            IdPerfil = perfiles.First().IdPerfil,
                            FechaCreacion = DateTime.Now,
                            FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(plan.DuracionDias)),
                            Estado = "DISPONIBLE"
                           
                        };

                        _context.Tarjetas.Add(tarjeta);
                        await _context.SaveChangesAsync();

                        resultados.Add(new
                        {
                            success = true,
                            cuenta = cuenta.Usuario,
                            tarjeta = codigo,
                            perfil = perfiles.First().Nombre,
                            pin = perfiles.First().Pin
                        });
                    }
                    catch (Exception ex)
                    {
                        resultados.Add(new
                        {
                            success = false,
                            cuenta = cuentaData.Usuario,
                            error = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Generación manual completada",
                    lote = lote,
                    totalProcesadas = request.Cuentas.Count,
                    resultados = resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en generación manual");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error en generación manual",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // ENDPOINTS EXISTENTES (modificados para compatibilidad)
        // ============================================================

        // GET: api/Tarjetas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TarjetaDTO>>> GetTarjetas(
            [FromQuery] string? estado,
            [FromQuery] string? lote,
            [FromQuery] int? idVendedor,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .Include(t => t.Vendedor)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(t => t.Estado == estado);
                }

                if (!string.IsNullOrEmpty(lote))
                {
                    query = query.Where(t => t.Lote.Contains(lote));
                }

                if (idVendedor.HasValue)
                {
                    query = query.Where(t => t.IdVendedor == idVendedor.Value);
                }

                var total = await query.CountAsync();
                var tarjetas = await query
                    .OrderByDescending(t => t.FechaCreacion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var tarjetasDTO = _mapper.Map<List<TarjetaDTO>>(tarjetas);

                return Ok(new
                {
                    success = true,
                    data = tarjetasDTO,
                    pagination = new
                    {
                        page,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling(total / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tarjetas");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
        // GET: api/Tarjetas/estadisticas-completas
        [HttpGet("estadisticas-completas")]
        public async Task<ActionResult> GetEstadisticasCompletas()
        {
            try
            {
                var tarjetas = await _context.Tarjetas
                    .Include(t => t.Plan)
                    .Include(t => t.Vendedor)
                    .Include(t => t.ClienteActivador)
                    .ToListAsync();

                var hoy = DateTime.Now;

                var estadisticas = new
                {
                    // Totales
                    Total = tarjetas.Count,
                    Generadas = tarjetas.Count(t => t.Estado == "GENERADA"),
                    Asignadas = tarjetas.Count(t => t.Estado == "ASIGNADA"),
                    Activadas = tarjetas.Count(t => t.Estado == "ACTIVADA"),
                    Vencidas = tarjetas.Count(t => t.Estado == "VENCIDA" ||
                        (t.FechaVencimiento.HasValue &&
                         t.FechaVencimiento.Value < DateOnly.FromDateTime(hoy))),
                    Utilizadas = tarjetas.Count(t => t.Estado == "UTILIZADA"),

                    // Por vendedor
                    PorVendedor = tarjetas
                        .Where(t => t.Vendedor != null)
                        .GroupBy(t => t.Vendedor)
                        .Select(g => new
                        {
                            VendedorId = g.Key.Id,
                            VendedorNombre = g.Key.Nombre,
                            Total = g.Count(),
                            Activadas = g.Count(t => t.Estado == "ACTIVADA"),
                            Vencidas = g.Count(t => t.FechaVencimiento.HasValue &&
                                t.FechaVencimiento.Value < DateOnly.FromDateTime(hoy))
                        })
                        .ToList(),

                    // Por servicio
                    PorServicio = tarjetas
                        .Where(t => t.Plan?.Servicio != null)
                        .GroupBy(t => t.Plan.Servicio)
                        .Select(g => new
                        {
                            ServicioId = g.Key.IdServicio,
                            ServicioNombre = g.Key.Nombre,
                            Total = g.Count(),
                            PorEstado = g.GroupBy(t => t.Estado)
                                .Select(e => new
                                {
                                    Estado = e.Key,
                                    Cantidad = e.Count()
                                })
                                .ToList()
                        })
                        .ToList(),

                    // Próximas a vencer (7 días)
                    ProximasAVencer = tarjetas
                        .Where(t => t.FechaVencimiento.HasValue &&
                                   t.FechaVencimiento.Value >= DateOnly.FromDateTime(hoy) &&
                                   (t.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - hoy).Days <= 7)
                        .Select(t => new
                        {
                            t.IdTarjeta,
                            t.Codigo,
                            t.Estado,
                            FechaVencimiento = t.FechaVencimiento,
                            DiasRestantes = (t.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - hoy).Days,
                            Vendedor = t.Vendedor != null ? t.Vendedor.Nombre : null
                        })
                        .ToList()
                };

                return Ok(new
                {
                    success = true,
                    data = estadisticas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener estadísticas",
                    error = ex.Message
                });
            }
        }
        // GET: api/Tarjetas/detalladas (endpoint específico para la página de tarjetas)
        [HttpGet("detalladas")]
        public async Task<ActionResult> GetTarjetasDetalladas(
            [FromQuery] string? estado,
            [FromQuery] string? lote,
            [FromQuery] int? idVendedor,
            [FromQuery] int? idServicio,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .Include(t => t.Vendedor)
                    .Include(t => t.ClienteActivador)
                    .AsQueryable();

                // Aplicar filtros
                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(t => t.Estado == estado);
                }

                if (!string.IsNullOrEmpty(lote))
                {
                    query = query.Where(t => t.Lote.Contains(lote));
                }

                if (idVendedor.HasValue)
                {
                    query = query.Where(t => t.IdVendedor == idVendedor.Value);
                }

                if (idServicio.HasValue)
                {
                    query = query.Where(t => t.Plan.IdServicio == idServicio.Value);
                }

                var total = await query.CountAsync();
                var tarjetas = await query
                    .OrderByDescending(t => t.FechaCreacion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Respuesta personalizada para la página de tarjetas
                var response = tarjetas.Select(t => new
                {
                    // Información básica de la tarjeta
                    t.IdTarjeta,
                    t.Codigo,
                    t.Serie,
                    t.Lote,
                    t.Estado,
                    t.FechaCreacion,
                    t.FechaActivacion,
                    t.FechaVencimiento,
                    t.IdVendedor,
                    t.IdClienteActivador,

                    // Información completa del Plan y Servicio
                    Plan = t.Plan != null ? new
                    {
                        t.Plan.IdPlan,
                        t.Plan.Nombre,
                        t.Plan.PrecioCompra,
                        t.Plan.PrecioVenta,
                        t.Plan.DuracionDias,
                        Servicio = t.Plan.Servicio != null ? new
                        {
                            t.Plan.Servicio.IdServicio,
                            t.Plan.Servicio.Nombre,
                            t.Plan.Servicio.Codigo,
                            t.Plan.Servicio.MaxPerfiles
                        } : null
                    } : null,

                    // Credenciales completas (Cuenta + Perfil)
                    Credenciales = t.Perfil != null ? new
                    {
                        // Información de la Cuenta
                        Cuenta = t.Perfil.Cuenta != null ? new
                        {
                            t.Perfil.Cuenta.IdCuenta,
                            t.Perfil.Cuenta.Usuario,
                            t.Perfil.Cuenta.Contrasena,
                            t.Perfil.Cuenta.Estado,
                            t.Perfil.Cuenta.FechaCreacion
                        } : null,

                        // Información del Perfil
                        Perfil = new
                        {
                            t.Perfil.IdPerfil,
                            t.Perfil.Nombre,
                            t.Perfil.Pin,
                            t.Perfil.Estado,
                            t.Perfil.FechaCreacion
                        }
                    } : null,

                    // Información del Vendedor (Cliente VENDEDOR)
                    Vendedor = t.Vendedor != null ? new
                    {
                        t.Vendedor.Id,
                        t.Vendedor.Nombre,
                        t.Vendedor.Usuario,
                        t.Vendedor.Celular,
                        t.Vendedor.Email,
                        t.Vendedor.TipoCliente,
                        t.Vendedor.Estado
                    } : null,

                    // Información del Cliente Final (Cliente FINAL)
                    ClienteFinal = t.ClienteActivador != null ? new
                    {
                        t.ClienteActivador.Id,
                        t.ClienteActivador.Nombre,
                        t.ClienteActivador.Usuario,
                        t.ClienteActivador.Celular,
                        t.ClienteActivador.Email,
                        t.ClienteActivador.TipoCliente,
                        t.ClienteActivador.Estado,
                        t.ClienteActivador.FechaCreacion
                    } : null,

                    // Campos calculados
                    EstaVencida = t.FechaVencimiento.HasValue &&
                                 t.FechaVencimiento.Value < DateOnly.FromDateTime(DateTime.Now),
                    DiasParaVencimiento = t.FechaVencimiento.HasValue ?
                        (t.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days : (int?)null
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Datos detallados de tarjetas",
                    data = response,
                    pagination = new
                    {
                        page,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling(total / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tarjetas detalladas");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }
        // GET: api/Tarjetas/lote/{lote}
        [HttpGet("lote/{lote}")]
        public async Task<ActionResult> GetTarjetasPorLote(string lote)
        {
            try
            {
                var tarjetas = await _context.Tarjetas
                    .Where(t => t.Lote == lote)
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .OrderBy(t => t.FechaCreacion)
                    .ToListAsync();

                if (!tarjetas.Any())
                {
                    return NotFound(new { message = "No se encontraron tarjetas para este lote" });
                }

                var detalles = tarjetas.Select(t => new
                {
                    t.IdTarjeta,
                    t.Codigo,
                    t.Serie,
                    t.Lote,
                    t.Estado,
                    t.FechaCreacion,
                    t.FechaVencimiento,
                    Usuario = t.Perfil?.Cuenta?.Usuario,
                    Contrasena = t.Perfil?.Cuenta?.Contrasena,
                    Perfil = t.Perfil?.Nombre,
                    Pin = t.Perfil?.Pin,
                    Plan = t.Plan?.Nombre,
                    Servicio = t.Plan?.Servicio?.Nombre,
                    Duracion = t.Plan?.DuracionDias
                });

                return Ok(new
                {
                    success = true,
                    data = detalles,
                    total = tarjetas.Count,
                    lote = lote,
                    fechaGeneracion = tarjetas.Min(t => t.FechaCreacion),
                    servicio = tarjetas.First().Plan?.Servicio?.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener tarjetas del lote {lote}");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Tarjetas/lote/{lote}/csv
        [HttpGet("lote/{lote}/csv")]
        public async Task<IActionResult> ExportarLoteCSV(string lote)
        {
            try
            {
                var tarjetas = await _context.Tarjetas
                    .Where(t => t.Lote == lote)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .OrderBy(t => t.FechaCreacion)
                    .ToListAsync();

                if (!tarjetas.Any())
                {
                    return NotFound(new { message = "Lote no encontrado" });
                }

                // Crear CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("N°,Código Tarjeta,Serie,Lote,Usuario,Contraseña,Perfil,PIN,Estado,Fecha Creación,Fecha Vencimiento,Servicio,Plan,Duración (días)");

                int contador = 1;
                foreach (var tarjeta in tarjetas)
                {
                    csv.AppendLine($"\"{contador}\"," +
                                  $"\"{tarjeta.Codigo}\"," +
                                  $"\"{tarjeta.Serie}\"," +
                                  $"\"{tarjeta.Lote}\"," +
                                  $"\"{tarjeta.Perfil?.Cuenta?.Usuario ?? ""}\"," +
                                  $"\"{tarjeta.Perfil?.Cuenta?.Contrasena ?? ""}\"," +
                                  $"\"{tarjeta.Perfil?.Nombre ?? ""}\"," +
                                  $"\"{tarjeta.Perfil?.Pin ?? ""}\"," +
                                  $"\"{tarjeta.Estado}\"," +
                                  $"\"{tarjeta.FechaCreacion:yyyy-MM-dd HH:mm:ss}\"," +
                                  $"\"{tarjeta.FechaVencimiento?.ToString("yyyy-MM-dd") ?? ""}\"," +
                                  $"\"{tarjeta.Plan?.Servicio?.Nombre ?? ""}\"," +
                                  $"\"{tarjeta.Plan?.Nombre ?? ""}\"," +
                                  $"\"{tarjeta.Plan?.DuracionDias ?? 0}\"");
                    contador++;
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"tarjetas_{lote}_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al exportar CSV del lote {lote}");
                return StatusCode(500, new { message = "Error al generar CSV", error = ex.Message });
            }
        }

        // GET: api/Tarjetas/estadisticas-generacion
        [HttpGet("estadisticas-generacion")]
        public async Task<ActionResult> GetEstadisticasGeneracion([FromQuery] string? fechaInicio, [FromQuery] string? fechaFin)
        {
            try
            {
                var query = _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out var inicio))
                {
                    query = query.Where(t => t.FechaCreacion >= inicio);
                }

                if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out var fin))
                {
                    query = query.Where(t => t.FechaCreacion <= fin.AddDays(1));
                }

                var estadisticas = await query
                    .GroupBy(t => new { t.Lote, t.Plan.Servicio.Nombre })
                    .Select(g => new
                    {
                        Lote = g.Key.Lote,
                        Servicio = g.Key.Nombre,
                        TotalTarjetas = g.Count(),
                        FechaGeneracion = g.Min(t => t.FechaCreacion),
                        Estados = g.GroupBy(t => t.Estado)
                            .Select(e => new
                            {
                                Estado = e.Key,
                                Cantidad = e.Count(),
                                Porcentaje = (e.Count() * 100.0) / g.Count()
                            }).ToList()
                    })
                    .OrderByDescending(e => e.FechaGeneracion)
                    .Take(20)
                    .ToListAsync();

                var totalGeneradas = await query.CountAsync();
                var lotesUnicos = await query.Select(t => t.Lote).Distinct().CountAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalGeneradas,
                        lotesUnicos,
                        lotesRecientes = estadisticas,
                        resumenPorEstado = await query
                            .GroupBy(t => t.Estado)
                            .Select(g => new
                            {
                                Estado = g.Key,
                                Cantidad = g.Count()
                            })
                            .ToListAsync()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de generación");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // ============================================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ============================================================

        private string GenerarCodigoUnico(int longitud)
        {
            const string caracteres = "0123456789";
            var random = new Random();

            string codigo;
            bool esUnico = false;
            int intentos = 0;

            do
            {
                codigo = new string(Enumerable.Repeat(caracteres, longitud)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                // Verificar si el código ya existe
                esUnico = !_context.Tarjetas.Any(t => t.Codigo == codigo);
                intentos++;

                if (intentos > 10)
                {
                    // Si después de 10 intentos no encuentra uno único, agregar timestamp
                    codigo = codigo.Substring(0, longitud - 4) + DateTime.Now.ToString("HHmm").Substring(0, 4);
                    esUnico = true;
                }
            } while (!esUnico);

            return codigo;
        }

        private string GenerarContrasenaAleatoria(int longitud)
        {
            const string caracteres = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, longitud)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerarPIN(int longitud)
        {
            const string numeros = "0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(numeros, longitud)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // ============================================================
        // MÉTODOS EXISTENTES (activar, imprimir, etc.)
        // ============================================================

        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<TarjetaDTO>> GetTarjetaPorCodigo(string codigo)
        {
            try
            {
                var tarjeta = await _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .FirstOrDefaultAsync(t => t.Codigo == codigo);

                if (tarjeta == null)
                {
                    return NotFound(new { message = "Código de tarjeta no válido" });
                }

                if (tarjeta.FechaVencimiento.HasValue && tarjeta.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now))
                {
                    tarjeta.Estado = "VENCIDA";
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "La tarjeta ha vencido" });
                }

                if (tarjeta.Estado == "ACTIVADA")
                {
                    return BadRequest(new { message = "La tarjeta ya ha sido activada" });
                }

                var tarjetaDTO = _mapper.Map<TarjetaDTO>(tarjeta);
                return Ok(new { success = true, data = tarjetaDTO });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar tarjeta por código");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPost("activar")]
        public async Task<ActionResult<CredencialesResponseDTO>> ActivarTarjeta([FromBody] ActivarTarjetaDTO activarDTO)
        {
            try
            {
                _logger.LogInformation($"Intentando activar tarjeta con código: {activarDTO.CodigoTarjeta}");

                var tarjeta = await _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .FirstOrDefaultAsync(t => t.Codigo == activarDTO.CodigoTarjeta);

                if (tarjeta == null)
                {
                    return NotFound(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "Código de tarjeta no válido"
                    });
                }

                // Validaciones
                if (tarjeta.Estado == "ACTIVADA")
                {
                    return BadRequest(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "La tarjeta ya ha sido activada"
                    });
                }

                if (tarjeta.Estado == "VENCIDA")
                {
                    return BadRequest(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "La tarjeta ha vencido"
                    });
                }

                if (tarjeta.FechaVencimiento.HasValue && tarjeta.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now))
                {
                    tarjeta.Estado = "VENCIDA";
                    await _context.SaveChangesAsync();
                    return BadRequest(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "La tarjeta ha vencido"
                    });
                }

                // Obtener credenciales
                var cuenta = tarjeta.Perfil.Cuenta;
                var usuario = cuenta.Usuario;
                var contrasena = cuenta.Contrasena;
                var perfil = tarjeta.Perfil.Nombre;
                var pin = tarjeta.Perfil.Pin;
                var servicio = tarjeta.Plan.Servicio.Nombre;

                // Crear registro de activación
                var activacion = new Activacion
                {
                    IdTarjeta = tarjeta.IdTarjeta,
                    UsuarioEnviado = usuario,
                    ContrasenaEnviada = contrasena,
                    PerfilEnviado = perfil,
                    PinEnviado = pin,
                    IpActivacion = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Dispositivo = activarDTO.Dispositivo,
                    Navegador = activarDTO.Navegador,
                    MetodoEnvio = activarDTO.MetodoEnvio,
                    NumeroEnvio = activarDTO.MetodoEnvio == "WHATSAPP" || activarDTO.MetodoEnvio == "SMS"
                        ? activarDTO.Celular
                        : null,
                    FechaEnvio = DateTime.Now
                };

                // Enviar credenciales
                bool enviado = false;
                string mensajeEnvio = "";

                switch (activarDTO.MetodoEnvio.ToUpper())
                {
                    case "EMAIL":
                        if (string.IsNullOrEmpty(activarDTO.Email))
                        {
                            return BadRequest(new CredencialesResponseDTO
                            {
                                Success = false,
                                Message = "Se requiere email para este método de envío"
                            });
                        }

                        var asunto = $"Credenciales de {servicio} - Activación #{tarjeta.Codigo}";
                        enviado = await _emailService.EnviarCredencialesAsync(
                            activarDTO.Email,
                            "Cliente",
                            usuario,
                            contrasena,
                            asunto,
                            perfil,
                            pin,
                            servicio,
                            tarjeta.FechaVencimiento
                        );
                        mensajeEnvio = "por email";
                        break;

                    default:
                        // Para otros métodos, simulamos éxito
                        enviado = true;
                        mensajeEnvio = "automáticamente";
                        break;
                }

                if (enviado)
                {
                    // Actualizar estado
                    tarjeta.Estado = "ACTIVADA";
                    tarjeta.FechaActivacion = DateTime.Now;
                    tarjeta.IpActivacion = HttpContext.Connection.RemoteIpAddress?.ToString();

                    activacion.Entregado = true;
                    activacion.FechaConfirmacion = DateTime.Now;

                    _context.Activaciones.Add(activacion);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Tarjeta {tarjeta.Codigo} activada exitosamente.");

                    return Ok(new CredencialesResponseDTO
                    {
                        Success = true,
                        Message = $"Credenciales generadas exitosamente",
                        Usuario = usuario,
                        Contrasena = contrasena,
                        Perfil = perfil,
                        Pin = pin,
                        Servicio = servicio,
                        FechaVencimiento = tarjeta.FechaVencimiento?.ToDateTime(TimeOnly.MinValue)
                    });
                }
                else
                {
                    activacion.Entregado = false;
                    _context.Activaciones.Add(activacion);
                    await _context.SaveChangesAsync();

                    return StatusCode(500, new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "Error al enviar las credenciales"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar tarjeta");
                return StatusCode(500, new CredencialesResponseDTO
                {
                    Success = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                });
            }
        }
        //[HttpGet("imprimir-lote/{lote}")]
        //public async Task<IActionResult> ImprimirLote(string lote)
        //{
        //    try
        //    {
        //        var tarjetas = await _context.Tarjetas
        //            .Where(t => t.Lote == lote)
        //            .Include(t => t.Plan)
        //                .ThenInclude(p => p.Servicio)
        //            .OrderBy(t => t.FechaCreacion)
        //            .ToListAsync();

        //        if (!tarjetas.Any())
        //        {
        //            return NotFound(new { message = "Lote no encontrado" });
        //        }

        //        var pdfBytes = _pdfService.GenerarPdfTarjetas(tarjetas);

        //        return File(pdfBytes, "application/pdf", $"tarjetas_{lote}.pdf");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error generando PDF para lote {lote}");
        //        return StatusCode(500, new { message = "Error generando PDF" });
        //    }
        //}
        // POST: api/Tarjetas/subir-imagen-lote
        [HttpPost("subir-imagen-lote")]
        public async Task<ActionResult> SubirImagenLote([FromForm] IFormFile imagen, [FromForm] string lote)
        {
            try
            {
                if (imagen == null || imagen.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No se envió imagen" });
                }

                // Validar tamaño (máximo 5MB)
                var maxFileSize = 5 * 1024 * 1024;
                if (imagen.Length > maxFileSize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La imagen es demasiado grande. Máximo 5MB"
                    });
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(imagen.FileName).ToLower();

                if (!extensionesPermitidas.Contains(extension))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Formato no soportado. Use JPG, JPEG, PNG o WEBP"
                    });
                }

                // Validar que el lote exista
                var loteExiste = await _context.Tarjetas.AnyAsync(t => t.Lote == lote);
                if (!loteExiste)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El lote especificado no existe"
                    });
                }

                // Crear carpeta si no existe
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "lotes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Eliminar imágenes anteriores del mismo lote
                foreach (var ext in extensionesPermitidas)
                {
                    var oldImage = Path.Combine(uploadsFolder, $"{lote}{ext}");
                    if (System.IO.File.Exists(oldImage))
                    {
                        System.IO.File.Delete(oldImage);
                    }
                }

                // Guardar nueva imagen
                var nombreArchivo = $"{lote}{extension}";
                var filePath = Path.Combine(uploadsFolder, nombreArchivo);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }

                return Ok(new
                {
                    success = true,
                    message = "Imagen subida exitosamente",
                    urlImagen = $"/uploads/lotes/{nombreArchivo}",
                    nombreArchivo = nombreArchivo,
                    lote = lote
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo imagen de lote");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno al subir imagen",
                    error = ex.Message
                });
            }
        }

        // En el TarjetasController, modifica el endpoint:

        [HttpGet("imprimir-con-imagen/{lote}")]
        public async Task<IActionResult> ImprimirConImagen(string lote)
        {
            try
            {
                // Obtener tarjetas del lote
                var tarjetas = await _context.Tarjetas
                    .Where(t => t.Lote == lote)
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .OrderBy(t => t.FechaCreacion)
                    .ToListAsync();

                if (!tarjetas.Any())
                {
                    return NotFound(new { message = "Lote no encontrado" });
                }

                // Buscar imagen del lote
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "lotes");
                string imagenLotePath = null;

                var extensiones = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                foreach (var ext in extensiones)
                {
                    var posibleImagen = Path.Combine(uploadsFolder, $"{lote}{ext}");
                    if (System.IO.File.Exists(posibleImagen))
                    {
                        imagenLotePath = posibleImagen;
                        break;
                    }
                }

                // Generar PDF con imagen en TODAS las tarjetas
                byte[] pdfBytes;

                if (!string.IsNullOrEmpty(imagenLotePath) && System.IO.File.Exists(imagenLotePath))
                {
                    // Generar PDF CON imagen en cada tarjeta
                    pdfBytes = _pdfService.GenerarPdfTarjetasConImagenEnTodas(tarjetas, imagenLotePath);
                }
                else
                {
                    // Generar PDF sin imagen
                    pdfBytes = _pdfService.GenerarPdfTarjetas(tarjetas);
                }

                var fileName = $"tarjetas_{lote}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // Configurar respuesta para descarga
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generando PDF para lote {lote}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generando PDF",
                    error = ex.Message
                });
            }
        }
        // 3. ENDPOINT PARA OBTENER SOLO CREDENCIALES DE UN PERFIL
        [HttpGet("credenciales/{idPerfil}")]
        public async Task<ActionResult> ObtenerCredencialesPorPerfil(int idPerfil)
        {
            try
            {
                _logger.LogInformation($"🔐 Obteniendo credenciales para perfil ID: {idPerfil}");

                var perfil = await _context.Perfiles
                    .Include(p => p.Cuenta)
                    .FirstOrDefaultAsync(p => p.IdPerfil == idPerfil);

                if (perfil == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Perfil no encontrado"
                    });
                }

                if (perfil.Cuenta == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El perfil no tiene cuenta asociada"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        perfil.IdPerfil,
                        perfil.Nombre,
                        perfil.Pin,
                        perfil.Estado,
                        perfil.FechaCreacion,
                        Usuario = perfil.Cuenta.Usuario,
                        Contrasena = perfil.Cuenta.Contrasena,
                        EstadoCuenta = perfil.Cuenta.Estado
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error obteniendo credenciales para perfil {idPerfil}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // 4. ENDPOINT PARA OBTENER CREDENCIALES POR CÓDIGO DE TARJETA (Alternativo)
        [HttpGet("credenciales-por-codigo/{codigo}")]
        public async Task<ActionResult> ObtenerCredencialesPorCodigo(string codigo)
        {
            try
            {
                _logger.LogInformation($"🔐 Obteniendo credenciales para tarjeta: {codigo}");

                var tarjeta = await _context.Tarjetas
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .FirstOrDefaultAsync(t => t.Codigo == codigo);

                if (tarjeta == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tarjeta no encontrada"
                    });
                }

                // Validar que la tarjeta esté activa
                if (tarjeta.Estado != "ACTIVA" && tarjeta.Estado != "ASIGNADA")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"La tarjeta no está activa. Estado actual: {tarjeta.Estado}"
                    });
                }

                // Validar que tenga perfil y cuenta
                if (tarjeta.Perfil?.Cuenta == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La tarjeta no tiene credenciales asignadas"
                    });
                }

                // Validar vencimiento
                if (tarjeta.FechaVencimiento.HasValue &&
                    tarjeta.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "La tarjeta ha vencido"
                    });
                }

                var cuenta = tarjeta.Perfil.Cuenta;
                var perfil = tarjeta.Perfil;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Información de la tarjeta
                        TarjetaId = tarjeta.IdTarjeta,
                        tarjeta.Codigo,
                        tarjeta.Estado,
                        tarjeta.FechaVencimiento,

                        // Información del plan y servicio
                        Plan = tarjeta.Plan?.Nombre,
                        Servicio = tarjeta.Plan?.Servicio?.Nombre,
                        DuracionDias = tarjeta.Plan?.DuracionDias,

                        // Credenciales
                        Usuario = cuenta.Usuario,
                        Contrasena = cuenta.Contrasena,
                        Perfil = perfil.Nombre,
                        Pin = perfil.Pin,
                        EstadoPerfil = perfil.Estado,
                        EstadoCuenta = cuenta.Estado,

                        // Información adicional
                        TienePin = !string.IsNullOrEmpty(perfil.Pin),
                        FechaActualizacion = perfil.FechaCreacion,
                        PuedeUsar = tarjeta.Estado == "ACTIVA" && cuenta.Estado == "OCUPADA"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error obteniendo credenciales para código {codigo}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }
        // EN TARJETASCONTROLLER.CS - MODIFICAR ESTE MÉTODO

        [HttpGet("verificar/{codigo}")]
        public async Task<ActionResult> VerificarTarjeta(string codigo)
        {
            try
            {
                _logger.LogInformation($"🔍 Verificando tarjeta: {codigo}");

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return BadRequest(new { success = false, message = "El código de tarjeta no puede estar vacío" });
                }

                string codigoLimpio = codigo.Trim().ToUpper();

                // Buscar tarjeta con relaciones
                var tarjeta = await _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .Include(t => t.Vendedor)
                    .Include(t => t.ClienteActivador)
                    .FirstOrDefaultAsync(t => t.Codigo == codigoLimpio);

                if (tarjeta == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tarjeta no encontrada"
                    });
                }

                // Verificar si está vencida
                bool estaVencida = false;
                if (tarjeta.FechaVencimiento.HasValue &&
                    tarjeta.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now))
                {
                    tarjeta.Estado = "VENCIDA";
                    await _context.SaveChangesAsync();
                    estaVencida = true;
                }

                // Calcular días restantes
                int? diasRestantes = null;
                if (tarjeta.FechaVencimiento.HasValue && !estaVencida)
                {
                    var hoy = DateTime.Now;
                    var fechaVenc = tarjeta.FechaVencimiento.Value.ToDateTime(TimeOnly.MinValue);
                    diasRestantes = (fechaVenc - hoy).Days;
                }

                // LÓGICA CORREGIDA: ¿Puede ver credenciales?
                // SOLO si está ACTIVA, NO está vencida, y tiene perfil con cuenta
                bool puedeVerCredenciales = tarjeta.Estado == "ACTIVA" &&
                                           !estaVencida &&
                                           tarjeta.Perfil != null &&
                                           tarjeta.Perfil.Cuenta != null;

                // Preparar objeto de credenciales SOLO si cumple condiciones
                object credencialesObj = null;
                if (puedeVerCredenciales)
                {
                    credencialesObj = new
                    {
                        Usuario = tarjeta.Perfil.Cuenta.Usuario,
                        Contrasena = tarjeta.Perfil.Cuenta.Contrasena,
                        Perfil = tarjeta.Perfil.Nombre,
                        Pin = tarjeta.Perfil.Pin,
                        Estado = "ASIGNADO"
                    };
                }

                // Información para tarjetas ASIGNADAS
                object informacionAsignacion = null;
                if (tarjeta.Estado == "ASIGNADA")
                {
                    informacionAsignacion = new
                    {
                        Mensaje = "Tarjeta asignada a vendedor - Pendiente de activación",
                        PuedeActivar = false,
                        Vendedor = tarjeta.Vendedor != null ? new
                        {
                            tarjeta.Vendedor.Nombre,
                            tarjeta.Vendedor.Celular,
                            tarjeta.Vendedor.Email
                        } : null,
                        Nota = "Las credenciales se generarán al activar la tarjeta"
                    };
                }

                // Cliente final (solo para ACTIVAS)
                object clienteFinal = null;
                if (tarjeta.Estado == "ACTIVADA" && tarjeta.ClienteActivador != null)
                {
                    clienteFinal = new
                    {
                        tarjeta.ClienteActivador.Id,
                        tarjeta.ClienteActivador.Nombre,
                        tarjeta.ClienteActivador.Celular,
                        tarjeta.ClienteActivador.Email
                    };
                }

                // PREPARAR RESPUESTA
                var response = new
                {
                    success = true,
                    data = new
                    {
                        // Información básica
                        tarjeta.IdTarjeta,
                        tarjeta.Codigo,
                        tarjeta.Serie,
                        tarjeta.Lote,
                        tarjeta.Estado,
                        tarjeta.FechaCreacion,
                        tarjeta.FechaActivacion,
                        tarjeta.FechaVencimiento,
                        DiasRestantes = diasRestantes,
                        EstaVencida = estaVencida,

                        // Plan y servicio
                        Plan = tarjeta.Plan != null ? new
                        {
                            tarjeta.Plan.IdPlan,
                            tarjeta.Plan.Nombre,
                            tarjeta.Plan.DuracionDias,
                            Servicio = tarjeta.Plan.Servicio != null ? new
                            {
                                tarjeta.Plan.Servicio.IdServicio,
                                tarjeta.Plan.Servicio.Nombre,
                                tarjeta.Plan.Servicio.Codigo,
                                tarjeta.Plan.Servicio.MaxPerfiles
                            } : null
                        } : null,

                        // CREDENCIALES (SOLO si está ACTIVA y cumple condiciones)
                        Credenciales = credencialesObj,

                        // Información para ASIGNADAS
                        InformacionAsignacion = informacionAsignacion,

                        // Información del cliente final (si está activa)
                        ClienteFinal = clienteFinal,

                        // Flags para el frontend
                        PuedeVerCredenciales = puedeVerCredenciales,
                        MensajeEstado = ObtenerMensajeDetallado(tarjeta.Estado, tarjeta.FechaVencimiento, tarjeta.Vendedor)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error verificando tarjeta {codigo}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // Método auxiliar mejorado
        private string ObtenerMensajeDetallado(string estado, DateOnly? fechaVencimiento, Cliente vendedor)
        {
            switch (estado)
            {
                case "GENERADA":
                    return "📄 Tarjeta generada - Esperando asignación a vendedor";

                case "ASIGNADA":
                    var mensajeVendedor = vendedor != null ?
                        $" al vendedor: {vendedor.Nombre}" :
                        " a un vendedor";
                    return $"📦 Tarjeta asignada{mensajeVendedor} - Contacta al vendedor para activarla";

                case "ACTIVADA":
                    if (fechaVencimiento.HasValue)
                    {
                        var diasRestantes = (fechaVencimiento.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;
                        if (diasRestantes > 0)
                            return $"✅ Tarjeta activa - Vence en {diasRestantes} días";
                        else if (diasRestantes == 0)
                            return "⚠️ Tarjeta activa - Vence hoy";
                        else
                            return "❌ Tarjeta vencida";
                    }
                    return "✅ Tarjeta activa - Lista para usar";

                case "VENCIDA":
                    return "❌ Tarjeta vencida - No puede ser utilizada";

                case "UTILIZADA":
                    return "🔄 Tarjeta ya utilizada - No disponible";

                default:
                    return $"Estado: {estado}";
            }
        }
        // También modifica el endpoint existente para usar el nuevo método
        [HttpGet("imprimir-lote/{lote}")]
        public async Task<IActionResult> ImprimirLote(string lote)
        {
            return await ImprimirConImagen(lote);
        }
        // POST: api/Tarjetas/activar-cliente-final
        [HttpPost("activar-cliente-final")]
        public async Task<ActionResult<CredencialesResponseDTO>> ActivarTarjetaClienteFinal([FromBody] ActivarTarjetaClienteFinalDTO activarDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation($"Activación para cliente final: {activarDTO.CodigoTarjeta}");

                // Buscar tarjeta
                var tarjeta = await _context.Tarjetas
                    .Include(t => t.Plan)
                        .ThenInclude(p => p.Servicio)
                    .Include(t => t.Perfil)
                        .ThenInclude(p => p.Cuenta)
                    .FirstOrDefaultAsync(t => t.Codigo == activarDTO.CodigoTarjeta);

                if (tarjeta == null)
                {
                    return NotFound(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "Código de tarjeta no válido"
                    });
                }

                // Validaciones de tarjeta
                if (tarjeta.Estado == "ACTIVADA")
                {
                    return BadRequest(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "Esta tarjeta ya ha sido activada"
                    });
                }

                if (tarjeta.Estado == "VENCIDA" ||
                    (tarjeta.FechaVencimiento.HasValue && tarjeta.FechaVencimiento < DateOnly.FromDateTime(DateTime.Now)))
                {
                    tarjeta.Estado = "VENCIDA";
                    await _context.SaveChangesAsync();
                    return BadRequest(new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "La tarjeta ha vencido"
                    });
                }

                // ============================================
                // CREAR O BUSCAR CLIENTE FINAL
                // ============================================
                Cliente clienteFinal = null;

                // Buscar por email primero
                if (!string.IsNullOrEmpty(activarDTO.Email))
                {
                    clienteFinal = await _context.Clientes
                        .FirstOrDefaultAsync(c =>
                            c.Email == activarDTO.Email &&
                            c.TipoCliente == "FINAL");
                }

                // Si no existe, buscar por celular
                if (clienteFinal == null && !string.IsNullOrEmpty(activarDTO.Celular))
                {
                    clienteFinal = await _context.Clientes
                        .FirstOrDefaultAsync(c =>
                            c.Celular == activarDTO.Celular &&
                            c.TipoCliente == "FINAL");
                }

                // Si no existe el cliente, crearlo
                if (clienteFinal == null)
                {
                    // Generar usuario automático
                    string usuarioGenerado = string.Empty;

                    if (!string.IsNullOrEmpty(activarDTO.Email))
                    {
                        usuarioGenerado = activarDTO.Email.Split('@')[0];
                    }
                    else if (!string.IsNullOrEmpty(activarDTO.Celular))
                    {
                        usuarioGenerado = "cli" + activarDTO.Celular.Replace("+", "").Replace(" ", "");
                    }
                    else
                    {
                        usuarioGenerado = "cliente" + DateTime.Now.ToString("yyMMddHHmmss");
                    }

                    // Generar contraseña aleatoria
                    string contrasenaGenerada = GenerarContrasenaAleatoria(8);

                    clienteFinal = new Cliente
                    {
                        Nombre = activarDTO.NombreCliente.Trim(),
                        Usuario = usuarioGenerado.ToLower(),
                        Celular = activarDTO.Celular?.Trim(),
                        Email = activarDTO.Email?.Trim(),
                        Contrasena = contrasenaGenerada,
                        TipoCliente = "FINAL",
                        Estado = "activo",
                        FechaCreacion = DateTime.Now
                    };

                    _context.Clientes.Add(clienteFinal);
                    await _context.SaveChangesAsync();
                }

                // ============================================
                // OBTENER CREDENCIALES DE LA CUENTA
                // ============================================
                var cuenta = tarjeta.Perfil.Cuenta;
                var usuarioCuenta = cuenta.Usuario;
                var contrasenaCuenta = cuenta.Contrasena;
                var perfil = tarjeta.Perfil.Nombre;
                var pin = tarjeta.Perfil.Pin;
                var servicio = tarjeta.Plan.Servicio.Nombre;

                // ============================================
                // CREAR REGISTRO DE ACTIVACIÓN
                // ============================================
                var activacion = new Activacion
                {
                    IdTarjeta = tarjeta.IdTarjeta,
                    IdClienteFinal = clienteFinal.Id,
                    UsuarioEnviado = usuarioCuenta,
                    ContrasenaEnviada = contrasenaCuenta,
                    PerfilEnviado = perfil,
                    PinEnviado = pin,
                    IpActivacion = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Dispositivo = activarDTO.Dispositivo,
                    Navegador = activarDTO.Navegador,
                    MetodoEnvio = activarDTO.MetodoEnvio.ToUpper(),
                    FechaEnvio = DateTime.Now,
                    Entregado = true,
                    FechaConfirmacion = DateTime.Now
                };

                // Configurar número/envío según método
                switch (activarDTO.MetodoEnvio.ToUpper())
                {
                    case "WHATSAPP":
                    case "SMS":
                        activacion.NumeroEnvio = activarDTO.Celular;
                        break;
                    case "EMAIL":
                        activacion.NumeroEnvio = activarDTO.Email;
                        break;
                }

                _context.Activaciones.Add(activacion);

                // ============================================
                // ACTUALIZAR TARJETA
                // ============================================
                tarjeta.Estado = "ACTIVADA";
                tarjeta.FechaActivacion = DateTime.Now;
                tarjeta.IpActivacion = activacion.IpActivacion;
                tarjeta.IdClienteActivador = clienteFinal.Id;

                await _context.SaveChangesAsync();

                // ============================================
                // ENVIAR CREDENCIALES
                // ============================================
                bool enviado = false;
                string mensajeEnvio = "";

                switch (activarDTO.MetodoEnvio.ToUpper())
                {
                    case "EMAIL":
                        if (string.IsNullOrEmpty(activarDTO.Email))
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new CredencialesResponseDTO
                            {
                                Success = false,
                                Message = "Se requiere email para envío por correo"
                            });
                        }

                        var asunto = $"🎬 ¡Tus Credenciales de {servicio} están Listas!";
                        var cuerpoPersonalizado = @$"
                    <h2>¡Hola {activarDTO.NombreCliente}!</h2>
                    <p>Gracias por activar tu tarjeta de {servicio}. Aquí tienes tus credenciales:</p>
                    
                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                        <h3>🔐 Tus Credenciales de Acceso</h3>
                        <p><strong>Servicio:</strong> {servicio}</p>
                        <p><strong>Usuario/Email:</strong> {usuarioCuenta}</p>
                        <p><strong>Contraseña:</strong> {contrasenaCuenta}</p>
                        {(!string.IsNullOrEmpty(perfil) ? $"<p><strong>Perfil:</strong> {perfil}</p>" : "")}
                        {(!string.IsNullOrEmpty(pin) ? $"<p><strong>PIN:</strong> {pin}</p>" : "")}
                        <p><strong>Válido hasta:</strong> {tarjeta.FechaVencimiento?.ToString("dd/MM/yyyy")}</p>
                    </div>

                    <div style='background-color: #e8f5e8; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <h4>📱 ¿Cómo acceder?</h4>
                        <ol>
                            <li>Ve a la aplicación/website de {servicio}</li>
                            <li>Selecciona <strong>Iniciar Sesión</strong></li>
                            <li>Ingresa las credenciales de arriba</li>
                            <li>¡Disfruta de tu contenido!</li>
                        </ol>
                    </div>

                    <p style='color: #666; font-size: 12px;'>
                        <em>Esta activación fue realizada el {DateTime.Now:dd/MM/yyyy HH:mm}</em>
                    </p>
                ";

                        // CORRECCIÓN: Usar el método que SÍ existe
                        enviado = await _emailService.EnviarCredencialesAsync(
                            activarDTO.Email,
                            activarDTO.NombreCliente,
                            usuarioCuenta,
                            contrasenaCuenta,
                            asunto,
                            perfil,
                            pin,
                            servicio,
                            tarjeta.FechaVencimiento
                        );

                        mensajeEnvio = "por correo electrónico";
                        break;

                    default:
                        // Para WhatsApp/SMS
                        activacion.Entregado = false;
                        enviado = true;
                        mensajeEnvio = "por mensaje";
                        break;
                }

                if (!enviado)
                {
                    activacion.Entregado = false;
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();

                    return StatusCode(500, new CredencialesResponseDTO
                    {
                        Success = false,
                        Message = "Error al enviar las credenciales. Por favor contacte al soporte."
                    });
                }

                await transaction.CommitAsync();

                _logger.LogInformation($"✅ Tarjeta {tarjeta.Codigo} activada por cliente final {clienteFinal.Nombre}");

                return Ok(new CredencialesResponseDTO
                {
                    Success = true,
                    Message = $"✅ ¡Activación exitosa! Las credenciales han sido enviadas {mensajeEnvio}",
                    Usuario = usuarioCuenta,
                    Contrasena = contrasenaCuenta,
                    Perfil = perfil,
                    Pin = pin,
                    Servicio = servicio,
                    FechaVencimiento = tarjeta.FechaVencimiento?.ToDateTime(TimeOnly.MinValue)
                    // Eliminado: DatosCliente ya que no existe en CredencialesResponseDTO
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error en activación para cliente final");
                return StatusCode(500, new CredencialesResponseDTO
                {
                    Success = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                });
            }
        }
    }

    // ============================================================
    // DTOs NECESARIOS
    // ============================================================

    public class GenerarTarjetasAutomaticoDTO
    {
        public int IdPlan { get; set; }
        public int Cantidad { get; set; } = 10;
        public string? PrefijoLote { get; set; }
        public bool AsignacionAutomatica { get; set; } = true;
        public int? IdVendedor { get; set; } // AÑADIR ESTO
    }

    public class GenerarTarjetasManualDTO
    {
        public int IdPlan { get; set; }
        public string? PrefijoLote { get; set; }
        public List<CuentaManualDTO> Cuentas { get; set; } = new List<CuentaManualDTO>();
    }

    public class CuentaManualDTO
    {
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public int? Perfiles { get; set; } = 1;
        public string? Pin { get; set; }
        public string? NombrePerfil { get; set; }
    }

    public class ImpresionRequestDTO
    {
        public List<int> IdsTarjetas { get; set; } = new List<int>();
        public bool IncluirQR { get; set; } = true;
        public string? Plantilla { get; set; }
    }

    public class AsignarVendedorDTO
    {
        public List<int> IdsTarjetas { get; set; } = new List<int>();
        public int IdVendedor { get; set; }
    }

    public class ActivarTarjetaDTO
    {
        public string CodigoTarjeta { get; set; } = null!;
        public string MetodoEnvio { get; set; } = "EMAIL";
        public string? Email { get; set; }
        public string? Celular { get; set; }
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
    }

    public class CredencialesResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string? Usuario { get; set; }
        public string? Contrasena { get; set; }
        public string? Perfil { get; set; }
        public string? Pin { get; set; }
        public string? Servicio { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }
}