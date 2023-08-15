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
            //retorna casas com status A que siginifica casas "Ativas" no BD
            return _context.Casas.Where(casa => casa.Status == "A").ToList();
        }
    }
}
