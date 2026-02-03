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
    public class VendedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VendedoresController> _logger;

        public VendedoresController(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<VendedoresController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Vendedores
        [HttpGet]
        public async Task<ActionResult> GetVendedores()
        {
            try
            {
                var vendedores = await _context.Clientes
                    .Where(c => c.TipoCliente == "VENDEDOR")
                    .Select(c => new
                    {
                        c.Id,
                        c.Nombre,
                        c.Usuario,
                        c.Celular,
                        c.Email,
                        c.Estado,
                        c.FechaCreacion,
                        ClientesAsignados = c.ClientesAsignados.Count,
                        TarjetasVendidas = c.TarjetasVendidas.Count,
                        TotalVentas = c.TarjetasVendidas.Sum(t => t.Plan.PrecioVenta)
                    })
                    .OrderByDescending(v => v.FechaCreacion)
                    .ToListAsync();

                return Ok(new { success = true, data = vendedores });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener vendedores");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/Vendedores
        [HttpPost]
        public async Task<ActionResult> CrearVendedor([FromBody] CrearVendedorDTO crearVendedorDTO)
        {
            try
            {
                // Validar que el usuario no exista
                var existeUsuario = await _context.Clientes
                    .AnyAsync(c => c.Usuario == crearVendedorDTO.Usuario);

                if (existeUsuario)
                {
                    return BadRequest(new { message = "El usuario ya existe" });
                }

                var vendedor = new Cliente
                {
                    Nombre = crearVendedorDTO.Nombre,
                    Usuario = crearVendedorDTO.Usuario,
                    Contrasena = crearVendedorDTO.Contrasena,
                    Celular = crearVendedorDTO.Celular,
                    Email = crearVendedorDTO.Email,
                    TipoCliente = "VENDEDOR",
                    Estado = "activo"
                };

                _context.Clientes.Add(vendedor);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Vendedor creado exitosamente",
                    data = new
                    {
                        vendedor.Id,
                        vendedor.Nombre,
                        vendedor.Usuario,
                        vendedor.Celular,
                        vendedor.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear vendedor");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Vendedores/5/estadisticas
        [HttpGet("{id}/estadisticas")]
        public async Task<ActionResult> GetEstadisticasVendedor(int id)
        {
            try
            {
                var vendedor = await _context.Clientes
                    .Where(c => c.Id == id && c.TipoCliente == "VENDEDOR")
                    .Select(c => new
                    {
                        c.Id,
                        c.Nombre,
                        c.Usuario
                    })
                    .FirstOrDefaultAsync();

                if (vendedor == null)
                {
                    return NotFound(new { message = "Vendedor no encontrado" });
                }

                var tarjetas = await _context.Tarjetas
                    .Where(t => t.IdVendedor == id)
                    .Include(t => t.Plan)
                    .ToListAsync();

                var estadisticas = new
                {
                    TotalTarjetas = tarjetas.Count,
                    TarjetasVendidas = tarjetas.Count(t => t.Estado == "ACTIVADA"),
                    TarjetasPendientes = tarjetas.Count(t => t.Estado == "ASIGNADA"),
                    TotalVentas = tarjetas.Sum(t => t.Plan.PrecioVenta),
                    VentasPorServicio = tarjetas
                        .GroupBy(t => t.Plan.Servicio.Nombre)
                        .Select(g => new
                        {
                            Servicio = g.Key,
                            Cantidad = g.Count(),
                            Total = g.Sum(t => t.Plan.PrecioVenta)
                        })
                        .ToList(),
                    VentasPorMes = tarjetas
                        .Where(t => t.FechaActivacion.HasValue)
                        .GroupBy(t => new { t.FechaActivacion.Value.Year, t.FechaActivacion.Value.Month })
                        .Select(g => new
                        {
                            Mes = $"{g.Key.Month}/{g.Key.Year}",
                            Cantidad = g.Count(),
                            Total = g.Sum(t => t.Plan.PrecioVenta)
                        })
                        .OrderByDescending(v => v.Mes)
                        .Take(12)
                        .ToList()
                };

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        vendedor,
                        estadisticas
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del vendedor");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Vendedores/5/clientes
        [HttpGet("{id}/clientes")]
        public async Task<ActionResult> GetClientesPorVendedor(int id)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.IdVendedorAsignado == id && c.TipoCliente == "FINAL")
                    .Select(c => new
                    {
                        c.Id,
                        c.Nombre,
                        c.Usuario,
                        c.Celular,
                        c.Email,
                        c.Estado,
                        c.FechaCreacion,
                        Activaciones = c.TarjetasActivadas.Count
                    })
                    .OrderByDescending(c => c.FechaCreacion)
                    .ToListAsync();

                return Ok(new { success = true, data = clientes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes del vendedor");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/Vendedores/5/asignar-cliente
        [HttpPost("{id}/asignar-cliente")]
        public async Task<ActionResult> AsignarClienteAVendedor(int id, [FromBody] AsignarClienteDTO asignarDTO)
        {
            try
            {
                var vendedor = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == id && c.TipoCliente == "VENDEDOR");

                if (vendedor == null)
                {
                    return NotFound(new { message = "Vendedor no encontrado" });
                }

                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == asignarDTO.IdCliente && c.TipoCliente == "FINAL");

                if (cliente == null)
                {
                    return NotFound(new { message = "Cliente no encontrado" });
                }

                // Verificar que el cliente no tenga ya un vendedor asignado
                if (cliente.IdVendedorAsignado.HasValue && cliente.IdVendedorAsignado != id)
                {
                    return BadRequest(new { message = "El cliente ya tiene un vendedor asignado" });
                }

                cliente.IdVendedorAsignado = id;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Cliente {cliente.Nombre} asignado al vendedor {vendedor.Nombre}",
                    data = new
                    {
                        cliente.Id,
                        cliente.Nombre,
                        vendedor = new
                        {
                            vendedor.Id,
                            vendedor.Nombre
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar cliente a vendedor");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }

    // DTOs para VendedoresController
    public class CrearVendedorDTO
    {
        public string Nombre { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public string Celular { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class AsignarClienteDTO
    {
        public int IdCliente { get; set; }
    }
}