using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Numerics;

namespace distels.Models
{
    [Table("servicios")]
    public class Servicio
    {
        [Key]
        [Column("id_servicio")]
        public int IdServicio { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(50)]
        public string Nombre { get; set; } = null!;

        [Required]
        [Column("codigo")]
        [MaxLength(10)]
        public string Codigo { get; set; } = null!;

        [Column("max_perfiles")]
        public int MaxPerfiles { get; set; } = 1;

        [Column("estado")]
        [MaxLength(10)]
        public string Estado { get; set; } = "ACTIVO";

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones
        public virtual ICollection<Plan> Planes { get; set; } = new List<Plan>();
        public virtual ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    }
}