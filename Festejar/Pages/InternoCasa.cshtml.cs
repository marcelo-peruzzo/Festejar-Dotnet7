using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class InternoCasaModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly IDiariasRepository _diariasRepository;

        public InternoCasaModel(ICasasRepository casasRepository, IDiariasRepository diariasRepository)
        {
            _casasRepository = casasRepository;
            _diariasRepository = diariasRepository;
        }
        public Casas InternoCasa { get; set; }

        [BindProperty]
       public DateTime DataReserva { get; set; }

        public void OnGet(int id, decimal? valorDiaria)
        {
            var casa = _casasRepository.Casas.FirstOrDefault(casas => casas.Id == id);

            if (casa != null)
            {
                InternoCasa = new Casas
                {
                    Id = casa.Id,
                    Titulo = casa.Titulo,
                    Descricao = casa.Descricao,
                    Status = casa.Status,
                    Capacidade = casa.Capacidade,
                    Cidade_id = casa.Cidade_id,
                    Endereco = casa.Endereco,
                    Telefone = casa.Telefone
                };
            }

            if (valorDiaria != null)
            {
                ViewData["ValorDiaria"] = valorDiaria;
            }
            else
            {
                ViewData["ValorDiaria"] = 500;
            }
        }

        public IActionResult OnPost(int id, decimal ?valorDiaria)
        {
            
            DateTime data = DataReserva;

            bool existeDiaria = _diariasRepository.Diarias.Any(diaria => diaria.Casa_id == id &&
                                                  diaria.Ano == data.Year &&
                                                  diaria.Mes == data.Month &&
                                                  diaria.Dia == data.Day &&
                                                  diaria.Deleted_at == null);
            if (existeDiaria)
            {
                var diaria = _diariasRepository.Diarias.First(diaria => diaria.Casa_id == id &&
                                                  diaria.Ano == data.Year &&
                                                  diaria.Mes == data.Month &&
                                                  diaria.Dia == data.Day);

                 valorDiaria = diaria.Valor;
            }

            return RedirectToPage(new { id, valorDiaria });
        }


        public IActionResult OnPostCheckout(int casaId, DateTime dataReserva)
        {
            return RedirectToPage("/Checkout", new { id = casaId, data = dataReserva });
        }
    }
}
