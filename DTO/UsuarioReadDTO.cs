using System.ComponentModel.DataAnnotations;
namespace distels.DTO
{
    public class UsuarioReadDTO
    {
        public string Usuario { get; set; }
        public string Password { get; set; }
    }
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = "Login exitoso";

        // Datos del usuario
        public string Usuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
