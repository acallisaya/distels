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
using distels.Models;

namespace distels
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            var port = Environment.GetEnvironmentVariable("PORT");
            var isRenderEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER"));

            if (isRenderEnv && !string.IsNullOrEmpty(port))
            {
                Console.WriteLine($"🎯  RENDER DETECTADO - Usando puerto: {port}");
                builder.WebHost.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Listen(IPAddress.Any, int.Parse(port));
                });
                builder.WebHost.UseUrls($"http://*:{port}");
            }
            else if (isDevelopment)
            {
                Console.WriteLine("🖥️  Desarrollo local - Puerto: 5127 (HTTP)");
                builder.WebHost.UseUrls("http://localhost:5127");
            }
            else
            {
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
            // ✅ CONFIGURACIÓN DE CORS - DEFINITIVA
            // ============================================

            builder.Services.AddCors(options =>
            {
                // 🟢 POLÍTICA PRINCIPAL - PARA FRONTEND ESPECÍFICO
                options.AddPolicy("PermitirFrontend", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                        origin == "http://localhost:5173" ||
                        origin == "http://localhost:3000" ||
                        origin == "https://distels-frontend.onrender.com" ||
                        origin.StartsWith("http://localhost:"))
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });

                // 🟢 POLÍTICA DE RESPALDO - PERMITE TODO
                options.AddPolicy("PermitirTodo", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // ============================================
            // ✅ CONFIGURACIÓN DE BASE DE DATOS
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

            // Función auxiliar para obtener connection string
            string GetConnectionString()
            {
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

                return GetConnectionStringFromEnvVars();
            }

            string ParseDatabaseUrlManually(string url)
            {
                Console.WriteLine($"📦  URL original: {url}");

                if (url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "postgres://" + url.Substring("postgresql://".Length);
                }
                else if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("URL debe empezar con postgres:// o postgresql://");
                }

                url = url.Substring("postgres://".Length);
                var atIndex = url.IndexOf('@');
                if (atIndex == -1) throw new FormatException("No hay @ en la URL");

                var credentials = url.Substring(0, atIndex);
                var rest = url.Substring(atIndex + 1);
                var colonIndex = credentials.IndexOf(':');
                if (colonIndex == -1) throw new FormatException("No hay : en las credenciales");

                var username = credentials.Substring(0, colonIndex);
                var password = credentials.Substring(colonIndex + 1);
                var slashIndex = rest.IndexOf('/');
                if (slashIndex == -1) throw new FormatException("No hay / después del host");

                var hostPort = rest.Substring(0, slashIndex);
                var database = rest.Substring(slashIndex + 1);
                var host = hostPort;
                var port = 5432;

                var colonPortIndex = hostPort.IndexOf(':');
                if (colonPortIndex != -1)
                {
                    host = hostPort.Substring(0, colonPortIndex);
                    var portStr = hostPort.Substring(colonPortIndex + 1);
                    if (int.TryParse(portStr, out var parsedPort))
                    {
                        port = parsedPort;
                    }
                }

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
            // ✅ CONFIGURACIÓN DE AUTENTICACIÓN
            // ============================================

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
            // ✅ CONFIGURAR MIDDLEWARE - ORDEN CORREGIDO (CRÍTICO PARA CORS)
            // ============================================

            // 1️⃣ MANEJO DE ERRORES (siempre primero)
            if (isDevelopment)
            {
                Console.WriteLine("🛠️  Configurando para DESARROLLO...");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                Console.WriteLine("🚀  Configurando para PRODUCCIÓN...");
                app.UseExceptionHandler("/error");
            }

            // 2️⃣ REDIRECCIÓN HTTPS (solo si no es Render)
            if (!isDevelopment && !isRender)
            {
                app.UseHttpsRedirection();
            }

            // 3️⃣ ARCHIVOS ESTÁTICOS (siempre)
            app.UseStaticFiles();

            // 4️⃣ 🔓 CORS - ¡UBICACIÓN CRÍTICA! DEBE IR ANTES DE UseRouting()
            Console.WriteLine("🔓  Configurando CORS con política PermitirFrontend...");
            app.UseCors("PermitirFrontend");

            // 🔥🔥🔥 SOLUCIÓN DEFINITIVA PARA PREFLIGHT OPTIONS (AGREGADO)
            app.Use((context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "https://distelsfrontend.onrender.com");
                    context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                    return context.Response.CompleteAsync();
                }
                return next();
            });

            // 5️⃣ ROUTING (a partir de aquí va el pipeline de endpoints)
            app.UseRouting();

            // 6️⃣ AUTENTICACIÓN Y AUTORIZACIÓN
            app.UseAuthentication();
            app.UseAuthorization();

            // 7️⃣ SERVIR ARCHIVOS DE UPLOADS (middleware adicional)
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

            // 8️⃣ ENDPOINTS (siempre al final)
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

            // 🆘 ENDPOINT DE EMERGENCIA - CREAR USUARIO ADMIN
            app.MapGet("/api/emergencia/crear-admin", async (ApplicationDbContext db) =>
            {
                try
                {
                    var sql = @"INSERT INTO public.usuarios (cod_usuario, tipo_rol, password, estado, fecha_registro)
                    VALUES ('admin', 'ADMIN', '1234', true, NOW())
                    ON CONFLICT (cod_usuario) DO NOTHING;";

                    await db.Database.ExecuteSqlRawAsync(sql);

                    return Results.Ok(new
                    {
                        success = true,
                        message = "✅ Usuario ADMIN creado exitosamente",
                        usuario = "admin",
                        password = "1234"
                    });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new
                    {
                        success = false,
                        error = ex.Message
                    });
                }
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
                    var canConnect = db.Database.CanConnect();

                    if (canConnect)
                    {
                        Console.WriteLine("✅  Conexión a PostgreSQL exitosa");

                        try
                        {
                            // Crear base de datos y tablas si no existen
                            db.Database.EnsureCreated();
                            Console.WriteLine("✅  Base de datos verificada");

                            // Datos semilla
                            SeedInitialData(db, scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
                        }
                        catch (Exception migEx)
                        {
                            Console.WriteLine($"ℹ️  {migEx.Message}");
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

        static void SeedInitialData(ApplicationDbContext db, ILogger logger)
        {
            try
            {
                // Servicios
                if (!db.Servicios.Any())
                {
                    logger.LogInformation("📦 Creando servicios iniciales...");
                    db.Servicios.AddRange(
                        new Servicio { Nombre = "Netflix", Codigo = "NFLX", MaxPerfiles = 4, Estado = "ACTIVO" },
                        new Servicio { Nombre = "Disney+", Codigo = "DNYP", MaxPerfiles = 4, Estado = "ACTIVO" },
                        new Servicio { Nombre = "HBO Max", Codigo = "HBO", MaxPerfiles = 3, Estado = "ACTIVO" }
                    );
                    db.SaveChanges();
                    logger.LogInformation("✅ Servicios creados");
                }

                // Planes
                if (!db.Planes.Any())
                {
                    logger.LogInformation("📦 Creando planes iniciales...");
                    var netflix = db.Servicios.First(s => s.Codigo == "NFLX");
                    var disney = db.Servicios.First(s => s.Codigo == "DNYP");

                    db.Planes.AddRange(
                        new Plan { IdServicio = netflix.IdServicio, Nombre = "Netflix 30 días", DuracionDias = 30, PrecioVenta = 15.99m, PrecioCompra = 10.99m, Estado = "ACTIVO" },
                        new Plan { IdServicio = netflix.IdServicio, Nombre = "Netflix 90 días", DuracionDias = 90, PrecioVenta = 45.99m, PrecioCompra = 30.99m, Estado = "ACTIVO" },
                        new Plan { IdServicio = disney.IdServicio, Nombre = "Disney+ 30 días", DuracionDias = 30, PrecioVenta = 12.99m, PrecioCompra = 8.99m, Estado = "ACTIVO" }
                    );
                    db.SaveChanges();
                    logger.LogInformation("✅ Planes creados");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "⚠️ Error creando datos semilla: {Message}", ex.Message);
            }
        }
    }
}