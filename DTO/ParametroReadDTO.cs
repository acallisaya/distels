using System.ComponentModel.DataAnnotations;

namespace distels.DTO
{
    public class ParametroReadDTO
    {
        public string idparametro { get; set; }
        public string descparametro { get; set; }
        public string tipoparametro { get; set; }
        public string estado { get; set; }
    }
}
