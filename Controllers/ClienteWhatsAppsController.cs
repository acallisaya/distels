using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using distels.Models;
using distels.Repositories;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteWhatsAppsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClienteWhatsAppsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ClienteWhatsApps
        [HttpGet]
        public async Task<ActionResult> GetClienteWhatsApps()
        {
            try
            {
                var whatsApps = await _context.ClienteWhatsApps
                    .Include(w => w.Cliente)
                    .ToListAsync();

                return Ok(new { success = true, data = whatsApps });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener WhatsApps",
                    error = ex.Message
                });
            }
        }

        // GET: api/ClienteWhatsApps/activos
        [HttpGet("activos")]
        public async Task<ActionResult> GetWhatsAppsActivos()
        {
            try
            {
                var whatsApps = await _context.ClienteWhatsApps
                    .Where(w => w.Estado == "activo")
                    .Include(w => w.Cliente)
                    .ToListAsync();

                return Ok(new { success = true, data = whatsApps });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener WhatsApps activos",
                    error = ex.Message
                });
            }
        }

        // GET: api/ClienteWhatsApps/cliente/{clienteId}
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult> GetWhatsAppByCliente(int clienteId)
        {
            try
            {
                var whatsApp = await _context.ClienteWhatsApps
                    .Include(w => w.Cliente)
                    .FirstOrDefaultAsync(w => w.ClienteId == clienteId);

                if (whatsApp == null)
                {
                    return Ok(new
                    {
                        success = true,
                        exists = false,
                        message = "No hay WhatsApp configurado para este cliente"
                    });
                }

                return Ok(new
                {
                    success = true,
                    exists = true,
                    data = whatsApp
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener WhatsApp del cliente",
                    error = ex.Message
                });
            }
        }

        // GET: api/ClienteWhatsApps/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetClienteWhatsApp(int id)
        {
            try
            {
                var clienteWhatsApp = await _context.ClienteWhatsApps
                    .Include(w => w.Cliente)
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (clienteWhatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    data = clienteWhatsApp
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener WhatsApp",
                    error = ex.Message
                });
            }
        }

        // POST: api/ClienteWhatsApps
        [HttpPost]
        public async Task<ActionResult> PostClienteWhatsApp([FromBody] ClienteWhatsAppCreateDto dto)
        {
            try
            {
                Console.WriteLine("=== CREAR WHATSAPP ===");
                Console.WriteLine($"ClienteId: {dto?.ClienteId}");
                Console.WriteLine($"WhatsAppNumber: {dto?.WhatsAppNumber}");

                // Validar que el cliente existe
                var cliente = await _context.Clientes.FindAsync(dto?.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El cliente no existe"
                    });
                }

                // Verificar que el cliente no tenga ya WhatsApp
                var existeWhatsApp = await _context.ClienteWhatsApps
                    .AnyAsync(w => w.ClienteId == dto.ClienteId);

                if (existeWhatsApp)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El cliente ya tiene WhatsApp configurado. Use PUT para actualizar."
                    });
                }

                // Validar número de WhatsApp
                if (string.IsNullOrEmpty(dto.WhatsAppNumber))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "El número de WhatsApp es requerido"
                    });
                }

                // Crear nueva entidad
                var clienteWhatsApp = new ClienteWhatsApp
                {
                    ClienteId = dto.ClienteId,
                    WhatsAppNumber = dto.WhatsAppNumber,
                    Estado = string.IsNullOrEmpty(dto.Estado) ? "activo" : dto.Estado,

                    // Archivos multimedia
                    ImagenUrl = dto.ImagenUrl ?? "",
                    ImagenNombre = dto.ImagenNombre ?? "",
                    VideoUrl = dto.VideoUrl ?? "",
                    VideoNombre = dto.VideoNombre ?? "",
                    AudioUrl = dto.AudioUrl ?? "",
                    AudioNombre = dto.AudioNombre ?? "",

                    // Textos
                    MensajeBienvenida = dto.MensajeBienvenida ?? "",
                    MensajePromocional = dto.MensajePromocional ?? "",

                    // Permisos
                    PermitirImagenes = dto.PermitirImagenes,
                    PermitirVideos = dto.PermitirVideos,
                    PermitirAudios = dto.PermitirAudios,
                    PermitirTextos = dto.PermitirTextos,

                    // Bot
                    BotActivo = dto.BotActivo,
                    RespuestaAutomatica = dto.RespuestaAutomatica ?? "",

                    // Fechas
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };

                _context.ClienteWhatsApps.Add(clienteWhatsApp);
                await _context.SaveChangesAsync();

                // Cargar datos del cliente para la respuesta
                await _context.Entry(clienteWhatsApp)
                    .Reference(w => w.Cliente)
                    .LoadAsync();

                Console.WriteLine($"WhatsApp creado exitosamente. ID: {clienteWhatsApp.Id}");

                return CreatedAtAction(nameof(GetClienteWhatsApp),
                    new { id = clienteWhatsApp.Id },
                    new
                    {
                        success = true,
                        message = "WhatsApp configurado correctamente",
                        data = clienteWhatsApp
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en POST WhatsApp: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al crear WhatsApp",
                    error = ex.Message
                });
            }
        }

        // PUT: api/ClienteWhatsApps/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClienteWhatsApp(int id, [FromBody] ClienteWhatsAppUpdateDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest(new
                    {
                        success = false,
                        message = "ID no coincide"
                    });

                // Verificar que exista
                var existingWhatsApp = await _context.ClienteWhatsApps.FindAsync(id);
                if (existingWhatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                // Actualizar propiedades
                existingWhatsApp.WhatsAppNumber = dto.WhatsAppNumber;
                existingWhatsApp.Estado = dto.Estado;
                existingWhatsApp.ImagenUrl = dto.ImagenUrl ?? existingWhatsApp.ImagenUrl;
                existingWhatsApp.ImagenNombre = dto.ImagenNombre ?? existingWhatsApp.ImagenNombre;
                existingWhatsApp.VideoUrl = dto.VideoUrl ?? existingWhatsApp.VideoUrl;
                existingWhatsApp.VideoNombre = dto.VideoNombre ?? existingWhatsApp.VideoNombre;
                existingWhatsApp.AudioUrl = dto.AudioUrl ?? existingWhatsApp.AudioUrl;
                existingWhatsApp.AudioNombre = dto.AudioNombre ?? existingWhatsApp.AudioNombre;
                existingWhatsApp.MensajeBienvenida = dto.MensajeBienvenida ?? existingWhatsApp.MensajeBienvenida;
                existingWhatsApp.MensajePromocional = dto.MensajePromocional ?? existingWhatsApp.MensajePromocional;
                existingWhatsApp.PermitirImagenes = dto.PermitirImagenes;
                existingWhatsApp.PermitirVideos = dto.PermitirVideos;
                existingWhatsApp.PermitirAudios = dto.PermitirAudios;
                existingWhatsApp.PermitirTextos = dto.PermitirTextos;
                existingWhatsApp.BotActivo = dto.BotActivo;
                existingWhatsApp.RespuestaAutomatica = dto.RespuestaAutomatica ?? existingWhatsApp.RespuestaAutomatica;
                existingWhatsApp.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "WhatsApp actualizado correctamente",
                    data = existingWhatsApp
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClienteWhatsAppExists(id))
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al actualizar WhatsApp",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/ClienteWhatsApps/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClienteWhatsApp(int id)
        {
            try
            {
                var clienteWhatsApp = await _context.ClienteWhatsApps.FindAsync(id);
                if (clienteWhatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                _context.ClienteWhatsApps.Remove(clienteWhatsApp);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "WhatsApp eliminado correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al eliminar WhatsApp",
                    error = ex.Message
                });
            }
        }

        // PATCH: api/ClienteWhatsApps/{id}/activar
        [HttpPatch("{id}/activar")]
        public async Task<IActionResult> ActivarWhatsApp(int id)
        {
            try
            {
                var whatsApp = await _context.ClienteWhatsApps.FindAsync(id);
                if (whatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                whatsApp.Estado = "activo";
                whatsApp.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "WhatsApp activado"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al activar WhatsApp",
                    error = ex.Message
                });
            }
        }

        // PATCH: api/ClienteWhatsApps/{id}/desactivar
        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> DesactivarWhatsApp(int id)
        {
            try
            {
                var whatsApp = await _context.ClienteWhatsApps.FindAsync(id);
                if (whatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                whatsApp.Estado = "inactivo";
                whatsApp.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "WhatsApp desactivado"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al desactivar WhatsApp",
                    error = ex.Message
                });
            }
        }

        // POST: api/ClienteWhatsApps/{id}/enviar-mensaje
        [HttpPost("{id}/enviar-mensaje")]
        public async Task<IActionResult> EnviarMensajePrueba(int id)
        {
            try
            {
                var whatsApp = await _context.ClienteWhatsApps
                    .Include(w => w.Cliente)
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (whatsApp == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "WhatsApp no encontrado"
                    });

                whatsApp.FechaActualizacion = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Mensaje de prueba enviado",
                    numero = whatsApp.WhatsAppNumber,
                    cliente = whatsApp.Cliente?.Nombre
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar mensaje",
                    error = ex.Message
                });
            }
        }

        // POST: api/ClienteWhatsApps/upsert
        [HttpPost("upsert")]
        public async Task<ActionResult> UpsertWhatsApp([FromBody] WhatsAppUpsertDto dto)
        {
            try
            {
                Console.WriteLine("=== UPSERT WHATSAPP ===");
                Console.WriteLine($"ClienteId: {dto.ClienteId}");
                Console.WriteLine($"WhatsAppNumber: {dto.WhatsAppNumber}");
                Console.WriteLine($"Estado: {dto.Estado}");

                // Validar que el cliente existe
                var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
                if (cliente == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Cliente con ID {dto.ClienteId} no existe"
                    });
                }

                // Verificar si ya existe para este cliente
                var existing = await _context.ClienteWhatsApps
                    .FirstOrDefaultAsync(w => w.ClienteId == dto.ClienteId);

                string action = "created";
                ClienteWhatsApp whatsApp;

                if (existing != null)
                {
                    // Actualizar
                    action = "updated";
                    whatsApp = existing;

                    whatsApp.WhatsAppNumber = dto.WhatsAppNumber;
                    whatsApp.Estado = dto.Estado ?? existing.Estado;
                    whatsApp.ImagenUrl = dto.ImagenUrl ?? existing.ImagenUrl;
                    whatsApp.ImagenNombre = dto.ImagenNombre ?? existing.ImagenNombre;
                    whatsApp.VideoUrl = dto.VideoUrl ?? existing.VideoUrl;
                    whatsApp.VideoNombre = dto.VideoNombre ?? existing.VideoNombre;
                    whatsApp.AudioUrl = dto.AudioUrl ?? existing.AudioUrl;
                    whatsApp.AudioNombre = dto.AudioNombre ?? existing.AudioNombre;
                    whatsApp.MensajeBienvenida = dto.MensajeBienvenida ?? existing.MensajeBienvenida;
                    whatsApp.MensajePromocional = dto.MensajePromocional ?? existing.MensajePromocional;
                    whatsApp.PermitirImagenes = dto.PermitirImagenes;
                    whatsApp.PermitirVideos = dto.PermitirVideos;
                    whatsApp.PermitirAudios = dto.PermitirAudios;
                    whatsApp.PermitirTextos = dto.PermitirTextos;
                    whatsApp.BotActivo = dto.BotActivo;
                    whatsApp.RespuestaAutomatica = dto.RespuestaAutomatica ?? existing.RespuestaAutomatica;
                    whatsApp.FechaActualizacion = DateTime.Now;
                }
                else
                {
                    // Crear nuevo
                    whatsApp = new ClienteWhatsApp
                    {
                        ClienteId = dto.ClienteId,
                        WhatsAppNumber = dto.WhatsAppNumber,
                        Estado = dto.Estado ?? "activo",
                        ImagenUrl = dto.ImagenUrl ?? "",
                        ImagenNombre = dto.ImagenNombre ?? "",
                        VideoUrl = dto.VideoUrl ?? "",
                        VideoNombre = dto.VideoNombre ?? "",
                        AudioUrl = dto.AudioUrl ?? "",
                        AudioNombre = dto.AudioNombre ?? "",
                        MensajeBienvenida = dto.MensajeBienvenida ?? "",
                        MensajePromocional = dto.MensajePromocional ?? "",
                        PermitirImagenes = dto.PermitirImagenes,
                        PermitirVideos = dto.PermitirVideos,
                        PermitirAudios = dto.PermitirAudios,
                        PermitirTextos = dto.PermitirTextos,
                        BotActivo = dto.BotActivo,
                        RespuestaAutomatica = dto.RespuestaAutomatica ?? "",
                        FechaCreacion = DateTime.Now,
                        FechaActualizacion = DateTime.Now
                    };

                    _context.ClienteWhatsApps.Add(whatsApp);
                }

                await _context.SaveChangesAsync();

                // Cargar relación con Cliente
                await _context.Entry(whatsApp)
                    .Reference(w => w.Cliente)
                    .LoadAsync();

                Console.WriteLine($"WhatsApp {action} exitosamente. ID: {whatsApp.Id}");

                return Ok(new
                {
                    success = true,
                    action = action,
                    message = action == "created"
                        ? "WhatsApp configurado exitosamente"
                        : "WhatsApp actualizado exitosamente",
                    data = whatsApp
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en UPSERT: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al guardar WhatsApp",
                    error = ex.Message
                });
            }
        }

        // GET: api/ClienteWhatsApps/diagnostico
        [HttpGet("diagnostico")]
        public async Task<ActionResult> Diagnostico()
        {
            try
            {
                var count = await _context.ClienteWhatsApps.CountAsync();
                var sample = await _context.ClienteWhatsApps
                    .Include(w => w.Cliente)
                    .Take(5)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    mensaje = "Diagnóstico del sistema WhatsApp",
                    totalRegistros = count,
                    ejemploRegistros = sample,
                    modeloEsperado = new
                    {
                        ClienteId = "int (requerido)",
                        WhatsAppNumber = "string (requerido, varchar 20)",
                        Estado = "string (default: 'activo')",
                        ImagenUrl = "string (opcional)",
                        VideoUrl = "string (opcional)",
                        AudioUrl = "string (opcional)"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        private bool ClienteWhatsAppExists(int id)
        {
            return _context.ClienteWhatsApps.Any(e => e.Id == id);
        }
    }

    // DTOs
    public class ClienteWhatsAppCreateDto
    {
        public int ClienteId { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Estado { get; set; } = "activo";

        // Archivos multimedia
        public string ImagenUrl { get; set; }
        public string ImagenNombre { get; set; }
        public string VideoUrl { get; set; }
        public string VideoNombre { get; set; }
        public string AudioUrl { get; set; }
        public string AudioNombre { get; set; }

        // Textos
        public string MensajeBienvenida { get; set; }
        public string MensajePromocional { get; set; }

        // Permisos
        public bool PermitirImagenes { get; set; } = true;
        public bool PermitirVideos { get; set; } = true;
        public bool PermitirAudios { get; set; } = true;
        public bool PermitirTextos { get; set; } = true;

        // Bot
        public bool BotActivo { get; set; } = false;
        public string RespuestaAutomatica { get; set; }
    }

    public class ClienteWhatsAppUpdateDto
    {
        public int Id { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Estado { get; set; }

        // Archivos multimedia
        public string ImagenUrl { get; set; }
        public string ImagenNombre { get; set; }
        public string VideoUrl { get; set; }
        public string VideoNombre { get; set; }
        public string AudioUrl { get; set; }
        public string AudioNombre { get; set; }

        // Textos
        public string MensajeBienvenida { get; set; }
        public string MensajePromocional { get; set; }

        // Permisos
        public bool PermitirImagenes { get; set; } = true;
        public bool PermitirVideos { get; set; } = true;
        public bool PermitirAudios { get; set; } = true;
        public bool PermitirTextos { get; set; } = true;

        // Bot
        public bool BotActivo { get; set; } = false;
        public string RespuestaAutomatica { get; set; }
    }

    public class WhatsAppUpsertDto
    {
        public int ClienteId { get; set; }
        public string WhatsAppNumber { get; set; }
        public string Estado { get; set; }

        // Archivos multimedia
        public string ImagenUrl { get; set; }
        public string ImagenNombre { get; set; }
        public string VideoUrl { get; set; }
        public string VideoNombre { get; set; }
        public string AudioUrl { get; set; }
        public string AudioNombre { get; set; }

        // Textos
        public string MensajeBienvenida { get; set; }
        public string MensajePromocional { get; set; }

        // Permisos
        public bool PermitirImagenes { get; set; } = true;
        public bool PermitirVideos { get; set; } = true;
        public bool PermitirAudios { get; set; } = true;
        public bool PermitirTextos { get; set; } = true;

        // Bot
        public bool BotActivo { get; set; } = false;
        public string RespuestaAutomatica { get; set; }
    }
}