using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Net;
using AutoMapper;
using distels.Services;
using distels.Repositories;
using distels.Profiles;

namespace distels
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================================
            // ✅ CONFIGURACIÓN COMÚN
            // ============================================

            // FIX para PostgreSQL
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            // ============================================
            // ✅ DETECCIÓN DE ENTORNO
            // ============================================

            bool isDevelopment = builder.Environment.IsDevelopment();
            bool isRender = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));
            bool isProduction = !isDevelopment;

            Console.WriteLine("========================================");
            Console.WriteLine($"🚀  DISTELS API - Iniciando");
            Console.WriteLine($"📁  Entorno: {(isDevelopment ? "DESARROLLO" : "PRODUCCIÓN")}");
            Console.WriteLine($"🌐  En Render: {(isRender ? "SÍ" : "NO")}");
            Console.WriteLine("========================================");

            // ============================================
            // ✅ CONFIGURAR SERVIDOR WEB
            // ============================================

            if (isRender)
            {
                // ✅ CONFIGURACIÓN PARA RENDER
                var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
                Console.WriteLine($"🔊  Render - Puerto asignado: {port}");

                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(System.Net.IPAddress.Any, int.Parse(port));
                });

                // Para Render, usar solo HTTP
                builder.WebHost.UseUrls($"http://*:{port}");
            }
            else if (isDevelopment)
            {
                // ✅ CONFIGURACIÓN PARA DESARROLLO LOCAL
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(5127); // HTTP
                });

                Console.WriteLine("🖥️  Desarrollo local - Puerto: 5127 (HTTP)");
            }

            // ============================================
            // ✅ CONFIGURACIÓN DE JSON
            // ============================================

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // ============================================
            // ✅ CONFIGURACIÓN DE LOGGING
            // ============================================

            if (isDevelopment)
            {
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
                Console.WriteLine("📝  Logging: MODO DEBUG ACTIVADO");
            }
            else
            {
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Information);
                Console.WriteLine("📝  Logging: MODO PRODUCCIÓN");
            }

            // ============================================
            // ✅ CONFIGURACIÓN DE CORS
            // ============================================

            builder.Services.AddCors(options =>
            {
                // Política para desarrollo
                options.AddPolicy("DevelopmentPolicy", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:5173",
                            "http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });

                // Política para producción
                options.AddPolicy("ProductionPolicy", policy =>
                {
                    var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ??
                                     "https://distels-frontend.onrender.com";

                    Console.WriteLine($"🌐  Frontend URL configurada: {frontendUrl}");

                    policy.WithOrigins(frontendUrl)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // ============================================
            // ✅ CONFIGURACIÓN DE BASE DE DATOS (VERSIÓN SIMPLIFICADA)
            // ============================================

            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                Console.WriteLine($"🔗  Configurando conexión a PostgreSQL...");

                if (isProduction)
                {
                    // ✅ Para producción con SSL
                    if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
                        !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
                    {
                        connectionString += ";SslMode=Require;Trust Server Certificate=true";
                        Console.WriteLine("🔐  SSL añadido al connection string");
                    }
                }

                // ✅ SOLUCIÓN DEFINITIVA para EnableRetryOnFailure
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    // OPCIÓN 1: Usar la sobrecarga con 3 parámetros
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null  // <-- ¡ESTO ES LO QUE FALTA!
                    );

                    // OPCIÓN 2: O usar lista vacía
                    // npgsqlOptions.EnableRetryOnFailure(
                    //     maxRetryCount: 5,
                    //     maxRetryDelay: TimeSpan.FromSeconds(10),
                    //     errorCodesToAdd: new List<string>()  // Lista vacía
                    // );

                    // OPCIÓN 3: O simplemente no usar EnableRetryOnFailure
                    // (comenta las líneas anteriores si prefieres esto)

                    // Configurar timeout
                    npgsqlOptions.CommandTimeout(30);
                });

                // Logging detallado solo en desarrollo
                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    Console.WriteLine("🔍  Sensitive Data Logging: ACTIVADO");
                }
            }, ServiceLifetime.Transient);

            // ============================================
            // ✅ CONFIGURACIÓN DE AUTENTICACIÓN (SIMPLIFICADA)
            // ============================================

            // Configuración básica de autenticación
            builder.Services.AddAuthentication();

            // ============================================
            // ✅ CONFIGURACIÓN DE SERVICIOS
            // ============================================

            // Email
            builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("EmailConfig"));
            builder.Services.AddScoped<IEmailService, EmailService>();

            // AutoMapper
            builder.Services.AddSingleton<IMapper>(sp =>
            {
                var config = new MapperConfiguration(cfg => {
                    cfg.AddProfile<AutoMapperProfile>();
                });
                return config.CreateMapper();
            });

            // Repositorios
            builder.Services.AddScoped<IParametroRepository, ParametroRepository>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            // Otros servicios
            builder.Services.AddScoped<IPdfService, PdfService>();
            builder.Services.AddControllers();

            // HttpClient para producción
            if (isProduction)
            {
                builder.Services.AddHttpClient();
            }

            Console.WriteLine("✅  Todos los servicios configurados correctamente");

            // ============================================
            // ✅ CONSTRUIR APLICACIÓN
            // ============================================

            var app = builder.Build();

            Console.WriteLine("========================================");
            Console.WriteLine("🏗️  Construyendo aplicación...");
            Console.WriteLine("========================================");

            // ============================================
            // ✅ CONFIGURAR MIDDLEWARE
            // ============================================

            if (isDevelopment)
            {
                Console.WriteLine("🛠️  Configurando para DESARROLLO...");
                app.UseDeveloperExceptionPage();
                app.UseCors("DevelopmentPolicy");
            }
            else
            {
                Console.WriteLine("🚀  Configurando para PRODUCCIÓN...");
                app.UseExceptionHandler("/error");
                app.UseCors("ProductionPolicy");
            }

            // Middleware comunes
            if (!isDevelopment && !isRender)
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            // Servir archivos uploads
            var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
                Console.WriteLine($"📁  Directorio uploads creado: {uploadsPath}");
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads"
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // ============================================
            // ✅ MAPEAR ENDPOINTS
            // ============================================

            app.MapControllers();

            // Health check endpoint
            app.MapGet("/health", () => new
            {
                status = "healthy",
                environment = app.Environment.EnvironmentName,
                timestamp = DateTime.UtcNow,
                isDevelopment = isDevelopment,
                isRender = isRender,
                database = "PostgreSQL",
                version = "1.0.0"
            });

            // Info endpoint
            app.MapGet("/api/info", () => new
            {
                app = "Distels API",
                version = "1.0.0",
                environment = app.Environment.EnvironmentName,
                supports = new[] { "Clientes", "Páginas", "Email", "PDF" }
            });

            // Root endpoint
            app.MapGet("/", () =>
            {
                return Results.Ok(new
                {
                    message = "Distels API funcionando",
                    environment = app.Environment.EnvironmentName,
                    documentation = "/api/info",
                    health = "/health"
                });
            });

            // ============================================
            // ✅ VERIFICAR BASE DE DATOS
            // ============================================

            if (isProduction || isRender)
            {
                Console.WriteLine("🔄  Verificando conexión a base de datos...");

                try
                {
                    using var scope = app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Intentar conexión simple
                    var canConnect = db.Database.CanConnect();

                    if (canConnect)
                    {
                        Console.WriteLine("✅  Conexión a PostgreSQL exitosa");

                        // Intentar aplicar migraciones si existen
                        try
                        {
                            db.Database.Migrate();
                            Console.WriteLine("✅  Base de datos verificada");
                        }
                        catch (Exception migEx)
                        {
                            Console.WriteLine($"ℹ️  Sin migraciones pendientes: {migEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️  No se pudo conectar a la base de datos");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌  Error en conexión a base de datos: {ex.Message}");
                    Console.WriteLine($"🔍  Detalle: {ex.InnerException?.Message}");
                }
            }

            // ============================================
            // ✅ INICIAR APLICACIÓN
            // ============================================

            Console.WriteLine("========================================");
            Console.WriteLine("🎉  APLICACIÓN LISTA PARA INICIAR");
            Console.WriteLine($"📡  Entorno: {app.Environment.EnvironmentName}");
            Console.WriteLine($"⏰  Hora de inicio: {DateTime.Now}");
            Console.WriteLine($"🌐  URL: {(isRender ? $"Puerto {Environment.GetEnvironmentVariable("PORT")}" : "http://localhost:5127")}");
            Console.WriteLine("========================================");

            app.Run();
        }
    }
}