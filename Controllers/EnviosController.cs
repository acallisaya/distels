using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using distels.Repositories;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnviosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnviosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Envios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Envio>>> GetEnvios()
        {
            return await _context.Envios
                .OrderByDescending(e => e.FechaEnvio)
                .ToListAsync();
        }

        // GET: api/Envios/cliente/5
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<IEnumerable<Envio>>> GetEnviosByCliente(int clienteId)
        {
            return await _context.Envios
                .Where(e => e.ClienteId == clienteId)
                .OrderByDescending(e => e.FechaEnvio)
                .ToListAsync();
        }

       

        // GET: api/Envios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Envio>> GetEnvio(int id)
        {
            var envio = await _context.Envios.FindAsync(id);
            if (envio == null) return NotFound();
            return envio;
        }

        // POST: api/Envios
        [HttpPost]
        public async Task<ActionResult<Envio>> PostEnvio(Envio envio)
        {
            // Validar IDs
            if (envio.ClienteId <= 0 )
                return BadRequest(new { message = "ClienteId  es requerido" });

            // Asignar fecha si no viene
            if (envio.FechaEnvio == default)
                envio.FechaEnvio = DateTime.Now;

            _context.Envios.Add(envio);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnvio), new { id = envio.Id }, envio);
        }

        // PUT: api/Envios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEnvio(int id, Envio envio)
        {
            if (id != envio.Id) return BadRequest();

            var existingEnvio = await _context.Envios.FindAsync(id);
            if (existingEnvio == null) return NotFound();

            // Actualizar solo campos permitidos
            existingEnvio.ClienteId = envio.ClienteId;
            existingEnvio.Medio = envio.Medio;
            existingEnvio.TipoEnvio = envio.TipoEnvio;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnvioExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // GET: api/Envios/estadisticas
        [HttpGet("estadisticas")]
        public async Task<ActionResult<object>> GetEstadisticasEnvios()
        {
            var totalEnvios = await _context.Envios.CountAsync();

            var enviosHoy = await _context.Envios
                .Where(e => e.FechaEnvio.Date == DateTime.Today)
                .CountAsync();

            var enviosPorMedio = await _context.Envios
                .Where(e => !string.IsNullOrEmpty(e.Medio))
                .GroupBy(e => e.Medio!)
                .Select(g => new { Medio = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var enviosPorTipo = await _context.Envios
                .Where(e => !string.IsNullOrEmpty(e.TipoEnvio))
                .GroupBy(e => e.TipoEnvio!)
                .Select(g => new { TipoEnvio = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                TotalEnvios = totalEnvios,
                EnviosHoy = enviosHoy,
                EnviosPorMedio = enviosPorMedio,
                EnviosPorTipo = enviosPorTipo
            });
        }

        // DELETE: api/Envios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnvio(int id)
        {
            var envio = await _context.Envios.FindAsync(id);
            if (envio == null) return NotFound();

            _context.Envios.Remove(envio);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EnvioExists(int id)
        {
            return _context.Envios.Any(e => e.Id == id);
        }
    }
}