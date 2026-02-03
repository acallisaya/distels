// DTOs/LoginClienteRequest.cs
using System.ComponentModel.DataAnnotations;

namespace distels.DTO
{
    public class LoginClienteRequest
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string Usuario { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Contrasena { get; set; } = null!;
    }
}