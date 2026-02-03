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
    public class ServiciosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiciosController> _logger;

        public ServiciosController(ApplicationDbContext context, IMapper mapper, ILogger<ServiciosController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Servicios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServicioDTO>>> GetServicios()
        {
            var servicios = await _context.Servicios
                .Where(s => s.Estado == "ACTIVO")
                .Include(s => s.Planes.Where(p => p.Estado == "ACTIVO"))
                .ToListAsync();

            return Ok(_mapper.Map<List<ServicioDTO>>(servicios));
        }

        // GET: api/Servicios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ServicioDTO>> GetServicio(int id)
        {
            var servicio = await _context.Servicios
                .Include(s => s.Planes)
                .FirstOrDefaultAsync(s => s.IdServicio == id);

            if (servicio == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<ServicioDTO>(servicio));
        }

        // POST: api/Servicios
        [HttpPost]
        public async Task<ActionResult<ServicioDTO>> CreateServicio(CrearServicioDTO crearServicioDTO)
        {
            try
            {
                // Validar que el código no exista
                var existeCodigo = await _context.Servicios
                    .AnyAsync(s => s.Codigo == crearServicioDTO.Codigo);

                if (existeCodigo)
                {
                    return BadRequest(new { message = "El código del servicio ya existe" });
                }

                // Validar que el nombre no exista
                var existeNombre = await _context.Servicios
                    .AnyAsync(s => s.Nombre == crearServicioDTO.Nombre);

                if (existeNombre)
                {
                    return BadRequest(new { message = "El nombre del servicio ya existe" });
                }

                var servicio = _mapper.Map<Servicio>(crearServicioDTO);
                _context.Servicios.Add(servicio);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetServicio), new { id = servicio.IdServicio },
                    _mapper.Map<ServicioDTO>(servicio));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear servicio");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/Servicios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServicio(int id, CrearServicioDTO crearServicioDTO)
        {
            try
            {
                var servicio = await _context.Servicios.FindAsync(id);
                if (servicio == null)
                {
                    return NotFound();
                }

                // Validar que el nuevo código no exista (si cambia)
                if (servicio.Codigo != crearServicioDTO.Codigo)
                {
                    var existeCodigo = await _context.Servicios
                        .AnyAsync(s => s.Codigo == crearServicioDTO.Codigo && s.IdServicio != id);

                    if (existeCodigo)
                    {
                        return BadRequest(new { message = "El código ya está en uso" });
                    }
                }

                // Validar que el nuevo nombre no exista (si cambia)
                if (servicio.Nombre != crearServicioDTO.Nombre)
                {
                    var existeNombre = await _context.Servicios
                        .AnyAsync(s => s.Nombre == crearServicioDTO.Nombre && s.IdServicio != id);

                    if (existeNombre)
                    {
                        return BadRequest(new { message = "El nombre ya está en uso" });
                    }
                }

                _mapper.Map(crearServicioDTO, servicio);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Servicio actualizado correctamente",
                    servicio = _mapper.Map<ServicioDTO>(servicio)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar servicio");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // DELETE: api/Servicios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServicio(int id)
        {
            try
            {
                var servicio = await _context.Servicios
                    .Include(s => s.Planes)
                    .Include(s => s.Cuentas)
                    .FirstOrDefaultAsync(s => s.IdServicio == id);

                if (servicio == null)
                {
                    return NotFound();
                }

                // Verificar si tiene planes activos
                if (servicio.Planes.Any(p => p.Estado == "ACTIVO"))
                {
                    return BadRequest(new
                    {
                        message = "No se puede eliminar el servicio porque tiene planes activos"
                    });
                }

                // Verificar si tiene cuentas
                if (servicio.Cuentas.Any())
                {
                    return BadRequest(new
                    {
                        message = "No se puede eliminar el servicio porque tiene cuentas asociadas"
                    });
                }

                // Cambiar estado a INACTIVO en lugar de eliminar
                servicio.Estado = "INACTIVO";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Servicio desactivado correctamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar servicio");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Servicios/5/planes
        [HttpGet("{id}/planes")]
        public async Task<ActionResult<IEnumerable<PlanDTO>>> GetPlanesPorServicio(int id)
        {
            var planes = await _context.Planes
                .Where(p => p.IdServicio == id && p.Estado == "ACTIVO")
                .Include(p => p.Servicio)
                .ToListAsync();

            return Ok(_mapper.Map<List<PlanDTO>>(planes));
        }

        // GET: api/Servicios/estadisticas
        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticas()
        {
            var estadisticas = await _context.Servicios
                .GroupBy(s => s.Estado)
                .Select(g => new
                {
                    Estado = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync();

            var totalPlanes = await _context.Planes.CountAsync();
            var totalCuentas = await _context.Cuentas.CountAsync();

            return Ok(new
            {
                serviciosPorEstado = estadisticas,
                totalPlanes,
                totalCuentas
            });
        }
    }
}