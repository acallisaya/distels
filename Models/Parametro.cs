using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("config_parametros", Schema = "public")]
    public class Parametro
    {
        [Key]
        public string cfg_idparametro { get; set; }
        public string cfg_descparametro { get; set; }
        public DateTime? cfg_desde { get; set; }
        public DateTime? cfg_hasta { get; set; }
        public string? cfg_dato1 { get; set; }
        public string? cfg_dato2 { get; set; }
        public decimal? cfg_numero { get; set; }
        public int? cfg_entero { get; set; }
        public int? cfg_posicion { get; set; }
        public string cfg_tipoparametro { get; set; }
        public string cfg_estado { get; set; }
    }
}
