using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class ArchivosController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ArchivosController> _logger;

        public ArchivosController(IWebHostEnvironment environment, ILogger<ArchivosController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        // POST: api/Archivos/subir/logo
        [HttpPost("subir/logo")]
        public async Task<ActionResult> SubirLogo(IFormFile archivo)
        {
            return await SubirArchivo(archivo, "logos");
        }

        // POST: api/Archivos/subir/banner
        [HttpPost("subir/banner")]
        public async Task<ActionResult> SubirBanner(IFormFile archivo)
        {
            return await SubirArchivo(archivo, "banners");
        }

        // POST: api/Archivos/subir/{tipo}
        [HttpPost("subir/{tipo}")]
        public async Task<ActionResult> SubirArchivo(IFormFile archivo, string tipo)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return BadRequest(new { message = "No se ha seleccionado ningún archivo" });

                // Validar tamaño máximo (5MB)
                if (archivo.Length > 5 * 1024 * 1024)
                    return BadRequest(new { message = "El archivo no puede ser mayor a 5MB" });

                // Validar extensiones permitidas
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

                if (!extensionesPermitidas.Contains(extension))
                    return BadRequest(new { message = $"Extensión no permitida. Use: {string.Join(", ", extensionesPermitidas)}" });

                // Crear carpeta si no existe
                var folderPath = Path.Combine(_environment.WebRootPath, "uploads", tipo);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Generar nombre único para el archivo
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // Generar URL pública
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var publicUrl = $"{baseUrl}/uploads/{tipo}/{fileName}";

                _logger.LogInformation($"Archivo subido exitosamente: {publicUrl}");

                return Ok(new
                {
                    message = "Archivo subido exitosamente",
                    url = publicUrl,
                    fileName = fileName,
                    size = archivo.Length,
                    type = archivo.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir archivo");
                return StatusCode(500, new { message = "Error al subir archivo", error = ex.Message });
            }
        }

        // GET: api/Archivos/uploads/{tipo}/{fileName}
        [HttpGet("uploads/{tipo}/{fileName}")]
        [AllowAnonymous]
        public IActionResult ObtenerArchivo(string tipo, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", tipo, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Archivo no encontrado" });

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener archivo: {tipo}/{fileName}");
                return StatusCode(500, new { message = "Error al obtener archivo" });
            }
        }

        // DELETE: api/Archivos/{tipo}/{fileName}
        [HttpDelete("{tipo}/{fileName}")]
        public IActionResult EliminarArchivo(string tipo, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", tipo, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Archivo no encontrado" });

                System.IO.File.Delete(filePath);

                _logger.LogInformation($"Archivo eliminado: {tipo}/{fileName}");

                return Ok(new { message = "Archivo eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar archivo: {tipo}/{fileName}");
                return StatusCode(500, new { message = "Error al eliminar archivo", error = ex.Message });
            }
        }

        // GET: api/Archivos/ejemplos
        [HttpGet("ejemplos")]
        [AllowAnonymous]
        public ActionResult GetEjemplos()
        {
            var ejemplos = new
            {
                logos = new[]
                {
                    "https://via.placeholder.com/200x100/007bff/ffffff?text=Logo+Ejemplo+1",
                    "https://via.placeholder.com/300x150/28a745/ffffff?text=Logo+Ejemplo+2",
                    "https://via.placeholder.com/250x120/dc3545/ffffff?text=Logo+Ejemplo+3"
                },
                banners = new[]
                {
                    "https://via.placeholder.com/1200x400/007bff/ffffff?text=Banner+Principal+1200x400",
                    "https://via.placeholder.com/800x300/6c757d/ffffff?text=Banner+Secundario+800x300",
                    "https://via.placeholder.com/1920x600/28a745/ffffff?text=Banner+Grande+1920x600"
                }
            };

            return Ok(ejemplos);
        }
    }
}