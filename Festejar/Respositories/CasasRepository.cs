using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;

namespace Festejar.Respositories
{
    public class CasasRepository : ICasasRepository
    {
        private readonly AppDbContext _context;

        public CasasRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Casas> Casas => _context.Casas;

        public List<Casas> GetAllCasas()
        {
            return _context.Casas.ToList();
        }
    }
}
