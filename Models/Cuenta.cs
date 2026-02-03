using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("cuentas")]
    public class Cuenta
    {
        [Key]
        [Column("id_cuenta")]
        public int IdCuenta { get; set; }

        [Required]
        [Column("id_servicio")]
        public int IdServicio { get; set; }

        [Required]
        [Column("usuario")]
        [MaxLength(200)]
        public string Usuario { get; set; } = null!;

        [Required]
        [Column("contrasena")]
        [MaxLength(200)]
        public string Contrasena { get; set; } = null!;

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_ultimo_uso")]
        public DateTime? FechaUltimoUso { get; set; }

        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "DISPONIBLE";

        // Relaciones
        [ForeignKey("IdServicio")]
        public virtual Servicio Servicio { get; set; } = null!;
        public virtual ICollection<Perfil> Perfiles { get; set; } = new List<Perfil>();
    }
}