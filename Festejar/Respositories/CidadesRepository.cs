using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace Festejar.Respositories
{
    public class CidadesRepository : ICidadesRepository
    {
        private readonly AppDbContext _context;
        public CidadesRepository(AppDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Cidades> Cidades => _context.Cidades;
        public List<Cidades> GetAllCidades()
        {
            return _context.Cidades.ToList();
        }
    }
}
