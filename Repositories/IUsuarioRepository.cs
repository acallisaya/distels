using distels.Models;

namespace distels.Repositories
{
    public interface IUsuarioRepository
    {
        Usuario GetUsuarioByCodigo(string codUsuario, string password);
        bool Guardar();
    }
}
