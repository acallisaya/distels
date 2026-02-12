using AutoMapper;
using distels.Services;
using distels.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using distels.Profiles;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using System.Net;
using System.IO;

namespace distels
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // ⭐⭐ SOLUCIÓN DEFINITIVA - Agrega estas 2 líneas ⭐⭐
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            // ⭐⭐ FIN DE LA SOLUCIÓN ⭐⭐
            // CONFIGURACIÓN GLOBAL para TLS (IMPORTANTE)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            // -------------------  Configurar Kestrel para escuchar en todas las IPs -------------------
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5127); // HTTP
                options.ListenAnyIP(7183, listenOptions => listenOptions.UseHttps()); // HTTPS
            });

            // ********** Configuración de JSON para evitar ciclos **********
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // ********** AGREGAR ESTO: Configuración de Logging para ver errores **********
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

            // ------------------- AutoMapper -------------------
            builder.Services.AddSingleton<IMapper>(sp =>
            {
                var config = new AutoMapper.MapperConfiguration(cfg => {
                    cfg.AddProfile<AutoMapperProfile>();
                });
                return config.CreateMapper();
            });

            // ------------------- DbContext -------------------
            // A esto (Pooling desactivado):
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Transient);  // <-- IMPORTANTE: Transient en lugar de Scoped

            // ------------------- Configuración Email (AGREGAR ESTO) -------------------
            // 1. Configurar la clase EmailConfig desde appsettings.json
            builder.Services.Configure<EmailConfig>(
                builder.Configuration.GetSection("EmailConfig"));

            // 2. Registrar el servicio de email
            builder.Services.AddScoped<IEmailService, EmailService>();

            // 3. Configurar para usar MailKit en lugar de SmtpClient obsoleto
            //    (Esto se hace automáticamente al usar MailKit en EmailService)

            // ------------------- Repositorios -------------------
            builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // ------------------- Controladores -------------------
            builder.Services.AddControllers();
            builder.Services.AddScoped<IPdfService, PdfService>();
            // ------------------- CORS -------------------
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173", "http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // ------------------- JWT Authentication -------------------
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);



            var app = builder.Build();

            // ------------------- Middleware -------------------
            // app.UseHttpsRedirection(); // Comentar en desarrollo
            app.UseStaticFiles(); // Para wwwroot
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
                RequestPath = "/uploads"
            });
            app.UseCors("AllowReact");

            app.UseAuthentication(); // Asegúrate de que esta línea está antes de UseAuthorization
            app.UseAuthorization();

            app.MapControllers();

            // ********** AGREGAR ESTO: Endpoint para verificar estado del servicio **********
            app.MapGet("/api/health", () =>
                new {
                    status = "running",
                    timestamp = DateTime.Now,
                    emailService = "configured"
                });

            app.Run();
        }
    }
}