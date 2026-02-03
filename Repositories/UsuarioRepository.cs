using distels.Models;
using Microsoft.Extensions.Hosting.Internal;

namespace distels.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;
        public UsuarioRepository(ApplicationDbContext context)
        {
            this._context = context;
        }
        public Usuario GetUsuarioByCodigo(string codUsuario, string password)
        {
            return _context.Usuarios.FirstOrDefault(p => p.cod_usuario == codUsuario && p.password == password);
        }
        public bool Guardar()
        {
            // La DDBB retorna 1 si se guardó correctamente, 0 si no se guardó nada, -1 si hubo un error
            return _context.SaveChanges() >= 0; // Si se guardó al menos un registro, se retorna true
        }
    }
}
