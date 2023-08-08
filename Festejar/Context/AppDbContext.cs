using Festejar.Models;
using Microsoft.EntityFrameworkCore;

namespace Festejar.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Cidades> Cidades { get; set; }
        public DbSet<Casas> Casas { get; set; }
        public DbSet<Comodidades> Comodidades { get; set; }
        public DbSet<Casa_comodidade> Casa_comodidade { get; set; }
    }
}
