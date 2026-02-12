
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Text;
using distels.DTO;
using distels.Models;
using distels.Repositories;


namespace distels.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioRepository _repo;
        private readonly IMapper _mapper;
        public UsuariosController(IUsuarioRepository repo, IMapper mapper)
        {
            this._repo = repo;
            this._mapper = mapper;
        }
        [HttpPost("Login")] // 

        public IActionResult Login([FromBody] UsuarioReadDTO request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Usuario))
                    return BadRequest(new { message = "El usuario es obligatorio" });

                var usuario = _repo.GetUsuarioByCodigo(request.Usuario, request.Password);

                if (usuario == null)
                    return Unauthorized(new { message = "Usuario no encontrado" });

                // ⚡ Aquí iría validación de password si existiera
                // if(request.Password != usuario.Password) return Unauthorized(...);


                var token = GenerateJwtToken(usuario);
                return Ok(new LoginResponse
                {
                    Token = token,
                    Message = "Login exitoso"
                });
            }
            catch (Exception source)
            {

                return Unauthorized(new { message = "Erro del servidor." + source.Message });
            }
        }
        private string GenerateJwtToken(Usuario usuario)
        {
            var jwtSettings = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.cod_usuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("rol", usuario.tipo_rol ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpPost("Registrar")]
        public IActionResult RegistrarUsuario([FromBody] UsuarioRegistroDTO request)
        {
            try
            {
                // Validar que no exista el usuario
                var usuarioExistente = _repo.GetUsuarioByCodigo(request.cod_usuario, null);
                if (usuarioExistente != null)
                    return BadRequest(new
                    {
                        success = false,
                        message = "El nombre de usuario ya existe"
                    });

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    cod_usuario = request.cod_usuario,
                    tipo_rol = request.tipo_rol ?? "VENDEDOR", // Por defecto VENDEDOR
                    password = request.password, // En producción deberías encriptarla
                    estado = true,
                    fecha_registro = DateTime.Now
                };

                _repo.AddUsuario(nuevoUsuario);

                return Ok(new
                {
                    success = true,
                    message = "✅ Usuario creado exitosamente",
                    usuario = new
                    {
                        nuevoUsuario.cod_usuario,
                        nuevoUsuario.tipo_rol,
                        nuevoUsuario.estado
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al crear usuario",
                    error = ex.Message
                });
            }
        }
        // DTO para registrar usuario
        public class UsuarioRegistroDTO
        {
            public string cod_usuario { get; set; } = null!;
            public string? tipo_rol { get; set; }
            public string password { get; set; } = null!;
        }
    }
}
