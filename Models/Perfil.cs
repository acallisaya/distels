using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("perfiles")]
    public class Perfil
    {
        [Key]
        [Column("id_perfil")]
        public int IdPerfil { get; set; }

        [Required]
        [Column("id_cuenta")]
        public int IdCuenta { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Column("pin")]
        [MaxLength(6)]
        public string Pin { get; set; } = "";

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "DISPONIBLE";

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_asignacion")]
        public DateTime? FechaAsignacion { get; set; }

        // Relaciones
        [ForeignKey("IdCuenta")]
        public virtual Cuenta Cuenta { get; set; } = null!;
    }
}