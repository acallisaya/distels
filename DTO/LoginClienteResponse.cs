// DTOs/LoginClienteResponse.cs
namespace distels.DTO
{
    public class LoginClienteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public ClienteDTO? Cliente { get; set; }
        public string? Token { get; set; }
        public SessionData? Session { get; set; }
    }

    public class ClienteDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string? Celular { get; set; }
        public string? Email { get; set; }  // ← Agregar esto
        public string? CodigoAcceso { get; set; }
        public string? EnlaceAcceso { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string Estado { get; set; } = null!;
        // Campo opcional para premium (si lo agregas después)
        public bool EsPremium { get; set; } = false;
        public int? IdVendedorAsignado { get; set; }
    }

    public class SessionData
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string? Celular { get; set; }
        public bool LoggedIn { get; set; }
        public long Timestamp { get; set; }
    }
}