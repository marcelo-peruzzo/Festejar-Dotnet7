using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;

namespace Festejar.Respositories
{
    public class Imagens_casasRepository : IImagens_casasRepository
    {
        private readonly AppDbContext _context;

        public Imagens_casasRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Imagens_casas> Imagens_casas => _context.Imagens_casas;

        public List<Imagens_casas> GetAllImagensCasas()
        {
            return _context.Imagens_casas.ToList();
        }
    }
}
