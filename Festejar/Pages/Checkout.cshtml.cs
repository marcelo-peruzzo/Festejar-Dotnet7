using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;

namespace Festejar.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly AppDbContext _context;

        public string NomeCasa { get; set; }
        public DateTime DataReserva { get; set; }
        public decimal ValorDiaria { get; set; }
        public int[] Quantidade { get; set; }
        public string[] Recurso { get; set; }

        public CheckoutModel(ICasasRepository casasRepository, AppDbContext context)
        {
            _casasRepository = casasRepository;
            _context = context;
        }

        public void OnGet(int casaid, DateTime dataReserva, decimal valorDiaria, int[] recursoId, int[] quantidade)
        {
            var casaDeFesta = _casasRepository.Casas.FirstOrDefault(c => c.Id == casaid);
            var recursos = _context.Recursos.Where(r => recursoId.Contains(r.Id)).ToList();
            NomeCasa = casaDeFesta.Titulo;
            DataReserva = dataReserva;
            ValorDiaria = valorDiaria;
            Quantidade = quantidade;
            Recurso = recursos.Select(r => r.Titulo).ToArray();

        }

    }
}
