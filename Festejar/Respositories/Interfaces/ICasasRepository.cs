using Festejar.Models;

namespace Festejar.Respositories.Interfaces
{
    public interface ICasasRepository
    {
        IEnumerable<Casas> Casas { get; }
        List<Casas> GetAllCasas();
    }
}
