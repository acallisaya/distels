using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace distels.Models
{
    [Table("clientes_pagina")]
    public class ClientePagina
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; } = null!;

        // Información básica
        [Column("encabezado")]
        [StringLength(200)]
        public string Encabezado { get; set; } = "Bienvenido a mi sitio";

        [Column("subtitulo")]
        [StringLength(200)]
        public string? Subtitulo { get; set; }

        [Column("descripcion_corta")]
        public string? DescripcionCorta { get; set; }

        [Column("cuerpo")]
        public string? Cuerpo { get; set; }

        // Información de contacto
        [Column("telefono")]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("horario_atencion")]
        [StringLength(100)]
        public string? HorarioAtencion { get; set; }

        // Colores y tema
        [Column("color_fondo")]
        [StringLength(20)]
        public string ColorFondo { get; set; } = "#ffffff";

        [Column("color_texto")]
        [StringLength(20)]
        public string ColorTexto { get; set; } = "#333333";

        [Column("color_primario")]
        [StringLength(20)]
        public string ColorPrimario { get; set; } = "#2196f3";

        [Column("color_secundario")]
        [StringLength(20)]
        public string ColorSecundario { get; set; } = "#ff9800";

        [Column("color_acento")]
        [StringLength(20)]
        public string ColorAcento { get; set; } = "#4caf50";

        [Column("tema")]
        [StringLength(20)]
        public string Tema { get; set; } = "claro";

        // Imágenes
        [Column("logo_url")]
        public string? LogoUrl { get; set; }

        [Column("banner_url")]
        public string? BannerUrl { get; set; }

        [Column("favicon_url")]
        public string? FaviconUrl { get; set; }

        // Configuración de secciones
        [Column("mostrar_testimonios")]
        public bool MostrarTestimonios { get; set; } = true;

        [Column("mostrar_servicios")]
        public bool MostrarServicios { get; set; } = true;

        [Column("mostrar_equipo")]
        public bool MostrarEquipo { get; set; } = false;

        [Column("mostrar_blog")]
        public bool MostrarBlog { get; set; } = false;

        [Column("mostrar_contacto")]
        public bool MostrarContacto { get; set; } = false;

        [Column("mostrar_mapa")]
        public bool MostrarMapa { get; set; } = false;

        [Column("mostrar_animaciones")]
        public bool MostrarAnimaciones { get; set; } = true;

        [Column("mostrar_galerias")]
        public bool MostrarGalerias { get; set; } = true;

        [Column("mostrar_videos")]
        public bool MostrarVideos { get; set; } = true;

        // Redes sociales
        [Column("facebook_url")]
        public string? FacebookUrl { get; set; }

        [Column("instagram_url")]
        public string? InstagramUrl { get; set; }

        [Column("twitter_url")]
        public string? TwitterUrl { get; set; }

        [Column("linkedin_url")]
        public string? LinkedInUrl { get; set; }

        [Column("youtube_url")]
        public string? YouTubeUrl { get; set; }

        [Column("whatsapp_url")]
        public string? WhatsAppUrl { get; set; }

        // SEO
        [Column("meta_titulo")]
        [StringLength(200)]
        public string? MetaTitulo { get; set; }

        [Column("meta_descripcion")]
        public string? MetaDescripcion { get; set; }

        [Column("meta_keywords")]
        public string? MetaKeywords { get; set; }

        // Configuración avanzada
        [Column("codigo_analytics")]
        public string? CodigoAnalytics { get; set; }

        [Column("codigo_header")]
        public string? CodigoHeader { get; set; }

        [Column("codigo_footer")]
        public string? CodigoFooter { get; set; }

        // Estado y configuración
        [Column("estado")]
        [StringLength(20)]
        public string Estado { get; set; } = "activo";

        [Column("es_responsive")]
        public bool EsResponsive { get; set; } = true;

        [Column("velocidad_carga")]
        [StringLength(20)]
        public string VelocidadCarga { get; set; } = "normal";
        [Column("banner2_url")]
        public string? Banner2Url { get; set; }

        [Column("banner3_url")]
        public string? Banner3Url { get; set; }

        [Column("modal_image_url")]
        public string? ModalImageUrl { get; set; }

        [Column("fecha_creacion", TypeName = "timestamp without time zone")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación para relaciones
        public virtual ICollection<PaginaServicio> ServiciosPersonalizados { get; set; } = new List<PaginaServicio>();
        public virtual ICollection<PaginaTestimonio> TestimoniosPersonalizados { get; set; } = new List<PaginaTestimonio>();
        public virtual ICollection<PaginaGaleria> GaleriasImagenes { get; set; } = new List<PaginaGaleria>();
        public virtual ICollection<PaginaVideo> VideosEmbebidos { get; set; } = new List<PaginaVideo>();

        // Propiedad de navegación adicional
        [NotMapped]
        public string? Link { get; set; }

        // Validación personalizada para el estado
        public static bool EsEstadoValido(string estado)
        {
            return estado == "activo" || estado == "inactivo";
        }
    }

    [Table("pagina_servicios")]
    public class PaginaServicio
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("pagina_id")]
        public int PaginaId { get; set; }

        [ForeignKey("PaginaId")]
        public virtual ClientePagina Pagina { get; set; } = null!;

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("icono")]
        [StringLength(50)]
        public string Icono { get; set; } = "Settings";

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("pagina_testimonios")]
    public class PaginaTestimonio
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("pagina_id")]
        public int PaginaId { get; set; }

        [ForeignKey("PaginaId")]
        public virtual ClientePagina Pagina { get; set; } = null!;

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("cargo")]
        [StringLength(100)]
        public string? Cargo { get; set; }

        [Required]
        [Column("comentario")]
        public string Comentario { get; set; } = string.Empty;

        [Column("calificacion")]
        public int Calificacion { get; set; } = 5;

        [Column("foto_url")]
        public string? FotoUrl { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("pagina_galerias")]
    public class PaginaGaleria
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("pagina_id")]
        public int PaginaId { get; set; }

        [ForeignKey("PaginaId")]
        public virtual ClientePagina Pagina { get; set; } = null!;

        [Required]
        [Column("titulo")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<PaginaGaleriaImagen> Imagenes { get; set; } = new List<PaginaGaleriaImagen>();
    }

    [Table("pagina_galeria_imagenes")]
    public class PaginaGaleriaImagen
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("galeria_id")]
        public int GaleriaId { get; set; }

        [ForeignKey("GaleriaId")]
        public virtual PaginaGaleria Galeria { get; set; } = null!;

        [Required]
        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [Column("titulo")]
        [StringLength(200)]
        public string? Titulo { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("pagina_videos")]
    public class PaginaVideo
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("pagina_id")]
        public int PaginaId { get; set; }

        [ForeignKey("PaginaId")]
        public virtual ClientePagina Pagina { get; set; } = null!;

        [Required]
        [Column("titulo")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("tipo")]
        [StringLength(50)]
        public string Tipo { get; set; } = "youtube";

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("banner2_url")]
        [NotMapped]
        public string? Banner2Url { get; set; } = string.Empty;

        [Column("banner3_url")]
        [NotMapped]
        public string? Banner3Url { get; set; } = string.Empty;

        [Column("modal_image_url")]
        [NotMapped]
        public string? ModalImageUrl { get; set; }

    }
}