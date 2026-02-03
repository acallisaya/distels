using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("llamadas_ia")]
    public class LlamadaIA
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("activacion_id")]
        public int ActivacionId { get; set; }

        [ForeignKey("ActivacionId")]
        public virtual Activacion Activacion { get; set; } = null!;

        [Required]
        [Column("vendedor_id")]
        public int VendedorId { get; set; }

        [ForeignKey("VendedorId")]
        public virtual Cliente Vendedor { get; set; } = null!;

        [Required]
        [Column("cliente_final_id")]
        public int ClienteFinalId { get; set; }

        [ForeignKey("ClienteFinalId")]
        public virtual Cliente ClienteFinal { get; set; } = null!;

        [Required]
        [Column("tipo_llamada")]
        [MaxLength(20)]
        public string TipoLlamada { get; set; } = "SALIENTE"; // SALIENTE, ENTRANTE

        [Required]
        [Column("estado")]
        [MaxLength(20)]
        public string Estado { get; set; } = "PROGRAMADA"; // PROGRAMADA, EN_CURSO, COMPLETADA, FALLIDA, CANCELADA

        [Required]
        [Column("horas_despues_activacion")]
        public int HorasDespuesActivacion { get; set; } = 24;

        [Required]
        [Column("fecha_programada", TypeName = "timestamp without time zone")]
        public DateTime FechaProgramada { get; set; }

        [Column("fecha_ejecucion", TypeName = "timestamp without time zone")]
        public DateTime? FechaEjecucion { get; set; }

        [Column("duracion_segundos")]
        public int? DuracionSegundos { get; set; }

        [Column("resultado")]
        [MaxLength(50)]
        public string? Resultado { get; set; } // CONTESTO, NO_CONTESTO, OCUPADO, ERROR

        [Column("grabacion_url")]
        public string? GrabacionUrl { get; set; }

        [Column("twilio_call_sid")]
        [MaxLength(100)]
        public string? TwilioCallSid { get; set; }

        [Column("intento_numero")]
        public int IntentoNumero { get; set; } = 1;

        [Column("max_intentos")]
        public int MaxIntentos { get; set; } = 3;

        [Column("transcripcion_completa")]
        public string? TranscripcionCompleta { get; set; }

        [Column("sentimiento")]
        [MaxLength(20)]
        public string? Sentimiento { get; set; } // POSITIVO, NEGATIVO, NEUTRAL

        [Column("satisfaccion")]
        public int? Satisfaccion { get; set; } // 1-10

        [Column("fecha_creacion", TypeName = "timestamp without time zone")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_actualizacion", TypeName = "timestamp without time zone")]
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [Column("telefono_destino")]
        [MaxLength(20)]
        public string? TelefonoDestino { get; set; }
        // Relación con RespuestasIA
        public virtual ICollection<RespuestaIA> Respuestas { get; set; } = new List<RespuestaIA>();
    }
}