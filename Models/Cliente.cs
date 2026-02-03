using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("clientes")]
    public class Cliente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nombre")]
        [MaxLength(150)]
        public string Nombre { get; set; } = null!;

        [Required]
        [Column("usuario")]
        [MaxLength(50)]
        public string Usuario { get; set; } = null!;

        [Required]
        [Column("contrasena")]
        [MaxLength(100)]
        public string Contrasena { get; set; } = null!;

        [Column("celular")]
        [MaxLength(20)]
        public string? Celular { get; set; }

        [Column("email")]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Column("codigo_acceso")]
        [MaxLength(50)]
        public string? CodigoAcceso { get; set; }

        [Column("enlace_acceso")]
        public string? EnlaceAcceso { get; set; }

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        [Column("estado")]
        [MaxLength(50)]
        public string Estado { get; set; } = "activo";

        // NUEVOS CAMPOS
        [Column("tipo_cliente")]
        [MaxLength(20)]
        public string TipoCliente { get; set; } = "VENDEDOR";

        [Column("id_vendedor_asignado")]
        public int? IdVendedorAsignado { get; set; }

        // Relaciones existentes
        public virtual ClienteWhatsApp? WhatsApp { get; set; }
        public virtual ClientePagina? Pagina { get; set; }
        public virtual ICollection<Envio>? Envios { get; set; }

        // Nuevas relaciones
        [ForeignKey("IdVendedorAsignado")]
        public virtual Cliente? VendedorAsignado { get; set; }

        public virtual ICollection<Cliente> ClientesAsignados { get; set; } = new List<Cliente>();
        public virtual ICollection<Tarjeta> TarjetasActivadas { get; set; } = new List<Tarjeta>();
        public virtual ICollection<Tarjeta> TarjetasVendidas { get; set; } = new List<Tarjeta>();
        public virtual ICollection<Activacion> Activaciones { get; set; } = new List<Activacion>();
        public virtual ICollection<LlamadaIA> LlamadasRecibidas { get; set; } = new List<LlamadaIA>();
        
        // OPCIONAL: Para llamadas como vendedor
        public virtual ICollection<LlamadaIA> LlamadasRealizadas { get; set; } = new List<LlamadaIA>();
    }
}