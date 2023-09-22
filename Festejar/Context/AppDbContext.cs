using Festejar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Festejar.Context
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Cidades> Cidades { get; set; }
        public DbSet<Casas> Casas { get; set; }
        public DbSet<Comodidades> Comodidades { get; set; }
        public DbSet<Casa_comodidade> Casa_comodidade { get; set; }
        public DbSet<Diarias> Diarias { get; set; }
        public DbSet<Imagens_casas> Imagens_casas { get; set; }
        public DbSet<Recursos> Recursos { get; set; }
        public DbSet<Casa_recurso> Casa_recurso { get; set; }
        public DbSet<DadosClientes> DadosClientes { get; set; }
    }
}
