
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
    }
}
