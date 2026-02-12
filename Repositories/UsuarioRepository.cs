
using distels.Models;
using Microsoft.EntityFrameworkCore;

namespace distels.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;

        public UsuarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario GetUsuarioByCodigo(string codUsuario, string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return _context.Usuarios
                    .FirstOrDefault(u => u.cod_usuario == codUsuario);
            }

            return _context.Usuarios
                .FirstOrDefault(u => u.cod_usuario == codUsuario &&
                                    u.password == password);
        }

        // 🔴 ¡ESTE MÉTODO ESTÁ FALTANDO!
        public void AddUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
        }

        public bool Guardar()
        {
            return _context.SaveChanges() > 0;
        }
    }
}