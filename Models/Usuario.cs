using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace distels.Models
{
    [Table("usuarios")] // el nombre exacto de la tabla en PostgreSQL
    public class Usuario
    {
        public int id_usuario { get; set; }
        [Key]
        public string cod_usuario { get; set; }
        public string tipo_rol { get; set; }
        public string password { get; set; }
        public bool estado { get; set; }

               public DateTime fecha_registro { get; set; }
    }
}
