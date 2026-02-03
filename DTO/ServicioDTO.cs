namespace distels.DTO
{
    public class ServicioDTO
    {
        public int IdServicio { get; set; }
        public string Nombre { get; set; } = null!;
        public string Codigo { get; set; } = null!;
        public int MaxPerfiles { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime? FechaCreacion { get; set; }
        public List<PlanDTO>? Planes { get; set; } = new List<PlanDTO>();
    }
    public class PerfilDTO
    {
        public int IdPerfil { get; set; }
        public int IdCuenta { get; set; }
        public string Nombre { get; set; } = null!;
        public string Pin { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaAsignacion { get; set; }
    }
    public class CrearServicioDTO
    {
        public string Nombre { get; set; } = null!;
        public string Codigo { get; set; } = null!;
        public int MaxPerfiles { get; set; } = 1;
        public string Estado { get; set; } = "ACTIVO";
    }

    public class PlanDTO
    {
        public int IdPlan { get; set; }
        public int IdServicio { get; set; }
        public string Nombre { get; set; } = null!;
        public int DuracionDias { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime? FechaCreacion { get; set; }
        public ServicioDTO? Servicio { get; set; }
    }

    public class CrearPlanDTO
    {
        public int IdServicio { get; set; }
        public string Nombre { get; set; } = null!;
        public int DuracionDias { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public string Estado { get; set; } = "ACTIVO";
    }

    public class GenerarTarjetasDTO
    {
        public int IdPlan { get; set; }
        public int Cantidad { get; set; }
        public string PrefijoLote { get; set; } = "LOT";
        public bool GenerarQR { get; set; } = true;
    }

    public class TarjetaDTO
    {
        public int IdTarjeta { get; set; }
        public int IdPlan { get; set; }
        public int? IdPerfil { get; set; }
        public int? IdClienteActivador { get; set; }
        public int? IdVendedor { get; set; }
        public string Codigo { get; set; } = null!;
        public string Serie { get; set; } = null!;
        public string Lote { get; set; } = null!;
        public DateTime? FechaActivacion { get; set; }
        public string? IpActivacion { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime? FechaCreacion { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public PlanDTO? Plan { get; set; }

        // AGREGAR ESTAS PROPIEDADES:
      
        public PerfilDTO? Perfil { get; set; }        // ← Agregar esto
        public ClienteDTO? Vendedor { get; set; }     // ← Agregar esto
        public ClienteDTO? ClienteActivador { get; set; } // ← Agregar esto

        public string? QRCode { get; set; }
    }

    public class ActivarTarjetaDTO
    {
        public string CodigoTarjeta { get; set; } = null!;
        public string Celular { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MetodoEnvio { get; set; } = "WHATSAPP";
        public string? Dispositivo { get; set; }
        public string? Navegador { get; set; }
    }

    public class CredencialesResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public string? Perfil { get; set; }
        public string? Pin { get; set; }
        public string Servicio { get; set; } = null!;
        public DateTime? FechaVencimiento { get; set; }
    }
}