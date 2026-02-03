using distels.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;
    private readonly IConfiguration _configuration;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger, IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("diagnostico")]
    public IActionResult Diagnostico()
    {
        try
        {
            var diagnostico = new
            {
                EmailServiceInyectado = _emailService != null,
                TipoEmailService = _emailService?.GetType().Name,
                Timestamp = DateTime.Now,
                Configuracion = "OK"
            };

            _logger.LogInformation($"Diagnóstico: {System.Text.Json.JsonSerializer.Serialize(diagnostico)}");

            return Ok(new
            {
                success = true,
                message = "Diagnóstico del EmailController",
                data = diagnostico
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en diagnóstico");
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("test-conexion")]
    public async Task<IActionResult> TestConexion()
    {
        try
        {
            _logger.LogInformation("=== TEST DE CONEXIÓN SMTP ===");

            var enviado = await _emailService.EnviarTestSimpleAsync();

            if (enviado)
            {
                return Ok(new
                {
                    success = true,
                    message = "Conexión SMTP exitosa"
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error en conexión SMTP"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en test-conexion");
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // ✅ ESTE ES EL ÚNICO MÉTODO EnviarCredenciales - SIN DUPLICADOS
    [HttpPost("enviar-credenciales")]
    public async Task<IActionResult> EnviarCredenciales([FromBody] CredencialesRequest request)
    {
        try
        {
            _logger.LogInformation($"📤 Enviando credenciales a: {request.Destinatario}");
            _logger.LogInformation($"👤 Cliente: {request.NombreCliente}");
            _logger.LogInformation($"🔑 Usuario: {request.Usuario}");

            var enviado = await _emailService.EnviarCredencialesAsync(
                request.Destinatario,
                request.NombreCliente,
                request.Usuario,
                request.Contrasena,
                request.Asunto
            );

            if (enviado)
            {
                _logger.LogInformation($"✅ Credenciales enviadas exitosamente a {request.Destinatario}");
                return Ok(new
                {
                    success = true,
                    message = "Credenciales enviadas exitosamente"
                });
            }
            else
            {
                _logger.LogWarning($"❌ Falló el envío a {request.Destinatario}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar el email"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EnviarCredenciales");
            return StatusCode(500, new
            {
                success = false,
                message = "Error del servidor de email",
                error = ex.Message
            });
        }
    }

    [HttpGet("test-envio-real")]
    public async Task<IActionResult> TestEnvioReal()
    {
        try
        {
            _logger.LogInformation("=== TEST ENVÍO REAL ===");

            var enviado = await _emailService.EnviarCredencialesAsync(
                destinatario: "nilocallisaya@gmail.com",
                nombreCliente: "Alejandro Test",
                usuario: "test_user",
                contrasena: "Test123!",
                asunto: "Test Envío Real - " + DateTime.Now.ToString("HH:mm:ss")
            );

            if (enviado)
            {
                return Ok(new
                {
                    success = true,
                    message = "✅ Email enviado exitosamente",
                    timestamp = DateTime.Now
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Error al enviar email",
                    timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en test-envio-real");
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("debug-config")]
    public IActionResult DebugConfig()
    {
        try
        {
            var password = _configuration["EmailConfig:Password"];
            var cleanPassword = password?.Replace(" ", "") ?? "";

            var configInfo = new
            {
                SmtpServer = _configuration["EmailConfig:SmtpServer"],
                SmtpPort = _configuration["EmailConfig:SmtpPort"],
                Username = _configuration["EmailConfig:Username"],
                PasswordOriginal = $"'{password}'",
                PasswordClean = $"'{cleanPassword}'",
                PasswordLength = cleanPassword.Length,
                FromEmail = _configuration["EmailConfig:FromEmail"],
                EnableSsl = _configuration["EmailConfig:EnableSsl"],
                CurrentTlsProtocol = System.Net.ServicePointManager.SecurityProtocol.ToString(),
                Time = DateTime.Now
            };

            _logger.LogInformation($"🔍 CONFIGURACIÓN ACTUAL: {System.Text.Json.JsonSerializer.Serialize(configInfo)}");

            return Ok(new
            {
                success = true,
                config = configInfo,
                analysis = cleanPassword.Length == 16 ?
                    "✅ Password OK (16 chars)" :
                    "❌ Password debe tener 16 caracteres"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en debug-config");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

// ✅ UNA SOLA definición de CredencialesRequest
public class CredencialesRequest
{
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}