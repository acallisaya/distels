using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace distels.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task<bool> EnviarCredencialesAsync(
            string destinatario,
            string nombreCliente,
            string usuario,
            string contrasena,
            string asunto,
            string? perfil = null,
            string? pin = null,
            string? servicio = null,
            DateOnly? fechaVencimiento = null)
        {
            try
            {
                _logger.LogInformation($"📤 INICIANDO: Enviando a {destinatario} - Servicio: {servicio}");

                // 1. Validar password
                var cleanPassword = LimpiarPassword(_config.Password);
                if (cleanPassword.Length != 16)
                {
                    _logger.LogError($"❌ Password inválido: {cleanPassword.Length} chars");
                    return false;
                }

                // 2. Crear HTML con todos los datos
                var cuerpoHtml = CrearCuerpoEmailHtml(
                    nombreCliente,
                    usuario,
                    contrasena,
                    perfil,
                    pin,
                    servicio,
                    fechaVencimiento);

                // 3. Configurar y enviar
                using (var smtpClient = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.TargetName = "STARTTLS/smtp.gmail.com";
                    smtpClient.Credentials = new NetworkCredential(_config.Username, cleanPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 30000;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_config.FromEmail, _config.FromName, Encoding.UTF8),
                        Subject = asunto,
                        Body = cuerpoHtml,
                        IsBodyHtml = true,
                        SubjectEncoding = Encoding.UTF8,
                        BodyEncoding = Encoding.UTF8
                    };

                    mailMessage.To.Add(new MailAddress(destinatario, nombreCliente, Encoding.UTF8));

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"🎉 ÉXITO: Email enviado a {destinatario}");
                    return true;
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError($"❌ SMTP Error: {ex.StatusCode} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnviarTestSimpleAsync()
        {
            // ... mantén este método igual ...
            try
            {
                var cleanPassword = LimpiarPassword(_config.Password);

                using (var smtpClient = new SmtpClient(_config.SmtpServer, _config.SmtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.TargetName = "STARTTLS/smtp.gmail.com";
                    smtpClient.Credentials = new NetworkCredential(_config.Username, cleanPassword);
                    smtpClient.Timeout = 10000;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_config.FromEmail, _config.FromName),
                        Subject = "Test Simple",
                        Body = "Test exitoso",
                        IsBodyHtml = false
                    };

                    mailMessage.To.Add("nilocallisaya@gmail.com");

                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation("✅ Test simple exitoso");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Test simple falló: {ex.Message}");
                return false;
            }
        }

        private string LimpiarPassword(string password)
        {
            return password?.Replace(" ", "")?.Trim() ?? "";
        }

        private string CrearCuerpoEmailHtml(
            string nombre,
            string usuario,
            string contrasena,
            string? perfil = null,
            string? pin = null,
            string? servicio = null,
            DateOnly? fechaVencimiento = null)
        {
            var fechaVencStr = fechaVencimiento.HasValue
                ? fechaVencimiento.Value.ToString("dd/MM/yyyy")
                : "No especificada";

            // Construir el HTML por partes
            var html = new StringBuilder();

            html.AppendLine($@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
                    .content {{ padding: 40px; }}
                    .card {{ background-color: #f8f9fa; border-radius: 10px; padding: 25px; margin: 20px 0; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
                    .credential-item {{ margin-bottom: 15px; padding: 12px; background-color: white; border-radius: 5px; border-left: 4px solid #4CAF50; }}
                    .label {{ font-weight: bold; color: #555; }}
                    .value {{ color: #222; font-size: 16px; }}
                    .warning {{ background-color: #fff3cd; border-left-color: #ffc107; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    .footer {{ text-align: center; padding: 20px; color: #777; font-size: 14px; border-top: 1px solid #eee; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🎉 ¡Tus Credenciales de Acceso!</h1>
                        <p>Servicio: {(string.IsNullOrEmpty(servicio) ? "Digital" : servicio)}</p>
                    </div>
                    
                    <div class='content'>
                        <p>Hola <strong>{nombre}</strong>,</p>
                        <p>Aquí tienes tus credenciales para acceder al servicio:</p>
                        
                        <div class='card'>
                            <h2 style='color: #4CAF50; margin-top: 0;'>📋 Información de Acceso</h2>
                            
                           
                            
                            <div class='credential-item'>
                                <div class='label'>Usuario/Email:</div>
                                <div class='value'>{usuario}</div>
                            </div>
                            
                            <div class='credential-item'>
                                <div class='label'>Contraseña:</div>
                                <div class='value'>{contrasena}</div>
                            </div>");

            // Agregar perfil si existe
            if (!string.IsNullOrEmpty(perfil))
            {
                html.AppendLine($@"
                            <div class='credential-item'>
                                <div class='label'>Perfil:</div>
                                <div class='value'>{perfil}</div>
                            </div>");
            }

            // Agregar PIN si existe
            if (!string.IsNullOrEmpty(pin))
            {
                html.AppendLine($@"
                            <div class='credential-item'>
                                <div class='label'>PIN:</div>
                                <div class='value'>{pin}</div>
                            </div>");
            }

            html.AppendLine($@"
                            <div class='credential-item'>
                                <div class='label'>Url de Acceso :</div>
                                <div class='value'>http://localhost:5173/login/cliente</div>
                            </div>
                        </div>
                        
                        <div class='warning'>
                            <strong>⚠️ Importante:</strong>
                            <ul>
                                <li>Guarda estas credenciales en un lugar seguro</li>
                                <li>No compartas tus datos con nadie</li>
                                <li>Cambia tu contraseña después del primer acceso</li>
                            </ul>
                        </div>
                        
                        <p style='text-align: center; margin-top: 30px;'>
                            <strong>¿Necesitas ayuda?</strong><br>
                            Contáctanos para cualquier duda o problema.
                        </p>
                    </div>
                    
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Distels - Todos los derechos reservados</p>
                        <p>Este es un email automático, por favor no responder</p>
                    </div>
                </div>
            </body>
            </html>");

            return html.ToString();
        }
    }
}