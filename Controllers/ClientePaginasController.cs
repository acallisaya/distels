using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using distels.Repositories;
using Npgsql;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientePaginasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public ClientePaginasController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        // GET: api/ClientePaginas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientePagina>>> GetClientePaginas()
        {
            var paginas = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .Include(p => p.GaleriasImagenes)
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos)
                .ToListAsync();

            foreach (var pagina in paginas)
            {
                pagina.Link = GenerarLink(pagina);
            }

            return Ok(paginas);
        }

        // GET: api/ClientePaginas/activas
        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<ClientePagina>>> GetPaginasActivas()
        {
            var paginas = await _context.ClientePaginas
                .Where(p => p.Estado == "activo")
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .Include(p => p.GaleriasImagenes)
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos)
                .ToListAsync();

            foreach (var pagina in paginas)
            {
                pagina.Link = GenerarLink(pagina);
            }

            return Ok(paginas);
        }

        // GET: api/
        //
        //
        // /cliente/5
        [HttpGet("cliente/{clienteId}")]
        public async Task<ActionResult<ClientePagina>> GetPaginaByCliente(int clienteId)
        {
            var pagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .Include(p => p.GaleriasImagenes)
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos)
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId);

            if (pagina == null)
            {
                return NotFound(new { message = $"No se encontró página para el cliente ID: {clienteId}" });
            }

            pagina.Link = GenerarLink(pagina);
            return Ok(pagina);
        }

        // GET: api/ClientePaginas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientePagina>> GetClientePagina(int id)
        {
            var clientePagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .Include(p => p.GaleriasImagenes)
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (clientePagina == null)
            {
                return NotFound(new { message = $"No se encontró página con ID: {id}" });
            }

            clientePagina.Link = GenerarLink(clientePagina);
            return Ok(clientePagina);
        }

        // GET: /pagina/{clienteId}  ← URL PÚBLICA - DEVUELVE JSON para React
        [HttpGet("/pagina/{clienteId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ClientePagina>> GetPaginaPublica(int clienteId)
        {
            var pagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados.Where(s => s.Activo))
                .Include(p => p.TestimoniosPersonalizados.Where(t => t.Activo))
                .Include(p => p.GaleriasImagenes.Where(g => g.Activo))
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos.Where(v => v.Activo))
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.Estado == "activo");

            if (pagina == null)
            {
                return NotFound(new
                {
                    message = $"No existe página activa para cliente ID: {clienteId}",
                    clienteId = clienteId
                });
            }

            pagina.Link = GenerarLink(pagina);
            return Ok(pagina);
        }

        // GET: /pagina-html/{clienteId}  ← HTML COMPLETO (opcional, si quieres HTML server-side)
        [HttpGet("/pagina-html/{clienteId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaginaHtml(int clienteId)
        {
            var pagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados.Where(s => s.Activo))
                .Include(p => p.TestimoniosPersonalizados.Where(t => t.Activo))
                .Include(p => p.GaleriasImagenes.Where(g => g.Activo))
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos.Where(v => v.Activo))
                .FirstOrDefaultAsync(p => p.ClienteId == clienteId && p.Estado == "activo");

            if (pagina == null)
            {
                var errorHtml = _env.IsDevelopment()
                    ? $"<html><body><h1>Página no encontrada</h1><p>No existe página activa para cliente ID: {clienteId}</p></body></html>"
                    : "<html><body><h1>Página no encontrada</h1><p>La página que buscas no existe o no está disponible.</p></body></html>";

                return Content(errorHtml, "text/html");
            }

            // Aquí puedes generar HTML si lo necesitas, pero lo mejor es que React lo maneje
            var html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{System.Net.WebUtility.HtmlEncode(pagina.Encabezado)}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        h1 {{ color: {pagina.ColorPrimario}; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{System.Net.WebUtility.HtmlEncode(pagina.Encabezado)}</h1>
        <p>{System.Net.WebUtility.HtmlEncode(pagina.DescripcionCorta ?? "")}</p>
        <!-- El resto lo maneja React -->
    </div>
    <div id='root'></div>
    <script src='/assets/bundle.js'></script> <!-- Tu bundle de React -->
</body>
</html>";

            return Content(html, "text/html");
        }

        // POST: api/ClientePaginas
        [HttpPost]
        public async Task<ActionResult<ClientePagina>> PostClientePagina(ClientePaginaDto clientePaginaDto)
        {
            // Validar modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validar estado
            if (!ClientePagina.EsEstadoValido(clientePaginaDto.Estado))
            {
                return BadRequest(new { message = "Estado inválido. Use 'activo' o 'inactivo'" });
            }

            // Verificar que el cliente existe
            var cliente = await _context.Clientes.FindAsync(clientePaginaDto.ClienteId);
            if (cliente == null)
            {
                return BadRequest(new { message = "El cliente no existe" });
            }

            // Verificar que el cliente no tenga ya una página
            var existePagina = await _context.ClientePaginas
                .AnyAsync(p => p.ClienteId == clientePaginaDto.ClienteId);

            if (existePagina)
            {
                return BadRequest(new { message = "El cliente ya tiene una página asignada" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Crear la página principal
                var pagina = new ClientePagina
                {
                    ClienteId = clientePaginaDto.ClienteId,
                    Encabezado = clientePaginaDto.Encabezado,
                    Subtitulo = clientePaginaDto.Subtitulo,
                    DescripcionCorta = clientePaginaDto.DescripcionCorta,
                    Cuerpo = clientePaginaDto.Cuerpo,
                    Telefono = clientePaginaDto.Telefono,
                    Email = clientePaginaDto.Email,
                    Direccion = clientePaginaDto.Direccion,
                    HorarioAtencion = clientePaginaDto.HorarioAtencion,
                    ColorFondo = clientePaginaDto.ColorFondo,
                    ColorTexto = clientePaginaDto.ColorTexto,
                    ColorPrimario = clientePaginaDto.ColorPrimario,
                    ColorSecundario = clientePaginaDto.ColorSecundario,
                    ColorAcento = clientePaginaDto.ColorAcento,
                    Tema = clientePaginaDto.Tema,
                    LogoUrl = clientePaginaDto.LogoUrl,
                    BannerUrl = clientePaginaDto.BannerUrl,
                    Banner2Url = clientePaginaDto.Banner2Url,
                    Banner3Url = clientePaginaDto.Banner3Url,
                    ModalImageUrl = clientePaginaDto.ModalImageUrl,
                    FaviconUrl = clientePaginaDto.FaviconUrl,
                    MostrarTestimonios = clientePaginaDto.MostrarTestimonios,
                    MostrarServicios = clientePaginaDto.MostrarServicios,
                    MostrarEquipo = clientePaginaDto.MostrarEquipo,
                    MostrarBlog = clientePaginaDto.MostrarBlog,
                    MostrarContacto = clientePaginaDto.MostrarContacto,
                    MostrarMapa = clientePaginaDto.MostrarMapa,
                    MostrarAnimaciones = clientePaginaDto.MostrarAnimaciones,
                    MostrarGalerias = clientePaginaDto.MostrarGalerias,
                    MostrarVideos = clientePaginaDto.MostrarVideos,
                    FacebookUrl = clientePaginaDto.FacebookUrl,
                    InstagramUrl = clientePaginaDto.InstagramUrl,
                    TwitterUrl = clientePaginaDto.TwitterUrl,
                    LinkedInUrl = clientePaginaDto.LinkedInUrl,
                    YouTubeUrl = clientePaginaDto.YouTubeUrl,
                    WhatsAppUrl = clientePaginaDto.WhatsAppUrl,
                    MetaTitulo = clientePaginaDto.MetaTitulo,
                    MetaDescripcion = clientePaginaDto.MetaDescripcion,
                    MetaKeywords = clientePaginaDto.MetaKeywords,
                    CodigoAnalytics = clientePaginaDto.CodigoAnalytics,
                    CodigoHeader = clientePaginaDto.CodigoHeader,
                    CodigoFooter = clientePaginaDto.CodigoFooter,
                    Estado = clientePaginaDto.Estado,
                    EsResponsive = clientePaginaDto.EsResponsive,
                    VelocidadCarga = clientePaginaDto.VelocidadCarga,
                   
                    FechaCreacion = DateTime.Now
                };

                _context.ClientePaginas.Add(pagina);
                await _context.SaveChangesAsync();

                // Agregar servicios personalizados
                if (clientePaginaDto.ServiciosPersonalizados != null && clientePaginaDto.ServiciosPersonalizados.Any())
                {
                    foreach (var servicioDto in clientePaginaDto.ServiciosPersonalizados)
                    {
                        var servicio = new PaginaServicio
                        {
                            PaginaId = pagina.Id,
                            Nombre = servicioDto.Nombre,
                            Descripcion = servicioDto.Descripcion,
                            Icono = servicioDto.Icono,
                            Orden = servicioDto.Orden,
                            Activo = servicioDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaServicios.Add(servicio);
                    }
                }

                // Agregar testimonios personalizados
                if (clientePaginaDto.TestimoniosPersonalizados != null && clientePaginaDto.TestimoniosPersonalizados.Any())
                {
                    foreach (var testimonioDto in clientePaginaDto.TestimoniosPersonalizados)
                    {
                        var testimonio = new PaginaTestimonio
                        {
                            PaginaId = pagina.Id,
                            Nombre = testimonioDto.Nombre,
                            Cargo = testimonioDto.Cargo,
                            Comentario = testimonioDto.Comentario,
                            Calificacion = testimonioDto.Calificacion,
                            FotoUrl = testimonioDto.FotoUrl,
                            Activo = testimonioDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaTestimonios.Add(testimonio);
                    }
                }

                // Agregar galerías
                if (clientePaginaDto.GaleriasImagenes != null && clientePaginaDto.GaleriasImagenes.Any())
                {
                    foreach (var galeriaDto in clientePaginaDto.GaleriasImagenes)
                    {
                        var galeria = new PaginaGaleria
                        {
                            PaginaId = pagina.Id,
                            Titulo = galeriaDto.Titulo,
                            Descripcion = galeriaDto.Descripcion,
                            Orden = galeriaDto.Orden,
                            Activo = galeriaDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaGalerias.Add(galeria);
                        await _context.SaveChangesAsync();

                        // Agregar imágenes de la galería
                        if (galeriaDto.Imagenes != null && galeriaDto.Imagenes.Any())
                        {
                            foreach (var imagenDto in galeriaDto.Imagenes)
                            {
                                var imagen = new PaginaGaleriaImagen
                                {
                                    GaleriaId = galeria.Id,
                                    Url = imagenDto.Url,
                                    Titulo = imagenDto.Titulo,
                                    Descripcion = imagenDto.Descripcion,
                                    Orden = imagenDto.Orden,
                                    CreatedAt = DateTime.Now
                                };
                                _context.PaginaGaleriaImagenes.Add(imagen);
                            }
                        }
                    }
                }

                // Agregar videos
                if (clientePaginaDto.VideosEmbebidos != null && clientePaginaDto.VideosEmbebidos.Any())
                {
                    foreach (var videoDto in clientePaginaDto.VideosEmbebidos)
                    {
                        var video = new PaginaVideo
                        {
                            PaginaId = pagina.Id,
                            Titulo = videoDto.Titulo,
                            Url = videoDto.Url,
                            Descripcion = videoDto.Descripcion,
                            Tipo = videoDto.Tipo,
                            Activo = videoDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaVideos.Add(video);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Cargar relaciones para la respuesta
                await _context.Entry(pagina)
                    .Reference(p => p.Cliente)
                    .LoadAsync();

                await _context.Entry(pagina)
                    .Collection(p => p.ServiciosPersonalizados)
                    .LoadAsync();

                await _context.Entry(pagina)
                    .Collection(p => p.TestimoniosPersonalizados)
                    .LoadAsync();

                await _context.Entry(pagina)
                    .Collection(p => p.GaleriasImagenes)
                    .LoadAsync();

                await _context.Entry(pagina)
                    .Collection(p => p.VideosEmbebidos)
                    .LoadAsync();

                pagina.Link = GenerarLink(pagina);

                return CreatedAtAction(
                    nameof(GetClientePagina),
                    new { id = pagina.Id },
                    new
                    {
                        pagina = pagina,
                        message = "Página creada exitosamente",
                        link = pagina.Link
                    }
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error al crear la página", error = ex.Message });
            }
        }

        // PUT: api/ClientePaginas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClientePagina(int id, ClientePaginaDto clientePaginaDto)
        {
            if (id != clientePaginaDto.Id)
            {
                return BadRequest(new { message = "El ID de la página no coincide" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validar estado
            if (!ClientePagina.EsEstadoValido(clientePaginaDto.Estado))
            {
                return BadRequest(new { message = "Estado inválido. Use 'activo' o 'inactivo'" });
            }

            // Verificar que la página existe
            var paginaExistente = await _context.ClientePaginas
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .Include(p => p.GaleriasImagenes)
                    .ThenInclude(g => g.Imagenes)
                .Include(p => p.VideosEmbebidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paginaExistente == null)
            {
                return NotFound(new { message = $"No se encontró página con ID: {id}" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Console.WriteLine($"📥 Actualizando página ID: {id}");
                Console.WriteLine($"🔍 Servicios en DTO: {clientePaginaDto.ServiciosPersonalizados?.Count ?? 0}");
                Console.WriteLine($"🔍 Testimonios en DTO: {clientePaginaDto.TestimoniosPersonalizados?.Count ?? 0}");

                // ACTUALIZAR CAMPOS DE LA PÁGINA
                // ======================================================
                paginaExistente.Encabezado = clientePaginaDto.Encabezado;
                paginaExistente.Subtitulo = clientePaginaDto.Subtitulo;
                paginaExistente.DescripcionCorta = clientePaginaDto.DescripcionCorta;
                paginaExistente.Cuerpo = clientePaginaDto.Cuerpo;
                paginaExistente.Telefono = clientePaginaDto.Telefono;
                paginaExistente.Email = clientePaginaDto.Email;
                paginaExistente.Direccion = clientePaginaDto.Direccion;
                paginaExistente.HorarioAtencion = clientePaginaDto.HorarioAtencion;
                paginaExistente.ColorFondo = clientePaginaDto.ColorFondo;
                paginaExistente.ColorTexto = clientePaginaDto.ColorTexto;
                paginaExistente.ColorPrimario = clientePaginaDto.ColorPrimario;
                paginaExistente.ColorSecundario = clientePaginaDto.ColorSecundario;
                paginaExistente.ColorAcento = clientePaginaDto.ColorAcento;
                paginaExistente.Tema = clientePaginaDto.Tema;
                paginaExistente.FaviconUrl = clientePaginaDto.FaviconUrl;
                paginaExistente.LogoUrl = clientePaginaDto.LogoUrl;
                paginaExistente.BannerUrl = clientePaginaDto.BannerUrl;
                paginaExistente.Banner2Url = clientePaginaDto.Banner2Url;
                paginaExistente.Banner3Url = clientePaginaDto.Banner3Url;
                paginaExistente.ModalImageUrl = clientePaginaDto.ModalImageUrl;
                paginaExistente.MostrarTestimonios = clientePaginaDto.MostrarTestimonios;
                paginaExistente.MostrarServicios = clientePaginaDto.MostrarServicios;
                paginaExistente.MostrarEquipo = clientePaginaDto.MostrarEquipo;
                paginaExistente.MostrarBlog = clientePaginaDto.MostrarBlog;
                paginaExistente.MostrarContacto = clientePaginaDto.MostrarContacto;
                paginaExistente.MostrarMapa = clientePaginaDto.MostrarMapa;
                paginaExistente.MostrarAnimaciones = clientePaginaDto.MostrarAnimaciones;
                paginaExistente.MostrarGalerias = clientePaginaDto.MostrarGalerias;
                paginaExistente.MostrarVideos = clientePaginaDto.MostrarVideos;
                paginaExistente.FacebookUrl = clientePaginaDto.FacebookUrl;
                paginaExistente.InstagramUrl = clientePaginaDto.InstagramUrl;
                paginaExistente.TwitterUrl = clientePaginaDto.TwitterUrl;
                paginaExistente.LinkedInUrl = clientePaginaDto.LinkedInUrl;
                paginaExistente.YouTubeUrl = clientePaginaDto.YouTubeUrl;
                paginaExistente.WhatsAppUrl = clientePaginaDto.WhatsAppUrl;
                paginaExistente.MetaTitulo = clientePaginaDto.MetaTitulo;
                paginaExistente.MetaDescripcion = clientePaginaDto.MetaDescripcion;
                paginaExistente.MetaKeywords = clientePaginaDto.MetaKeywords;
                paginaExistente.CodigoAnalytics = clientePaginaDto.CodigoAnalytics;
                paginaExistente.CodigoHeader = clientePaginaDto.CodigoHeader;
                paginaExistente.CodigoFooter = clientePaginaDto.CodigoFooter;
                paginaExistente.Estado = clientePaginaDto.Estado;
                paginaExistente.EsResponsive = clientePaginaDto.EsResponsive;
                paginaExistente.VelocidadCarga = clientePaginaDto.VelocidadCarga;

                // ========== ACTUALIZAR SERVICIOS ==========
                if (clientePaginaDto.ServiciosPersonalizados != null)
                {
                    Console.WriteLine("🔄 Procesando servicios...");

                    // Limpiar servicios existentes de esta página
                    var serviciosExistentes = await _context.PaginaServicios
                        .Where(s => s.PaginaId == id)
                        .ToListAsync();

                    if (serviciosExistentes.Any())
                    {
                        Console.WriteLine($"🗑️ Eliminando {serviciosExistentes.Count} servicios existentes");
                        _context.PaginaServicios.RemoveRange(serviciosExistentes);
                        await _context.SaveChangesAsync();
                    }

                    // Agregar los nuevos servicios del DTO
                    foreach (var servicioDto in clientePaginaDto.ServiciosPersonalizados)
                    {
                        var nuevoServicio = new PaginaServicio
                        {
                            PaginaId = id,
                            Nombre = servicioDto.Nombre,
                            Descripcion = servicioDto.Descripcion,
                            Icono = servicioDto.Icono,
                            Orden = servicioDto.Orden,
                            Activo = servicioDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaServicios.Add(nuevoServicio);
                        Console.WriteLine($"✅ Agregando servicio: {servicioDto.Nombre}");
                    }
                }

                // ========== ACTUALIZAR TESTIMONIOS ==========
                if (clientePaginaDto.TestimoniosPersonalizados != null)
                {
                    Console.WriteLine("🔄 Procesando testimonios...");

                    // Limpiar testimonios existentes de esta página
                    var testimoniosExistentes = await _context.PaginaTestimonios
                        .Where(t => t.PaginaId == id)
                        .ToListAsync();

                    if (testimoniosExistentes.Any())
                    {
                        Console.WriteLine($"🗑️ Eliminando {testimoniosExistentes.Count} testimonios existentes");
                        _context.PaginaTestimonios.RemoveRange(testimoniosExistentes);
                        await _context.SaveChangesAsync();
                    }

                    // Agregar los nuevos testimonios del DTO
                    foreach (var testimonioDto in clientePaginaDto.TestimoniosPersonalizados)
                    {
                        var nuevoTestimonio = new PaginaTestimonio
                        {
                            PaginaId = id,
                            Nombre = testimonioDto.Nombre,
                            Cargo = testimonioDto.Cargo,
                            Comentario = testimonioDto.Comentario,
                            Calificacion = testimonioDto.Calificacion,
                            FotoUrl = testimonioDto.FotoUrl,
                            Activo = testimonioDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaTestimonios.Add(nuevoTestimonio);
                        Console.WriteLine($"✅ Agregando testimonio: {testimonioDto.Nombre}");
                    }
                }

                // ========== ACTUALIZAR GALERÍAS E IMÁGENES ==========
                if (clientePaginaDto.GaleriasImagenes != null)
                {
                    Console.WriteLine("🔄 Procesando galerías...");

                    // Limpiar galerías existentes de esta página (y sus imágenes)
                    var galeriasExistentes = await _context.PaginaGalerias
                        .Where(g => g.PaginaId == id)
                        .Include(g => g.Imagenes)
                        .ToListAsync();

                    if (galeriasExistentes.Any())
                    {
                        Console.WriteLine($"🗑️ Eliminando {galeriasExistentes.Count} galerías existentes");

                        // Primero eliminar las imágenes
                        foreach (var galeria in galeriasExistentes)
                        {
                            if (galeria.Imagenes != null && galeria.Imagenes.Any())
                            {
                                _context.PaginaGaleriaImagenes.RemoveRange(galeria.Imagenes);
                            }
                        }
                        await _context.SaveChangesAsync();

                        // Luego eliminar las galerías
                        _context.PaginaGalerias.RemoveRange(galeriasExistentes);
                        await _context.SaveChangesAsync();
                    }

                    // Agregar las nuevas galerías del DTO
                    foreach (var galeriaDto in clientePaginaDto.GaleriasImagenes)
                    {
                        var nuevaGaleria = new PaginaGaleria
                        {
                            PaginaId = id,
                            Titulo = galeriaDto.Titulo,
                            Descripcion = galeriaDto.Descripcion,
                            Orden = galeriaDto.Orden,
                            Activo = galeriaDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaGalerias.Add(nuevaGaleria);
                        await _context.SaveChangesAsync(); // Guardar para obtener el ID

                        Console.WriteLine($"✅ Agregando galería: {galeriaDto.Titulo} (ID: {nuevaGaleria.Id})");

                        // Agregar imágenes de la galería
                        if (galeriaDto.Imagenes != null && galeriaDto.Imagenes.Any())
                        {
                            foreach (var imagenDto in galeriaDto.Imagenes)
                            {
                                var nuevaImagen = new PaginaGaleriaImagen
                                {
                                    GaleriaId = nuevaGaleria.Id,
                                    Url = imagenDto.Url,
                                    Titulo = imagenDto.Titulo,
                                    Descripcion = imagenDto.Descripcion,
                                    Orden = imagenDto.Orden,
                                    CreatedAt = DateTime.Now
                                };
                                _context.PaginaGaleriaImagenes.Add(nuevaImagen);
                            }
                            Console.WriteLine($"   📷 Agregadas {galeriaDto.Imagenes.Count} imágenes");
                        }
                    }
                }

                // ========== ACTUALIZAR VIDEOS ==========
                if (clientePaginaDto.VideosEmbebidos != null)
                {
                    Console.WriteLine("🔄 Procesando videos...");

                    // Limpiar videos existentes de esta página
                    var videosExistentes = await _context.PaginaVideos
                        .Where(v => v.PaginaId == id)
                        .ToListAsync();

                    if (videosExistentes.Any())
                    {
                        Console.WriteLine($"🗑️ Eliminando {videosExistentes.Count} videos existentes");
                        _context.PaginaVideos.RemoveRange(videosExistentes);
                        await _context.SaveChangesAsync();
                    }

                    // Agregar los nuevos videos del DTO
                    foreach (var videoDto in clientePaginaDto.VideosEmbebidos)
                    {
                        var nuevoVideo = new PaginaVideo
                        {
                            PaginaId = id,
                            Titulo = videoDto.Titulo,
                            Url = videoDto.Url,
                            Descripcion = videoDto.Descripcion,
                            Tipo = videoDto.Tipo,
                            Activo = videoDto.Activo,
                            CreatedAt = DateTime.Now
                        };
                        _context.PaginaVideos.Add(nuevoVideo);
                        Console.WriteLine($"✅ Agregando video: {videoDto.Titulo}");
                    }
                }

                // Guardar todos los cambios en la base de datos
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine("✅ Guardado completado exitosamente");

                // Cargar las relaciones actualizadas para retornarlas
                var paginaActualizada = await _context.ClientePaginas
                    .Include(p => p.ServiciosPersonalizados)
                    .Include(p => p.TestimoniosPersonalizados)
                    .Include(p => p.GaleriasImagenes)
                        .ThenInclude(g => g.Imagenes)
                    .Include(p => p.VideosEmbebidos)
                    .FirstOrDefaultAsync(p => p.Id == id);

                // Generar link actualizado
                paginaActualizada.Link = GenerarLink(paginaActualizada);

                Console.WriteLine($"📊 Resultado final:");
                Console.WriteLine($"   • Servicios: {paginaActualizada.ServiciosPersonalizados?.Count ?? 0}");
                Console.WriteLine($"   • Testimonios: {paginaActualizada.TestimoniosPersonalizados?.Count ?? 0}");
                Console.WriteLine($"   • Galerías: {paginaActualizada.GaleriasImagenes?.Count ?? 0}");
                Console.WriteLine($"   • Videos: {paginaActualizada.VideosEmbebidos?.Count ?? 0}");

                return Ok(new
                {
                    message = "✅ Página actualizada exitosamente",
                    pagina = paginaActualizada
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error al actualizar la página: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    message = "Error al actualizar la página",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }


        // PATCH: api/ClientePaginas/5/activar
        [HttpPatch("{id}/activar")]
        public async Task<IActionResult> ActivarPagina(int id)
        {
            var pagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pagina == null)
            {
                return NotFound(new { message = $"No se encontró página con ID: {id}" });
            }

            pagina.Estado = "activo";
            await _context.SaveChangesAsync();

            pagina.Link = GenerarLink(pagina);

            return Ok(new
            {
                message = "Página activada exitosamente",
                link = pagina.Link,
                qrCode = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(pagina.Link)}"
            });
        }

        // PATCH: api/ClientePaginas/5/desactivar
        [HttpPatch("{id}/desactivar")]
        public async Task<IActionResult> DesactivarPagina(int id)
        {
            var pagina = await _context.ClientePaginas.FindAsync(id);
            if (pagina == null)
            {
                return NotFound(new { message = $"No se encontró página con ID: {id}" });
            }

            pagina.Estado = "inactivo";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Página desactivada exitosamente" });
        }

        // GET: api/ClientePaginas/5/link
        [HttpGet("{id}/link")]
        public async Task<ActionResult> GetLinkPagina(int id)
        {
            var pagina = await _context.ClientePaginas
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pagina == null)
            {
                return NotFound(new { message = $"No se encontró página con ID: {id}" });
            }

            var link = GenerarLink(pagina);

            return Ok(new
            {
                link = link,
                qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(link)}",
                clienteId = pagina.ClienteId,
                estado = pagina.Estado
            });
        }

        // GET: api/ClientePaginas/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ClientePagina>>> SearchPaginas(
            [FromQuery] string? estado,
            [FromQuery] int? clienteId,
            [FromQuery] string? encabezado = null)
        {
            var query = _context.ClientePaginas
                .Include(p => p.Cliente)
                .Include(p => p.ServiciosPersonalizados)
                .Include(p => p.TestimoniosPersonalizados)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(p => p.Estado == estado);
            }

            if (clienteId.HasValue)
            {
                query = query.Where(p => p.ClienteId == clienteId.Value);
            }

            if (!string.IsNullOrEmpty(encabezado))
            {
                query = query.Where(p => p.Encabezado.Contains(encabezado));
            }

            var paginas = await query.ToListAsync();

            foreach (var pagina in paginas)
            {
                pagina.Link = GenerarLink(pagina);
            }

            return Ok(paginas);
        }

        // GET: api/ClientePaginas/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetEstadisticas()
        {
            var total = await _context.ClientePaginas.CountAsync();
            var activas = await _context.ClientePaginas.CountAsync(p => p.Estado == "activo");
            var inactivas = await _context.ClientePaginas.CountAsync(p => p.Estado == "inactivo");

            var ultimasCreadas = await _context.ClientePaginas
                .OrderByDescending(p => p.FechaCreacion)
                .Take(5)
                .Include(p => p.Cliente)
                .Select(p => new
                {
                    p.Id,
                    p.Encabezado,
                    p.Estado,
                    p.FechaCreacion,
                    ClienteNombre = p.Cliente!.Nombre
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                activas,
                inactivas,
                ultimasCreadas
            });
        }


        // ========== MÉTODOS PRIVADOS ==========
        private string GenerarLink(ClientePagina pagina)
        {
            var baseUrl = _configuration["AppSettings:BaseUrl"] ??
                         $"{Request.Scheme}://{Request.Host}";

            return $"{baseUrl}/pagina/{pagina.ClienteId}";
        }

        private bool ClientePaginaExists(int id)
        {
            return _context.ClientePaginas.Any(e => e.Id == id);
        }
    }

    // ========== DTOs PARA RECIBIR DATOS ==========
    public class ClientePaginaDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }

        // Información básica
        public string Encabezado { get; set; } = "Bienvenido a mi sitio";
        public string? Subtitulo { get; set; }
        public string? DescripcionCorta { get; set; }
        public string? Cuerpo { get; set; }

        // Información de contacto
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? HorarioAtencion { get; set; }

        // Colores y tema
        public string ColorFondo { get; set; } = "#ffffff";
        public string ColorTexto { get; set; } = "#333333";
        public string ColorPrimario { get; set; } = "#2196f3";
        public string ColorSecundario { get; set; } = "#ff9800";
        public string ColorAcento { get; set; } = "#4caf50";
        public string Tema { get; set; } = "claro";

        // Imágenes
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Banner2Url { get; set; }
        public string? Banner3Url { get; set; }
        public string? FaviconUrl { get; set; }

        public string? ModalImageUrl { get; set; }
        // Configuración de secciones
        public bool MostrarTestimonios { get; set; } = true;
        public bool MostrarServicios { get; set; } = true;
        public bool MostrarEquipo { get; set; } = false;
        public bool MostrarBlog { get; set; } = false;
        public bool MostrarContacto { get; set; } = false;
        public bool MostrarMapa { get; set; } = false;
        public bool MostrarAnimaciones { get; set; } = true;
        public bool MostrarGalerias { get; set; } = true;
        public bool MostrarVideos { get; set; } = true;

        // Redes sociales
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? WhatsAppUrl { get; set; }

        // SEO
        public string? MetaTitulo { get; set; }
        public string? MetaDescripcion { get; set; }
        public string? MetaKeywords { get; set; }

        // Configuración avanzada
        public string? CodigoAnalytics { get; set; }
        public string? CodigoHeader { get; set; }
        public string? CodigoFooter { get; set; }

        // Estado y configuración
        public string Estado { get; set; } = "activo";
        public bool EsResponsive { get; set; } = true;
        public string VelocidadCarga { get; set; } = "normal";

        // Elementos personalizados
        public List<ServicioDto>? ServiciosPersonalizados { get; set; }
        public List<TestimonioDto>? TestimoniosPersonalizados { get; set; }
        public List<GaleriaDto>? GaleriasImagenes { get; set; }
        public List<VideoDto>? VideosEmbebidos { get; set; }
    }

    public class ServicioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Icono { get; set; } = "Settings";
        public int Orden { get; set; } = 0;
        public bool Activo { get; set; } = true;
    }

    public class TestimonioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public int Calificacion { get; set; } = 5;
        public string? FotoUrl { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class GaleriaDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public List<ImagenDto>? Imagenes { get; set; }
    }

    public class ImagenDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public int Orden { get; set; } = 0;
    }

    public class VideoDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Tipo { get; set; } = "youtube";
        public bool Activo { get; set; } = true;
    }
}