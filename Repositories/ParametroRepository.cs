using System.Reflection.Metadata;
using distels.Models;
namespace distels.Repositories
{
    public class ParametroRepository : IParametroRepository
    {
        private readonly ApplicationDbContext _context;
        public ParametroRepository(ApplicationDbContext context)
        {
            this._context = context;
        }
        public Parametro GetParametro(string id)
        {
            return _context.Parametros.FirstOrDefault(p => p.cfg_idparametro == id);
        }
        public IEnumerable<Parametro> GetParametros()
        {
            return _context.Parametros.ToList();
        }
        public void AddParametro(Parametro parametro)
        {
            if (parametro == null)
                throw new ArgumentNullException(nameof(parametro), "El parametro no puede ser nulo.");
            _context.Parametros.Add(parametro);
            // Cuando se ejecute SaveChanges, se ejecutará algo como: INSERT INTO Estudiante (nombre, apellido, carrera, email) VALUES (@nombre, @apellido, @carrera, @email)
        }
        public bool Guardar()
        {
            // La DDBB retorna 1 si se guardó correctamente, 0 si no se guardó nada, -1 si hubo un error
            return _context.SaveChanges() >= 0; // Si se guardó al menos un registro, se retorna true
        }
    }
}
