using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("tarjetas")]
    public class Tarjeta
    {
        [Key]
        [Column("id_tarjeta")]
        public int IdTarjeta { get; set; }

        [Required]
        [Column("id_plan")]
        public int IdPlan { get; set; }

        [Column("id_perfil")]
        public int? IdPerfil { get; set; }

        [Column("id_cliente_activador")]
        public int? IdClienteActivador { get; set; }

        [Column("id_vendedor")]
        public int? IdVendedor { get; set; }

        // Datos de la tarjeta
        [Required]
        [Column("codigo")]
        [MaxLength(15)]
        public string Codigo { get; set; } = null!;

        [Required]
        [Column("serie")]
        [MaxLength(30)]
        public string Serie { get; set; } = null!;

        [Required]
        [Column("lote")]
        [MaxLength(30)]
        public string Lote { get; set; } = null!;

        // Activación
        [Column("fecha_activacion")]
        public DateTime? FechaActivacion { get; set; }

        [Column("ip_activacion")]
        [MaxLength(45)]
        public string? IpActivacion { get; set; }

        // Estado
        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "GENERADA";

        // Fechas
        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_vencimiento")]
        public DateOnly? FechaVencimiento { get; set; }

        // Relaciones
        [ForeignKey("IdPlan")]
        public virtual Plan Plan { get; set; } = null!;

        [ForeignKey("IdPerfil")]
        public virtual Perfil? Perfil { get; set; }

        [ForeignKey("IdClienteActivador")]
        public virtual Cliente? ClienteActivador { get; set; }

        [ForeignKey("IdVendedor")]
        public virtual Cliente? Vendedor { get; set; }

        public virtual ICollection<Activacion> Activaciones { get; set; } = new List<Activacion>();
    }
}
