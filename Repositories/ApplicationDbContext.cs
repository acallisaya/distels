using Microsoft.EntityFrameworkCore;
using distels.Models;
using System.Numerics;

namespace distels.Repositories
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets existentes
        public DbSet<ClienteWhatsApp> ClienteWhatsApps { get; set; }
        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Envio> Envios { get; set; } = null!;
        public DbSet<Cuenta> Cuentas { get; set; } = null!;
        public DbSet<ClientePagina> ClientePaginas { get; set; }
        public DbSet<PaginaServicio> PaginaServicios { get; set; }
        public DbSet<PaginaTestimonio> PaginaTestimonios { get; set; }
        public DbSet<PaginaGaleria> PaginaGalerias { get; set; }
        public DbSet<PaginaGaleriaImagen> PaginaGaleriaImagenes { get; set; }
        public DbSet<PaginaVideo> PaginaVideos { get; set; }

        // Nuevos DbSets para el sistema de tarjetas
        public DbSet<Servicio> Servicios { get; set; } = null!;
        public DbSet<Plan> Planes { get; set; } = null!;
        public DbSet<Perfil> Perfiles { get; set; } = null!;
        public DbSet<Tarjeta> Tarjetas { get; set; } = null!;
        public DbSet<Activacion> Activaciones { get; set; } = null!;
        // NUEVOS DbSets para Call Center
        public DbSet<LlamadaIA> LlamadasIA { get; set; }
        public DbSet<ScriptLlamada> ScriptsLlamada { get; set; }
        public DbSet<RespuestaIA> RespuestasIA { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==============================================
            // PRIMERO: Configuraciones EXISTENTES (no tocar)
            // ==============================================

            // 1. Configuración EXISTENTE de ClienteWhatsApp (MANTENER COMO ESTABA)
            modelBuilder.Entity<ClienteWhatsApp>(entity =>
            {
                entity.ToTable("clientes_whatsapp");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.ClienteId)
                    .HasColumnName("cliente_id")
                    .IsRequired();

                entity.Property(e => e.WhatsAppNumber)
                    .HasColumnName("whatsapp_number")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(15)
                    .HasDefaultValue("activo");

                entity.Property(e => e.ImagenUrl)
                    .HasColumnName("imagen_url")
                    .HasColumnType("text");

                entity.Property(e => e.ImagenNombre)
                    .HasColumnName("imagen_nombre")
                    .HasMaxLength(255);

                entity.Property(e => e.VideoUrl)
                    .HasColumnName("video_url")
                    .HasColumnType("text");

                entity.Property(e => e.VideoNombre)
                    .HasColumnName("video_nombre")
                    .HasMaxLength(255);

                entity.Property(e => e.AudioUrl)
                    .HasColumnName("audio_url")
                    .HasColumnType("text");

                entity.Property(e => e.AudioNombre)
                    .HasColumnName("audio_nombre")
                    .HasMaxLength(255);

                entity.Property(e => e.MensajeBienvenida)
                    .HasColumnName("mensaje_bienvenida")
                    .HasColumnType("text");

                entity.Property(e => e.MensajePromocional)
                    .HasColumnName("mensaje_promocional")
                    .HasColumnType("text");

                entity.Property(e => e.PermitirImagenes)
                    .HasColumnName("permitir_imagenes")
                    .HasDefaultValue(true);

                entity.Property(e => e.PermitirVideos)
                    .HasColumnName("permitir_videos")
                    .HasDefaultValue(true);

                entity.Property(e => e.PermitirAudios)
                    .HasColumnName("permitir_audios")
                    .HasDefaultValue(true);

                entity.Property(e => e.PermitirTextos)
                    .HasColumnName("permitir_textos")
                    .HasDefaultValue(true);

                entity.Property(e => e.BotActivo)
                    .HasColumnName("bot_activo")
                    .HasDefaultValue(false);

                entity.Property(e => e.RespuestaAutomatica)
                    .HasColumnName("respuesta_automatica")
                    .HasColumnType("text");

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.FechaActualizacion)
                    .HasColumnName("fecha_actualizacion")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("NOW()");

                // Relación con Cliente
                entity.HasOne(cw => cw.Cliente)
                    .WithOne(c => c.WhatsApp)
                    .HasForeignKey<ClienteWhatsApp>(cw => cw.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ClienteId)
                    .IsUnique();
            });

            // 2. Configuración EXISTENTE de ClientePagina (MANTENER COMO ESTABA)
            modelBuilder.Entity<ClientePagina>(entity =>
            {
                entity.ToTable("clientes_pagina");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.ClienteId)
                    .HasColumnName("cliente_id")
                    .IsRequired();

                entity.Property(e => e.Encabezado)
                    .HasColumnName("encabezado")
                    .HasMaxLength(200)
                    .HasDefaultValue("Bienvenido a mi sitio");

                entity.Property(e => e.Cuerpo)
                    .HasColumnName("cuerpo")
                    .HasColumnType("text");

                entity.Property(e => e.Telefono)
                    .HasColumnName("telefono")
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(e => e.ColorFondo)
                    .HasColumnName("color_fondo")
                    .HasMaxLength(20)
                    .HasDefaultValue("#ffffff");

                entity.Property(e => e.ColorTexto)
                    .HasColumnName("color_texto")
                    .HasMaxLength(20)
                    .HasDefaultValue("#333333");

                entity.Property(e => e.LogoUrl)
                    .HasColumnName("logo_url")
                    .HasColumnType("text");

                entity.Property(e => e.BannerUrl)
                    .HasColumnName("banner_url")
                    .HasColumnType("text");

//               entity.Property(e => e.Banner2Url).HasColumnName("banner2_url").HasColumnType("text");
//entity.Property(e => e.Banner3Url).HasColumnName("banner3_url").HasColumnType("text");

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(20)
                    .HasDefaultValue("activo")
                    .HasConversion<string>();

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("NOW()");
                // En tu OnModelCreating, en la configuración de ClientePagina
                entity.Property(e => e.Banner2Url)
                    .HasColumnName("banner2_url")  // Asegúrate que el nombre coincida exactamente
                    .HasColumnType("text")
                    .HasDefaultValue("");

                entity.Property(e => e.Banner3Url)
                    .HasColumnName("banner3_url")
                    .HasColumnType("text")
                    .HasDefaultValue("");
                // Relación con Cliente
                entity.HasOne(cp => cp.Cliente)
                    .WithOne(c => c.Pagina)
                    .HasForeignKey<ClientePagina>(cp => cp.ClienteId)
                     .HasConstraintName("clientes_pagina_cliente_id_fkey") // Nombre EXACTO del constraint en PostgreSQL
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ClienteId)
                    .IsUnique();
            });

            // ==============================================
            // SEGUNDO: Configuración de Cliente (ACTUALIZADA con nuevos campos)
            // ==============================================
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("clientes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.Usuario)
                    .HasColumnName("usuario")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Contrasena)
                    .HasColumnName("contrasena")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Celular)
                    .HasColumnName("celular")
                    .HasMaxLength(20);

                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(e => e.CodigoAcceso)
                    .HasColumnName("codigo_acceso")
                    .HasMaxLength(50);

                entity.Property(e => e.EnlaceAcceso)
                    .HasColumnName("enlace_acceso")
                    .HasColumnType("text");

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(50)
                    .HasDefaultValue("activo");

                // NUEVOS CAMPOS (AGREGAR)
                entity.Property(e => e.TipoCliente)
                    .HasColumnName("tipo_cliente")
                    .HasMaxLength(20)
                    .HasDefaultValue("FINAL");

                entity.Property(e => e.IdVendedorAsignado)
                    .HasColumnName("id_vendedor_asignado");

                // Índices
                entity.HasIndex(e => e.Usuario).IsUnique();
                entity.HasIndex(e => e.Celular);
                entity.HasIndex(e => e.TipoCliente);

                // Relación con vendedor (nueva)
                entity.HasOne(c => c.VendedorAsignado)
                    .WithMany(v => v.ClientesAsignados)
                    .HasForeignKey(c => c.IdVendedorAsignado)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==============================================
            // TERCERO: Nuevas configuraciones para sistema de tarjetas
            // ==============================================

            // 3. Configuración de Servicio
            modelBuilder.Entity<Servicio>(entity =>
            {
                entity.ToTable("servicios");
                entity.HasKey(e => e.IdServicio);

                entity.Property(e => e.IdServicio)
                    .HasColumnName("id_servicio")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Codigo)
                    .HasColumnName("codigo")
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.MaxPerfiles)
                    .HasColumnName("max_perfiles")
                    .HasDefaultValue(1);

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(10)
                    .HasDefaultValue("ACTIVO");

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.HasIndex(e => e.Nombre).IsUnique();
                entity.HasIndex(e => e.Codigo).IsUnique();
            });

            // 4. Configuración de Plan
            modelBuilder.Entity<Plan>(entity =>
            {
                entity.ToTable("planes");
                entity.HasKey(e => e.IdPlan);

                entity.Property(e => e.IdPlan)
                    .HasColumnName("id_plan")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.IdServicio)
                    .HasColumnName("id_servicio")
                    .IsRequired();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DuracionDias)
                    .HasColumnName("duracion_dias")
                    .IsRequired();

                entity.Property(e => e.PrecioCompra)
                    .HasColumnName("precio_compra")
                    .HasColumnType("decimal(8,2)")
                    .IsRequired();

                entity.Property(e => e.PrecioVenta)
                    .HasColumnName("precio_venta")
                    .HasColumnType("decimal(8,2)")
                    .IsRequired();

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(10)
                    .HasDefaultValue("ACTIVO");

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                // Relación con Servicio
                entity.HasOne(p => p.Servicio)
                    .WithMany(s => s.Planes)
                    .HasForeignKey(p => p.IdServicio)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdServicio, e.Nombre }).IsUnique();
            });

            // 5. Configuración de Cuenta
            modelBuilder.Entity<Cuenta>(entity =>
            {
                entity.ToTable("cuentas");
                entity.HasKey(e => e.IdCuenta);

                entity.Property(e => e.IdCuenta)
                    .HasColumnName("id_cuenta")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.IdServicio)
                    .HasColumnName("id_servicio")
                    .IsRequired();

                entity.Property(e => e.Usuario)
                    .HasColumnName("usuario")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Contrasena)
                    .HasColumnName("contrasena")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.FechaUltimoUso)
                    .HasColumnName("fecha_ultimo_uso")
                    .HasColumnType("timestamp");

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(20)
                    .HasDefaultValue("DISPONIBLE")
                    .HasConversion<string>();

                // Relación con Servicio
                entity.HasOne(c => c.Servicio)
                    .WithMany(s => s.Cuentas)
                    .HasForeignKey(c => c.IdServicio)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.IdServicio, e.Usuario }).IsUnique();
            });

            // 6. Configuración de Perfil
            modelBuilder.Entity<Perfil>(entity =>
            {
                entity.ToTable("perfiles");
                entity.HasKey(e => e.IdPerfil);

                entity.Property(e => e.IdPerfil)
                    .HasColumnName("id_perfil")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.IdCuenta)
                    .HasColumnName("id_cuenta")
                    .IsRequired();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Pin)
                    .HasColumnName("pin")
                    .HasMaxLength(6)
                    .HasDefaultValue("");

                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(20)
                    .HasDefaultValue("DISPONIBLE")
                    .HasConversion<string>();

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.FechaAsignacion)
                    .HasColumnName("fecha_asignacion")
                    .HasColumnType("timestamp");

                // Relación con Cuenta
                entity.HasOne(p => p.Cuenta)
                    .WithMany(c => c.Perfiles)
                    .HasForeignKey(p => p.IdCuenta)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. Configuración de Tarjeta
            modelBuilder.Entity<Tarjeta>(entity =>
            {
                entity.ToTable("tarjetas");
                entity.HasKey(e => e.IdTarjeta);

                entity.Property(e => e.IdTarjeta)
                    .HasColumnName("id_tarjeta")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.IdPlan)
                    .HasColumnName("id_plan")
                    .IsRequired();

                entity.Property(e => e.IdPerfil)
                    .HasColumnName("id_perfil");

                entity.Property(e => e.IdClienteActivador)
                    .HasColumnName("id_cliente_activador");
                entity.Ignore(e => e.ClienteActivador);
                entity.Property(e => e.IdVendedor)
                    .HasColumnName("id_vendedor");

                // Datos de la tarjeta
                entity.Property(e => e.Codigo)
                    .HasColumnName("codigo")
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Serie)
                    .HasColumnName("serie")
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Lote)
                    .HasColumnName("lote")
                    .IsRequired()
                    .HasMaxLength(30);

                // Activación
                entity.Property(e => e.FechaActivacion)
                    .HasColumnName("fecha_activacion")
                    .HasColumnType("timestamp");

                entity.Property(e => e.IpActivacion)
                    .HasColumnName("ip_activacion")
                    .HasMaxLength(45);

                // Estado
                entity.Property(e => e.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(20)
                    .HasDefaultValue("GENERADA")
                    .HasConversion<string>();

                // Fechas
                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.FechaVencimiento)
                    .HasColumnName("fecha_vencimiento")
                    .HasColumnType("date");

                // Relaciones
                // ✅ RELACIONES CORRECTAS - Usando IdClienteActivador (NO ClienteId)
                entity.HasOne(t => t.Plan)
                    .WithMany(p => p.Tarjetas)
                    .HasForeignKey(t => t.IdPlan)
                    .HasConstraintName("tarjetas_id_plan_fkey")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Perfil)
                    .WithMany()
                    .HasForeignKey(t => t.IdPerfil)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.ClienteActivador)
          .WithMany(c => c.TarjetasActivadas)
          .HasForeignKey(t => t.IdClienteActivador)  // Usa IdClienteActivador
          .HasConstraintName("tarjetas_id_cliente_activador_fkey")  // Nombre EXACTO del constraint
          .OnDelete(DeleteBehavior.SetNull);


                entity.HasOne(t => t.Vendedor)
          .WithMany(c => c.TarjetasVendidas)
          .HasForeignKey(t => t.IdVendedor)
          .HasConstraintName("tarjetas_id_vendedor_fkey")
          .OnDelete(DeleteBehavior.SetNull);
                // ✅ RESTRICCIÓN CHECK para estado (igual que en tu BD)
                entity.HasCheckConstraint("tarjetas_estado_check","estado IN ('GENERADA', 'ASIGNADA', 'ACTIVADA', 'VENCIDA')");
                // Índices
                entity.HasIndex(e => e.Codigo).IsUnique();
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.IdVendedor);
                entity.HasIndex(e => e.IdClienteActivador);
            });

            // 8. Configuración de Activacion
            modelBuilder.Entity<Activacion>(entity =>
            {
                entity.ToTable("activaciones");
                entity.HasKey(e => e.IdActivacion);
                entity.Property(e => e.IdActivacion)
                    .HasColumnName("id_activacion")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.IdTarjeta)
                    .HasColumnName("id_tarjeta")
                    .IsRequired();

                entity.Property(e => e.IdClienteFinal)
                    .HasColumnName("id_cliente_final");

                // Credenciales enviadas
                entity.Property(e => e.UsuarioEnviado)
                    .HasColumnName("usuario_enviado")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ContrasenaEnviada)
                    .HasColumnName("contrasena_enviada")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.PerfilEnviado)
                    .HasColumnName("perfil_enviado")
                    .HasMaxLength(100);

                entity.Property(e => e.PinEnviado)
                    .HasColumnName("pin_enviado")
                    .HasMaxLength(6);

                // Datos de la activación
                entity.Property(e => e.FechaActivacion)
                    .HasColumnName("fecha_activacion")
                    .HasColumnType("timestamp")
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.IpActivacion)
                    .HasColumnName("ip_activacion")
                    .HasMaxLength(45);

                entity.Property(e => e.Dispositivo)
                    .HasColumnName("dispositivo")
                    .HasMaxLength(100);

                entity.Property(e => e.Navegador)
                    .HasColumnName("navegador")
                    .HasMaxLength(100);

                // Método de envío
                entity.Property(e => e.MetodoEnvio)
                    .HasColumnName("metodo_envio")
                    .HasMaxLength(20)
                    .HasDefaultValue("WHATSAPP")
                    .HasConversion<string>();

                entity.Property(e => e.NumeroEnvio)
                    .HasColumnName("numero_envio")
                    .HasMaxLength(20);

                entity.Property(e => e.FechaEnvio)
                    .HasColumnName("fecha_envio")
                    .HasColumnType("timestamp");

                // Confirmación
                entity.Property(e => e.Entregado)
                    .HasColumnName("entregado")
                    .HasDefaultValue(false);

                entity.Property(e => e.FechaConfirmacion)
                    .HasColumnName("fecha_confirmacion")
                    .HasColumnType("timestamp");

                // Reenvíos
                entity.Property(e => e.VecesReenviado)
                    .HasColumnName("veces_reenviado")
                    .HasDefaultValue(0);

                entity.Property(e => e.FechaUltimoReenvio)
                    .HasColumnName("fecha_ultimo_reenvio")
                    .HasColumnType("timestamp");

                // Relaciones
                entity.HasOne(a => a.Tarjeta)
                    .WithMany(t => t.Activaciones)
                    .HasForeignKey(a => a.IdTarjeta)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ClienteFinal)
      .WithMany(c => c.Activaciones) // si quieres que Cliente tenga la colección de activaciones
      .HasForeignKey(a => a.IdClienteFinal)  // usa la columna correcta
      .HasConstraintName("activaciones_id_cliente_final_fkey") // nombre del constraint en Postgres
      .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(e => e.IdTarjeta);
                entity.HasIndex(e => e.FechaActivacion);
                entity.HasIndex(e => e.IdClienteFinal);
            });

            // Configurar relación LlamadaIA -> RespuestaIA
            modelBuilder.Entity<LlamadaIA>()
                .HasMany(l => l.Respuestas)
                .WithOne(r => r.Llamada)
                .HasForeignKey(r => r.LlamadaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar array para Tags (PostgreSQL)
            modelBuilder.Entity<RespuestaIA>()
                .Property(r => r.Tags)
                .HasColumnType("text[]");

            // Configurar relación Cliente -> LlamadasIA como Vendedor
            modelBuilder.Entity<LlamadaIA>()
                .HasOne(l => l.Vendedor)
                 .WithMany(c => c.LlamadasRealizadas) // ← aquí indicamos la colección correcta
                .HasForeignKey(l => l.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación Cliente -> LlamadasIA como ClienteFinal
            modelBuilder.Entity<LlamadaIA>()
                .HasOne(l => l.ClienteFinal)
                .WithMany(c => c.LlamadasRecibidas)
                .HasForeignKey(l => l.ClienteFinalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación ScriptLlamada -> Cliente (Vendedor)
            modelBuilder.Entity<ScriptLlamada>()
                .HasOne(s => s.Vendedor)
                .WithMany()
                .HasForeignKey(s => s.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ScriptLlamada>()
    .Property(s => s.ScriptJson)
    .HasColumnType("jsonb");
        }
    }
}