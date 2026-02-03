// Program.Render.cs - Versión ESPECÍFICA para Render
using System.Text;
using distels.Profiles;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using distels.Repositories;
using distels.Services;

namespace distels
{
    public class ProgramRender
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🔥 CRUCIAL PARA RENDER: Puerto dinámico
            var port = Environment.GetEnvironmentVariable("PORT") ?? "5127";
            builder.WebHost.UseUrls($"http://*:{port}");

            // CONFIGURACIÓN GLOBAL para TLS
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            // Configuración de JSON
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Logging simplificado
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // AutoMapper
            builder.Services.AddSingleton<IMapper>(sp =>
            {
                var config = new AutoMapper.MapperConfiguration(cfg => {
                    cfg.AddProfile<AutoMapperProfile>();
                });
                return config.CreateMapper();
            });

            // DbContext - Con variable de entorno
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                                  builder.Configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine($"Database connection string configured: {!string.IsNullOrEmpty(connectionString)}");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configuración Email
            builder.Services.Configure<EmailConfig>(
                builder.Configuration.GetSection("EmailConfig"));
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Repositorios (mantén los tuyos)
            builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // CORS para Render
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("RenderPolicy", policy =>
                {
                    policy
                        .AllowAnyOrigin()  // Temporal para pruebas
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "super-secret-key-min-16-chars");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "tniservice",
                    ValidAudience = jwtSettings["Audience"] ?? "tniservice-users",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            var app = builder.Build();

            // Middleware
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
                RequestPath = "/uploads"
            });

            app.UseCors("RenderPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Endpoints específicos para Render
            app.MapGet("/", () =>
                "TNI Service API - Running on Render\n" +
                $"Environment: {app.Environment.EnvironmentName}\n" +
                $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            app.MapGet("/health", () =>
                new {
                    status = "healthy",
                    service = "tniservice",
                    timestamp = DateTime.UtcNow,
                    port = port
                });

            app.Run();
        }
    }
}