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
using Npgsql;
using System.Collections;

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
            // ✅ CONFIGURAR SERVIDOR WEB (VERSIÓN CORREGIDA)
            // ============================================

            // SIEMPRE usar la variable PORT en Render
            var port = Environment.GetEnvironmentVariable("PORT");
            var isRenderEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

            if (isRenderEnv && !string.IsNullOrEmpty(port))
            {
                // ✅ CORRECCIÓN: Render asigna puerto dinámico
                Console.WriteLine($"🎯  RENDER DETECTADO - Usando puerto: {port}");

                // Configurar Kestrel para el puerto de Render
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Listen(IPAddress.Any, int.Parse(port));
                });

                // También configurar URLs
                builder.WebHost.UseUrls($"http://*:{port}");
            }
            else if (isDevelopment)
            {
                // ✅ Desarrollo local
                Console.WriteLine("🖥️  Desarrollo local - Puerto: 5127 (HTTP)");
                builder.WebHost.UseUrls("http://localhost:5127");
            }
            else
            {
                // ✅ Producción (no Render)
                Console.WriteLine("🌐  Producción - Puerto por defecto: 8080");
                builder.WebHost.UseUrls("http://*:8080");
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
            // ✅ CONFIGURACIÓN DE BASE DE DATOS - VERSIÓN DEFINITIVA
            // ============================================

            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                Console.WriteLine("🔧  Configurando conexión a PostgreSQL...");

                string connectionString = GetConnectionString();

                Console.WriteLine($"✅  Connection string listo");

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.MigrationsAssembly("distels");
                });

                if (isDevelopment)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }

            }, ServiceLifetime.Transient);

            // Función auxiliar para obtener connection string SIN usar Uri.Parse
            string GetConnectionString()
            {
                // 1. Primero intentar DATABASE_URL
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    Console.WriteLine("🔍  Parseando DATABASE_URL manualmente...");

                    try
                    {
                        return ParseDatabaseUrlManually(databaseUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌  Error parseando DATABASE_URL: {ex.Message}");
                        Console.WriteLine("🔄  Intentando con variables individuales...");
                    }
                }

                // 2. Si no hay DATABASE_URL o falló, usar variables individuales
                return GetConnectionStringFromEnvVars();
            }

            string ParseDatabaseUrlManually(string url)
            {
                // Parsear MANUALMENTE sin usar Uri class
                Console.WriteLine($"📦  URL original: {url}");

                // Normalizar a postgres://
                if (url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "postgres://" + url.Substring("postgresql://".Length);
                }
                else if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("URL debe empezar con postgres:// o postgresql://");
                }

                // Remover postgres://
                url = url.Substring("postgres://".Length);

                // Separar usuario:contraseña@host:puerto/basedatos
                var atIndex = url.IndexOf('@');
                if (atIndex == -1) throw new FormatException("No hay @ en la URL");

                var credentials = url.Substring(0, atIndex);
                var rest = url.Substring(atIndex + 1);

                // Parsear credenciales
                var colonIndex = credentials.IndexOf(':');
                if (colonIndex == -1) throw new FormatException("No hay : en las credenciales");

                var username = credentials.Substring(0, colonIndex);
                var password = credentials.Substring(colonIndex + 1);

                // Parsear host:puerto/basedatos
                var slashIndex = rest.IndexOf('/');
                if (slashIndex == -1) throw new FormatException("No hay / después del host");

                var hostPort = rest.Substring(0, slashIndex);
                var database = rest.Substring(slashIndex + 1);

                // Parsear host y puerto
                var host = hostPort;
                var port = 5432; // Puerto por defecto PostgreSQL

                var colonPortIndex = hostPort.IndexOf(':');
                if (colonPortIndex != -1)
                {
                    host = hostPort.Substring(0, colonPortIndex);
                    var portStr = hostPort.Substring(colonPortIndex + 1);

                    if (int.TryParse(portStr, out var parsedPort))
                    {
                        port = parsedPort;
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Puerto '{portStr}' inválido, usando 5432");
                    }
                }

                Console.WriteLine($"✅  Parseado exitoso:");
                Console.WriteLine($"   👤 Usuario: {username}");
                Console.WriteLine($"   🌐 Host: {host}");
                Console.WriteLine($"   🚪 Puerto: {port}");
                Console.WriteLine($"   🗄️  Base de datos: {database}");

                return new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = port,
                    Username = username,
                    Password = password,
                    Database = database,
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true,
                    Pooling = true,
                    MaxPoolSize = 20,
                    Timeout = 30
                }.ToString();
            }

            string GetConnectionStringFromEnvVars()
            {
                Console.WriteLine("🔍  Usando variables de entorno individuales...");

                var host = Environment.GetEnvironmentVariable("DB_HOST");
                var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "distels";
                var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
                var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

                if (string.IsNullOrEmpty(host))
                {
                    Console.WriteLine("⚠️  No hay variables DB_*, usando configuración local");
                    return "Host=localhost;Database=distels;Username=postgres;Password=postgres;";
                }

                var portStr = Environment.GetEnvironmentVariable("DB_PORT");
                var port = 5432;
                if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out var parsedPort))
                {
                    port = parsedPort;
                }

                Console.WriteLine($"✅  Variables encontradas:");
                Console.WriteLine($"   🌐 Host: {host}");
                Console.WriteLine($"   🚪 Puerto: {port}");
                Console.WriteLine($"   🗄️  Database: {database}");
                Console.WriteLine($"   👤 Usuario: {username}");

                return new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = port,
                    Username = username,
                    Password = password,
                    Database = database,
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true,
                    Pooling = true,
                    MaxPoolSize = 20
                }.ToString();
            }
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