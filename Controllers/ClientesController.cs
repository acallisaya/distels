using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using distels.DTO;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AutoMapper;
using distels.Repositories;
using System.Text.Json;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public ClientesController(
            ApplicationDbContext context,
            IMapper mapper,
            IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        // GET: api/Clientes/tipo/{tipo} ✅ NUEVO ENDPOINT AÑADIDO
        [HttpGet("tipo/{tipo}")]
        public async Task<ActionResult> GetClientesPorTipo(string tipo)
        {
            try
            {
                var clientes = await _context.Clientes
                    .Where(c => c.TipoCliente == tipo && c.Estado == "activo")
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = clientes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.WhatsApp)
                .Include(c => c.Pagina)
                .Include(c => c.Envios)
                .ToListAsync();

            // Ocultar contraseñas para seguridad
            clientes.ForEach(c => c.Contrasena = null);

            return Ok(clientes);
        }

        // GET: api/Clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.WhatsApp)
                .Include(c => c.Pagina)
                .Include(c => c.Envios)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) return NotFound();

            // Ocultar contraseña para seguridad
            cliente.Contrasena = null;

            return Ok(cliente);
        }

        // GET: api/Clientes/buscar?term=valor
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<Cliente>>> BuscarClientes([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                var clientes = await GetClientes();
                return clientes;
            }

            var clientesEncontrados = await _context.Clientes
                .Where(c =>
                    c.Nombre.Contains(term) ||
                    c.Usuario.Contains(term) ||
                    (c.Celular != null && c.Celular.Contains(term)))
                .Include(c => c.WhatsApp)
                .Include(c => c.Pagina)
                .Take(20)
                .ToListAsync();

            // Ocultar contraseñas
            clientesEncontrados.ForEach(c => c.Contrasena = null);

            return Ok(clientesEncontrados);
        }

        // POST: api/Clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            // Validar que el usuario no exista
            var existeUsuario = await _context.Clientes
                .AnyAsync(c => c.Usuario == cliente.Usuario);

            if (existeUsuario)
                return BadRequest(new { message = "El usuario ya existe" });

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Ocultar contraseña en la respuesta
            cliente.Contrasena = null;

            return CreatedAtAction(nameof(GetCliente), new { id = cliente.Id }, cliente);
        }

        // PUT: api/Clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            try
            {
                Console.WriteLine($"🔄 PUT Cliente - ID: {id}");
                Console.WriteLine($"📦 Datos recibidos: {JsonSerializer.Serialize(cliente)}");

                // Asignar ID desde la ruta
                cliente.Id = id;

                // Buscar cliente existente
                var clienteExistente = await _context.Clientes.FindAsync(id);
                if (clienteExistente == null)
                {
                    Console.WriteLine($"❌ Cliente {id} no encontrado");
                    return NotFound(new { message = $"Cliente con ID {id} no encontrado" });
                }

                // Validar unicidad de usuario
                if (clienteExistente.Usuario != cliente.Usuario)
                {
                    var existeUsuario = await _context.Clientes
                        .AnyAsync(c => c.Usuario == cliente.Usuario && c.Id != id);

                    if (existeUsuario)
                    {
                        Console.WriteLine($"❌ Usuario '{cliente.Usuario}' ya existe");
                        return BadRequest(new
                        {
                            message = $"El usuario '{cliente.Usuario}' ya está en uso"
                        });
                    }
                }

                // **¡ESTO ES LO IMPORTANTE!** Mantener contraseña si viene vacía
                if (string.IsNullOrEmpty(cliente.Contrasena))
                {
                    Console.WriteLine($"🔒 Manteniendo contraseña existente (vino vacía o null)");
                    cliente.Contrasena = clienteExistente.Contrasena;
                }
                else
                {
                    Console.WriteLine($"🔑 Cambiando contraseña");
                }

                Console.WriteLine($"📋 Datos antes de actualizar:");
                Console.WriteLine($"   - ID: {cliente.Id}");
                Console.WriteLine($"   - Nombre: {cliente.Nombre}");
                Console.WriteLine($"   - Usuario: {cliente.Usuario}");
                Console.WriteLine($"   - Contraseña: {(string.IsNullOrEmpty(cliente.Contrasena) ? "[MANTENIDA]" : "[NUEVA]")}");
                Console.WriteLine($"   - Celular: {cliente.Celular}");
                Console.WriteLine($"   - Email: {cliente.Email}");
                Console.WriteLine($"   - Estado: {cliente.Estado}");

                // Copiar valores del cliente nuevo al existente
                _context.Entry(clienteExistente).CurrentValues.SetValues(cliente);
                _context.Entry(clienteExistente).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Cliente {id} actualizado correctamente");

                    // Ocultar contraseña en la respuesta
                    clienteExistente.Contrasena = null;

                    return Ok(new
                    {
                        success = true,
                        message = "Cliente actualizado correctamente",
                        cliente = clienteExistente
                    });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"💥 Error de concurrencia: {ex.Message}");
                    if (!ClienteExists(id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Error al guardar: {ex.Message}");
                    Console.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                    return StatusCode(500, new
                    {
                        message = "Error interno del servidor",
                        error = ex.Message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error general en PutCliente: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpGet("{id}/contrasena")]
        public async Task<ActionResult> GetContrasenaCliente(int id)
        {
            try
            {
                var cliente = await _context.Clientes.FindAsync(id);
                if (cliente == null) return NotFound();

                return Ok(new
                {
                    success = true,
                    contrasena = cliente.Contrasena
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/Clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ============================================
        // NUEVOS ENDPOINTS PARA LOGIN DE CLIENTES
        // ============================================

        // GET: api/Clientes/by-usuario/{usuario}
        [HttpGet("by-usuario/{usuario}")]
        public async Task<ActionResult<ClienteDTO>> GetClienteByUsuario(string usuario)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Usuario == usuario)
                .Select(c => new ClienteDTO
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Usuario = c.Usuario,
                    Celular = c.Celular,
                    CodigoAcceso = c.CodigoAcceso,
                    EnlaceAcceso = c.EnlaceAcceso,
                    FechaCreacion = c.FechaCreacion,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Usuario no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                data = cliente
            });
        }

        // POST: api/Clientes/login-cliente
        [HttpPost("login-cliente")]
        public async Task<ActionResult<LoginClienteResponse>> LoginCliente([FromBody] LoginClienteRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Usuario) || string.IsNullOrEmpty(request.Contrasena))
                {
                    return BadRequest(new LoginClienteResponse
                    {
                        Success = false,
                        Message = "Usuario y contraseña son requeridos"
                    });
                }

                // Buscar cliente por usuario
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Usuario == request.Usuario);

                if (cliente == null)
                {
                    return Unauthorized(new LoginClienteResponse
                    {
                        Success = false,
                        Message = "Credenciales incorrectas"
                    });
                }

                // Verificar contraseña
                if (cliente.Contrasena != request.Contrasena)
                {
                    return Unauthorized(new LoginClienteResponse
                    {
                        Success = false,
                        Message = "Credenciales incorrectas"
                    });
                }

                // Verificar estado
                if (cliente.Estado?.ToLower() != "activo")
                {
                    return StatusCode(403, new LoginClienteResponse
                    {
                        Success = false,
                        Message = "Cuenta inactiva. Contacte al administrador."
                    });
                }

                // Generar token JWT
                var token = GenerateJwtToken(cliente);

                // Crear DTO del cliente sin contraseña
                var clienteDto = new ClienteDTO
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Usuario = cliente.Usuario,
                    Celular = cliente.Celular,
                    Email = cliente.Email,
                    CodigoAcceso = cliente.CodigoAcceso,
                    EnlaceAcceso = cliente.EnlaceAcceso,
                    FechaCreacion = cliente.FechaCreacion,
                    Estado = cliente.Estado
                };

                // Datos de sesión
                var sessionData = new SessionData
                {
                    Id = cliente.Id,
                    Nombre = cliente.Nombre,
                    Usuario = cliente.Usuario,
                    Celular = cliente.Celular,
                    LoggedIn = true,
                    Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                };

                return Ok(new LoginClienteResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Cliente = clienteDto,
                    Token = token,
                    Session = sessionData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginClienteResponse
                {
                    Success = false,
                    Message = $"Error interno del servidor: {ex.Message}"
                });
            }
        }

        // Método auxiliar para generar token JWT
        private string GenerateJwtToken(Cliente cliente)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, cliente.Usuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", cliente.Id.ToString()),
                new Claim("nombre", cliente.Nombre),
                new Claim("rol", "cliente"),
                new Claim("tipo", "cliente")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"] ?? "1440")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // GET: api/Clientes/{id}/pagina (para obtener datos de página del cliente)
        [HttpGet("{id}/pagina")]
        public async Task<ActionResult> GetClientePagina(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Pagina)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Cliente no encontrado"
                });
            }

            if (cliente.Pagina == null)
            {
                return Ok(new
                {
                    success = true,
                    data = (object?)null,
                    message = "El cliente no tiene página configurada"
                });
            }

            return Ok(new
            {
                success = true,
                data = cliente.Pagina
            });
        }

        // POST: api/Clientes/{id}/session-validate (para validar sesión)
        [HttpPost("{id}/session-validate")]
        public async Task<ActionResult> ValidateSession(int id, [FromBody] SessionValidationRequest request)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Cliente no encontrado"
                });
            }

            // Verificar si la sesión ha expirado
            var sessionAge = DateTimeOffset.Now.ToUnixTimeMilliseconds() - request.Timestamp;
            var maxAge = 24 * 60 * 60 * 1000; // 24 horas

            if (sessionAge > maxAge)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Sesión expirada"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Sesión válida",
                cliente = new
                {
                    id = cliente.Id,
                    nombre = cliente.Nombre,
                    usuario = cliente.Usuario,
                    celular = cliente.Celular
                }
            });
        }

        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }
    }

    // Clases para validación de sesión
    public class SessionValidationRequest
    {
        public long Timestamp { get; set; }
    }
}