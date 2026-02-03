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
    public class PlanesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PlanesController> _logger;

        public PlanesController(ApplicationDbContext context, IMapper mapper, ILogger<PlanesController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/Planes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlanDTO>>> GetPlanes()
        {
            var planes = await _context.Planes
                .Where(p => p.Estado == "ACTIVO")
                .Include(p => p.Servicio)
                .OrderBy(p => p.Servicio.Nombre)
                .ThenBy(p => p.Nombre)
                .ToListAsync();

            return Ok(_mapper.Map<List<PlanDTO>>(planes));
        }

        // GET: api/Planes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlanDTO>> GetPlan(int id)
        {
            var plan = await _context.Planes
                .Include(p => p.Servicio)
                .FirstOrDefaultAsync(p => p.IdPlan == id);

            if (plan == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<PlanDTO>(plan));
        }

        // POST: api/Planes
        [HttpPost]
        public async Task<ActionResult<PlanDTO>> CreatePlan(CrearPlanDTO crearPlanDTO)
        {
            try
            {
                // Verificar que el servicio exista
                var servicio = await _context.Servicios.FindAsync(crearPlanDTO.IdServicio);
                if (servicio == null)
                {
                    return BadRequest(new { message = "El servicio no existe" });
                }

                // Verificar que no exista un plan con el mismo nombre para este servicio
                var existePlan = await _context.Planes
                    .AnyAsync(p => p.IdServicio == crearPlanDTO.IdServicio &&
                                   p.Nombre == crearPlanDTO.Nombre);

                if (existePlan)
                {
                    return BadRequest(new { message = "Ya existe un plan con este nombre para el servicio" });
                }

                var plan = _mapper.Map<Plan>(crearPlanDTO);
                _context.Planes.Add(plan);
                await _context.SaveChangesAsync();

                // Cargar relación para la respuesta
                await _context.Entry(plan)
                    .Reference(p => p.Servicio)
                    .LoadAsync();

                return CreatedAtAction(nameof(GetPlan), new { id = plan.IdPlan },
                    _mapper.Map<PlanDTO>(plan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/Planes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlan(int id, CrearPlanDTO crearPlanDTO)
        {
            try
            {
                var plan = await _context.Planes.FindAsync(id);
                if (plan == null)
                {
                    return NotFound();
                }

                // Verificar que el servicio exista si cambia
                if (plan.IdServicio != crearPlanDTO.IdServicio)
                {
                    var servicio = await _context.Servicios.FindAsync(crearPlanDTO.IdServicio);
                    if (servicio == null)
                    {
                        return BadRequest(new { message = "El servicio no existe" });
                    }
                }

                // Verificar que no exista otro plan con el mismo nombre para este servicio
                if (plan.Nombre != crearPlanDTO.Nombre || plan.IdServicio != crearPlanDTO.IdServicio)
                {
                    var existePlan = await _context.Planes
                        .AnyAsync(p => p.IdServicio == crearPlanDTO.IdServicio &&
                                       p.Nombre == crearPlanDTO.Nombre &&
                                       p.IdPlan != id);

                    if (existePlan)
                    {
                        return BadRequest(new { message = "Ya existe un plan con este nombre para el servicio" });
                    }
                }

                _mapper.Map(crearPlanDTO, plan);
                await _context.SaveChangesAsync();

                // Cargar relación para la respuesta
                await _context.Entry(plan)
                    .Reference(p => p.Servicio)
                    .LoadAsync();

                return Ok(new
                {
                    success = true,
                    message = "Plan actualizado correctamente",
                    plan = _mapper.Map<PlanDTO>(plan)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // DELETE: api/Planes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            try
            {
                var plan = await _context.Planes
                    .Include(p => p.Tarjetas)
                    .FirstOrDefaultAsync(p => p.IdPlan == id);

                if (plan == null)
                {
                    return NotFound();
                }

                // Verificar si tiene tarjetas generadas
                if (plan.Tarjetas.Any())
                {
                    return BadRequest(new
                    {
                        message = "No se puede eliminar el plan porque tiene tarjetas generadas"
                    });
                }

                // Cambiar estado a INACTIVO
                plan.Estado = "INACTIVO";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Plan desactivado correctamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plan");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/Planes/5/tarjetas
        [HttpGet("{id}/tarjetas")]
        public async Task<ActionResult> GetTarjetasPorPlan(int id)
        {
            var tarjetas = await _context.Tarjetas
                .Where(t => t.IdPlan == id)
                .Include(t => t.Plan)
                .ThenInclude(p => p.Servicio)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            var estadisticas = tarjetas
                .GroupBy(t => t.Estado)
                .Select(g => new
                {
                    Estado = g.Key,
                    Cantidad = g.Count()
                })
                .ToList();

            return Ok(new
            {
                tarjetas = _mapper.Map<List<TarjetaDTO>>(tarjetas),
                estadisticas,
                total = tarjetas.Count
            });
        }

        // POST: api/Planes/5/generar-tarjetas
        [HttpPost("{id}/generar-tarjetas")]
        public async Task<ActionResult> GenerarTarjetas(int id, [FromBody] GenerarTarjetasDTO generarTarjetasDTO)
        {
            try
            {
                if (generarTarjetasDTO.Cantidad <= 0 || generarTarjetasDTO.Cantidad > 1000)
                {
                    return BadRequest(new { message = "La cantidad debe estar entre 1 y 1000" });
                }

                var plan = await _context.Planes
                    .Include(p => p.Servicio)
                    .FirstOrDefaultAsync(p => p.IdPlan == id);

                if (plan == null)
                {
                    return NotFound(new { message = "Plan no encontrado" });
                }

                // Obtener cuentas disponibles para este servicio
                var cuentasDisponibles = await _context.Cuentas
                    .Where(c => c.IdServicio == plan.IdServicio && c.Estado == "DISPONIBLE")
                    .Include(c => c.Perfiles.Where(p => p.Estado == "DISPONIBLE"))
                    .ToListAsync();

                if (!cuentasDisponibles.Any())
                {
                    return BadRequest(new
                    {
                        message = "No hay cuentas disponibles para el servicio: " + plan.Servicio.Nombre
                    });
                }

                // Generar lote único
                var lote = $"{generarTarjetasDTO.PrefijoLote}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                var serieBase = $"SER-{DateTime.Now:yyyyMMdd}";

                var tarjetasGeneradas = new List<Tarjeta>();
                var random = new Random();

                for (int i = 0; i < generarTarjetasDTO.Cantidad; i++)
                {
                    // Seleccionar cuenta aleatoria
                    var cuenta = cuentasDisponibles[random.Next(cuentasDisponibles.Count)];

                    // Seleccionar perfil aleatorio si la cuenta tiene perfiles
                    Perfil? perfilAsignado = null;
                    if (cuenta.Perfiles.Any())
                    {
                        perfilAsignado = cuenta.Perfiles.FirstOrDefault();
                    }

                    // Generar código único
                    var codigo = GenerarCodigoTarjeta();

                    // Crear tarjeta
                    var tarjeta = new Tarjeta
                    {
                        IdPlan = id,
                        IdPerfil = perfilAsignado?.IdPerfil,
                        Codigo = codigo,
                        Serie = $"{serieBase}-{i + 1:0000}",
                        Lote = lote,
                        Estado = "GENERADA",
                        FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(plan.DuracionDias))
                    };

                    tarjetasGeneradas.Add(tarjeta);

                    // Marcar cuenta y perfil como ocupados
                    if (perfilAsignado != null)
                    {
                        perfilAsignado.Estado = "OCUPADO";
                        perfilAsignado.FechaAsignacion = DateTime.Now;
                    }
                    cuenta.Estado = "OCUPADA";
                    cuenta.FechaUltimoUso = DateTime.Now;
                }

                // Guardar todas las tarjetas
                _context.Tarjetas.AddRange(tarjetasGeneradas);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generadas {tarjetasGeneradas.Count} tarjetas para el plan {plan.Nombre}, lote: {lote}");

                return Ok(new
                {
                    success = true,
                    message = $"Se generaron {tarjetasGeneradas.Count} tarjetas exitosamente",
                    lote,
                    tarjetasGeneradas = tarjetasGeneradas.Count,
                    fechaGeneracion = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar tarjetas");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        private string GenerarCodigoTarjeta()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 15)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}