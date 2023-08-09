using Festejar.Models;

namespace Festejar.Respositories.Interfaces
{
    public interface IDiariasRepository
    {
        IEnumerable<Diarias> Diarias { get; }
    }
}
