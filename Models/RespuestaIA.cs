using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("respuestas_ia")]
    public class RespuestaIA
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("llamada_id")]
        public int LlamadaId { get; set; }

        [ForeignKey("LlamadaId")]
        public virtual LlamadaIA Llamada { get; set; } = null!;

        [Column("pregunta_id")]
        [MaxLength(50)]
        public string? PreguntaId { get; set; }

        [Column("pregunta_texto")]
        [MaxLength(500)]
        public string? PreguntaTexto { get; set; }

        [Column("respuesta_cliente")]
        public string? RespuestaCliente { get; set; }

        [Column("categoria_respuesta")]
        [MaxLength(50)]
        public string? CategoriaRespuesta { get; set; } // QUEJA, FELICITACION, CONSULTA, NEUTRAL

        [Column("sentimiento")]
        [MaxLength(20)]
        public string? Sentimiento { get; set; }

        [Column("requiere_seguimiento")]
        public bool RequiereSeguimiento { get; set; } = false;

        [Column("tags")]
        public string[] Tags { get; set; } = Array.Empty<string>();

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}