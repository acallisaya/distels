using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace distels.DTO
{
    public class GenerarTarjetasAutomaticoDTO
    {
        [Required]
        public int IdPlan { get; set; }

        [Range(1, 1000)]
        public int Cantidad { get; set; } = 10;

        [StringLength(50)]
        public string? PrefijoLote { get; set; }

        public bool AsignacionAutomatica { get; set; } = true;
    }

    public class GenerarTarjetasManualDTO
    {
        [Required]
        public int IdPlan { get; set; }

        [StringLength(50)]
        public string? PrefijoLote { get; set; }

        [Required]
        [MinLength(1)]
        public List<CuentaManualDTO> Cuentas { get; set; } = new List<CuentaManualDTO>();
    }

    public class CuentaManualDTO
    {
        [Required]
        [EmailAddress]
        public string Usuario { get; set; } = null!;

        [Required]
        [MinLength(4)]
        public string Contrasena { get; set; } = null!;

        [Range(1, 10)]
        public int? Perfiles { get; set; } = 1;

        [StringLength(4)]
        public string? Pin { get; set; }

        [StringLength(50)]
        public string? NombrePerfil { get; set; }
    }

    public class RespuestaGeneracionDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int TarjetasGeneradas { get; set; }
        public string Lote { get; set; } = null!;
        public int CuentasCreadas { get; set; }
        public int PerfilesCreados { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public List<DetalleTarjetaDTO>? Detalles { get; set; }
    }

    public class DetalleTarjetaDTO
    {
        public int Numero { get; set; }
        public string Codigo { get; set; } = null!;
        public string Serie { get; set; } = null!;
        public string Lote { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public string Contrasena { get; set; } = null!;
        public string Perfil { get; set; } = null!;
        public string? Pin { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime FechaCreacion { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
    }
}