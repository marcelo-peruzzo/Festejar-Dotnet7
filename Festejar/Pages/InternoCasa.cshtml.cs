using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class InternoCasaModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;

        public InternoCasaModel(ICasasRepository casasRepository)
        {
            _casasRepository = casasRepository;
        }
        public Casas InternoCasa { get; set; }
        
        public void OnGet(int id)
        {
            var casa = _casasRepository.Casas.FirstOrDefault(casas => casas.Id == id);

            if (casa != null)
            {
                InternoCasa = new Casas
                {
                    Id = casa.Id,
                    Titulo = casa.Titulo,
                    Status = casa.Status,
                    Capacidade = casa.Capacidade,
                    Cidade_id = casa.Cidade_id,
                    Endereco = casa.Endereco,
                    Telefone = casa.Telefone
                };
            }
        }

        public IActionResult OnPostCheckout(int casaId)
        {
            return RedirectToPage("/Checkout", new { id = casaId });
        }
    }
}
