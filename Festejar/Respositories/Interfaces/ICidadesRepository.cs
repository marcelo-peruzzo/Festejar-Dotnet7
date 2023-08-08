using Festejar.Models;

namespace Festejar.Respositories.Interfaces
{
    public interface ICidadesRepository
    {
        public IEnumerable<Cidades> Cidades { get; }

        List<Cidades> GetAllCidades();
    }
}
