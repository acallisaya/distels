using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("clientes_whatsapp")]
    public class ClienteWhatsApp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("cliente_id")]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; }

        [Required]
        [Column("whatsapp_number")]
        [StringLength(20)]
        public string WhatsAppNumber { get; set; }

        [Required]
        [StringLength(15)]
        [Column("estado")]
        public string Estado { get; set; } = "activo";

        [Column("imagen_url")]
        public string ImagenUrl { get; set; }

        [Column("imagen_nombre")]
        [StringLength(255)]
        public string ImagenNombre { get; set; }

        [Column("video_url")]
        public string VideoUrl { get; set; }

        [Column("video_nombre")]
        [StringLength(255)]
        public string VideoNombre { get; set; }

        [Column("audio_url")]
        public string AudioUrl { get; set; }

        [Column("audio_nombre")]
        [StringLength(255)]
        public string AudioNombre { get; set; }

        [Column("mensaje_bienvenida")]
        public string MensajeBienvenida { get; set; }

        [Column("mensaje_promocional")]
        public string MensajePromocional { get; set; }

        [Column("permitir_imagenes")]
        public bool PermitirImagenes { get; set; } = true;

        [Column("permitir_videos")]
        public bool PermitirVideos { get; set; } = true;

        [Column("permitir_audios")]
        public bool PermitirAudios { get; set; } = true;

        [Column("permitir_textos")]
        public bool PermitirTextos { get; set; } = true;

        [Column("bot_activo")]
        public bool BotActivo { get; set; } = false;

        [Column("respuesta_automatica")]
        public string RespuestaAutomatica { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_actualizacion")]
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}