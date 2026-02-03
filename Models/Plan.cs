using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("planes")]
    public class Plan
    {
        [Key]
        [Column("id_plan")]
        public int IdPlan { get; set; }

        [Required]
        [Column("id_servicio")]
        public int IdServicio { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(50)]
        public string Nombre { get; set; } = null!;

        [Required]
        [Column("duracion_dias")]
        public int DuracionDias { get; set; }

        [Required]
        [Column("precio_compra")]
        public decimal PrecioCompra { get; set; }

        [Required]
        [Column("precio_venta")]
        public decimal PrecioVenta { get; set; }

        [Column("estado")]
        [MaxLength(10)]
        public string Estado { get; set; } = "ACTIVO";

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        [ForeignKey("IdServicio")]
        public virtual Servicio Servicio { get; set; } = null!;
        public virtual ICollection<Tarjeta> Tarjetas { get; set; } = new List<Tarjeta>();
        // Métodos auxiliares
        [NotMapped]
        public string DuracionTexto
        {
            get
            {
                if (DuracionDias >= 365)
                    return $"{DuracionDias / 365} año{(DuracionDias / 365 > 1 ? "s" : "")}";
                if (DuracionDias >= 30)
                    return $"{DuracionDias / 30} mes{(DuracionDias / 30 > 1 ? "es" : "")}";
                return $"{DuracionDias} día{(DuracionDias > 1 ? "s" : "")}";
            }
        }
    }
}