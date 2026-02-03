using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("activaciones")]
    public class Activacion
    {
        [Key]
        [Column("id_activacion")]
        public int IdActivacion { get; set; }

        [Required]
        [Column("id_tarjeta")]
        public int IdTarjeta { get; set; }

        [Column("id_cliente_final")]
        public int? IdClienteFinal { get; set; }

        // ===== DATOS ENVIADOS =====
        [Required]
        [Column("usuario_enviado")]
        [MaxLength(200)]
        public string UsuarioEnviado { get; set; } = null!;

        [Required]
        [Column("contrasena_enviada")]
        [MaxLength(200)]
        public string ContrasenaEnviada { get; set; } = null!;

        [Column("perfil_enviado")]
        [MaxLength(100)]
        public string? PerfilEnviado { get; set; }

        [Column("pin_enviado")]
        [MaxLength(6)]
        public string? PinEnviado { get; set; }

        // ===== ACTIVACIÓN =====
        [Column("fecha_activacion")]
        public DateTime FechaActivacion { get; set; } = DateTime.Now;

        [Column("ip_activacion")]
        [MaxLength(45)]
        public string? IpActivacion { get; set; }

        [Column("dispositivo")]
        [MaxLength(100)]
        public string? Dispositivo { get; set; }

        [Column("navegador")]
        [MaxLength(100)]
        public string? Navegador { get; set; }

        // ===== ENVÍO =====
        [Column("metodo_envio")]
        [MaxLength(20)]
        public string MetodoEnvio { get; set; } = "WHATSAPP";

        [Column("numero_envio")]
        [MaxLength(20)]
        public string? NumeroEnvio { get; set; }

        [Column("fecha_envio")]
        public DateTime? FechaEnvio { get; set; }

        // ===== CONFIRMACIÓN =====
        [Column("entregado")]
        public bool Entregado { get; set; } = false;

        [Column("fecha_confirmacion")]
        public DateTime? FechaConfirmacion { get; set; }

        // ===== REENVÍOS =====
        [Column("veces_reenviado")]
        public int VecesReenviado { get; set; } = 0;

        [Column("fecha_ultimo_reenvio")]
        public DateTime? FechaUltimoReenvio { get; set; }

        // ===== RELACIONES (CLAVES EXPLÍCITAS) =====
        [ForeignKey(nameof(IdTarjeta))]
        public virtual Tarjeta Tarjeta { get; set; } = null!;

        [ForeignKey(nameof(IdClienteFinal))]
        public virtual Cliente? ClienteFinal { get; set; }
    }
}
