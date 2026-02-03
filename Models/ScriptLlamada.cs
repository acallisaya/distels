using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("scripts_llamada")]
    public class ScriptLlamada
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("vendedor_id")]
        public int? VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public virtual Cliente? Vendedor { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Required]
        [Column("horas_despues_activacion")]
        public int HorasDespuesActivacion { get; set; } = 24;

        [Column("orden_ejecucion")]
        public int OrdenEjecucion { get; set; } = 1;

        [Required]
        [Column("script_json")]
        public string ScriptJson { get; set; } = "{}";

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("intentos_permitidos")]
        public int IntentosPermitidos { get; set; } = 3;

        [Column("horario_inicio")]
        public TimeSpan HorarioInicio { get; set; } = new TimeSpan(9, 0, 0);

        [Column("horario_fin")]
        public TimeSpan HorarioFin { get; set; } = new TimeSpan(21, 0, 0);

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}