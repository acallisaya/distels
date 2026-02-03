using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace distels.Models
{
    [Table("envios")]
    public class Envio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cliente_id")]
        public int ClienteId { get; set; }

        

        [Column("fecha_envio")]
        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        [Column("estado")]
        public string? Medio { get; set; } // WhatsApp, Email, etc.

        [Column("tipo_envio")]
        public string? TipoEnvio { get; set; } // WhatsApp, Email, etc.
    }
}