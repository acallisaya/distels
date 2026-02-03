using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using distels.DTO;
using AutoMapper;
using distels.Repositories;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CuentasController> _logger;

        public CuentasController(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<CuentasController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Cuentas
        [HttpGet]
        public async Task<ActionResult> GetCuentas(
            [FromQuery] int? idServicio,
            [FromQuery] string? estado,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Cuentas
                    .Include(c => c.Servicio)
                    .Include(c => c.Perfiles)
                    .AsQueryable();

                // Aplicar filtros
                if (idServicio.HasValue)
                {
                    query = query.Where(c => c.IdServicio == idServicio.Value);
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(c => c.Estado == estado);
                }

                // Paginación
                var total = await query.CountAsync();
                var cuentas = await query
                    .OrderByDescending(c => c.FechaCreacion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = cuentas.Select(c => new
                    {
                        c.IdCuenta,
                        c.Usuario,
                        Servicio = c.Servicio.Nombre,
                        c.Estado,
                        c.FechaCreacion,
                        c.FechaUltimoUso,
                        Perfiles = c.Perfiles.Count,
                        PerfilesDisponibles = c.Perfiles.Count(p => p.Estado == "DISPONIBLE")
                    }),
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
                _logger.LogError(ex, "Error al obtener cuentas");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/Cuentas/importar
        [HttpPost("importar")]
        public async Task<ActionResult> ImportarCuentas([FromBody] ImportarCuentasDTO importarDTO)
        {
            try
            {
                if (importarDTO.Cuentas == null || !importarDTO.Cuentas.Any())
                {
                    return BadRequest(new { message = "No se proporcionaron cuentas" });
                }

                // Verificar que el servicio exista
                var servicio = await _context.Servicios.FindAsync(importarDTO.IdServicio);
                if (servicio == null)
                {
                    return BadRequest(new { message = "El servicio no existe" });
                }

                var cuentasExistentes = await _context.Cuentas
                    .Where(c => c.IdServicio == importarDTO.IdServicio)
                    .Select(c => c.Usuario)
                    .ToListAsync();

                var cuentasNuevas = new List<Cuenta>();
                var perfilesNuevos = new List<Perfil>();
                var cuentasDuplicadas = new List<string>();
                var contador = 0;

                foreach (var cuentaDTO in importarDTO.Cuentas)
                {
                    // Verificar si la cuenta ya existe
                    if (cuentasExistentes.Contains(cuentaDTO.Usuario))
                    {
                        cuentasDuplicadas.Add(cuentaDTO.Usuario);
                        continue;
                    }

                    // Crear cuenta
                    var cuenta = new Cuenta
                    {
                        IdServicio = importarDTO.IdServicio,
                        Usuario = cuentaDTO.Usuario,
                        Contrasena = cuentaDTO.Contrasena,
                        Estado = "DISPONIBLE"
                    };

                    cuentasNuevas.Add(cuenta);

                    // Crear perfiles según el servicio
                    int perfilesACrear = Math.Min(servicio.MaxPerfiles, cuentaDTO.Perfiles ?? 1);
                    for (int i = 1; i <= perfilesACrear; i++)
                    {
                        var perfil = new Perfil
                        {
                            Cuenta = cuenta,
                            Nombre = $"Perfil {i}",
                            Pin = cuentaDTO.Pin ?? "",
                            Estado = "DISPONIBLE"
                        };
                        perfilesNuevos.Add(perfil);
                    }

                    contador++;
                }

                // Guardar en la base de datos
                _context.Cuentas.AddRange(cuentasNuevas);
                _context.Perfiles.AddRange(perfilesNuevos);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Importadas {contador} cuentas para el servicio {servicio.Nombre}");

                return Ok(new
                {
                    success = true,
                    message = $"Se importaron {contador} cuentas exitosamente",
                    importadas = contador,
                    duplicadas = cuentasDuplicadas.Count,
                    cuentasDuplicadas,
                    totalPerfiles = perfilesNuevos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar cuentas");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Cuentas/estadisticas
        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticasCuentas()
        {
            try
            {
                var estadisticas = await _context.Cuentas
                    .GroupBy(c => new { c.IdServicio, c.Estado })
                    .Select(g => new
                    {
                        ServicioId = g.Key.IdServicio,
                        Servicio = g.FirstOrDefault().Servicio.Nombre,
                        Estado = g.Key.Estado,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();

                var totalCuentas = await _context.Cuentas.CountAsync();
                var cuentasDisponibles = await _context.Cuentas.CountAsync(c => c.Estado == "DISPONIBLE");
                var cuentasOcupadas = await _context.Cuentas.CountAsync(c => c.Estado == "OCUPADA");

                var perfiles = await _context.Perfiles
                    .GroupBy(p => p.Estado)
                    .Select(g => new
                    {
                        Estado = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalCuentas,
                        cuentasDisponibles,
                        cuentasOcupadas,
                        estadisticasPorServicio = estadisticas,
                        perfiles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de cuentas");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/Cuentas/5/reset
        [HttpPut("{id}/reset")]
        public async Task<ActionResult> ResetearCuenta(int id)
        {
            try
            {
                var cuenta = await _context.Cuentas
                    .Include(c => c.Perfiles)
                    .FirstOrDefaultAsync(c => c.IdCuenta == id);

                if (cuenta == null)
                {
                    return NotFound(new { message = "Cuenta no encontrada" });
                }

                // Resetear cuenta a DISPONIBLE
                cuenta.Estado = "DISPONIBLE";
                cuenta.FechaUltimoUso = null;

                // Resetear todos los perfiles asociados
                foreach (var perfil in cuenta.Perfiles)
                {
                    perfil.Estado = "DISPONIBLE";
                    perfil.FechaAsignacion = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cuenta reseteada exitosamente",
                    cuenta = new
                    {
                        cuenta.IdCuenta,
                        cuenta.Usuario,
                        cuenta.Estado,
                        perfilesReseteados = cuenta.Perfiles.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear cuenta");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Cuentas/disponibles
        [HttpGet("disponibles")]
        public async Task<ActionResult> GetCuentasDisponibles([FromQuery] int idServicio)
        {
            try
            {
                var cuentas = await _context.Cuentas
                    .Where(c => c.IdServicio == idServicio && c.Estado == "DISPONIBLE")
                    .Include(c => c.Perfiles.Where(p => p.Estado == "DISPONIBLE"))
                    .Select(c => new
                    {
                        c.IdCuenta,
                        c.Usuario,
                        PerfilesDisponibles = c.Perfiles.Count,
                        Perfiles = c.Perfiles.Select(p => new
                        {
                            p.IdPerfil,
                            p.Nombre,
                            p.Pin
                        })
                    })
                    .Take(100) // Limitar para no sobrecargar
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = cuentas,
                    total = cuentas.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuentas disponibles");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }

    // DTOs para CuentasController
    public class ImportarCuentasDTO
    {
        public int IdServicio { get; set; }
        public List<CuentaImportarDTO> Cuentas { get; set; } = new List<CuentaImportarDTO>();
    }

    public class CuentaImportarDTO
    {
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public int? Perfiles { get; set; } = 1;
        public string? Pin { get; set; }
    }
}