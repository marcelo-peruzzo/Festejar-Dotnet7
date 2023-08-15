using Festejar.Models;

namespace Festejar.Respositories.Interfaces
{
    public interface IImagens_casasRepository
    {
        IEnumerable<Imagens_casas> Imagens_casas { get; }
        List<Imagens_casas> GetAllImagensCasas();
    }
}
