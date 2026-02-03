using System.Reflection.Metadata;
using distels.Models;
namespace distels.Repositories
{
    public interface IParametroRepository
    {
        IEnumerable<Parametro> GetParametros();
        Parametro GetParametro(string id);
        void AddParametro(Parametro parametro);
        bool Guardar();
    }
}
