using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;

namespace Festejar.Respositories
{
    public class DiariasRepository : IDiariasRepository
    {
        private readonly AppDbContext _context;

        public DiariasRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Diarias> Diarias => _context.Diarias;
    }
}
