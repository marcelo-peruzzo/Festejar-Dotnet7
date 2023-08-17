using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;

namespace Festejar.Pages
{
    public class InternoCasaModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly IDiariasRepository _diariasRepository;
        private static readonly HttpClient client = new HttpClient();
        private static readonly CultureInfo culture = new CultureInfo("pt-BR");
        public InternoCasaModel(ICasasRepository casasRepository, IDiariasRepository diariasRepository)
        {
            _casasRepository = casasRepository;
            _diariasRepository = diariasRepository;
        }
        public Casas InternoCasa { get; set; }

        [BindProperty]
       public DateTime DataReserva { get; set; }

        public void OnGet(int id, decimal? valorDiaria, DateTime? dataSelecionada)
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

            if (dataSelecionada != null)
            {
                ViewData["DataSelecionada"] = dataSelecionada;
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



        public async Task<IActionResult> OnPost(int id, decimal? valorDiaria)
        {
            DateTime data = DataReserva;
            string start = data.ToString("yyyy-MM-dd");
            string end = data.ToString("yyyy-MM-dd");
            string url = $"https://painel.globalprodutos.com/api/RetornaDiasMesByPrioridade?start={start}T00%3A00%3A00-03%3A00&end={end}T00%3A00%3A00-03%3A00";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                if (apiResponse.Events.Count > 0)
                {
                    string title = apiResponse.Events[0].Title;
                    valorDiaria = decimal.Parse(title, NumberStyles.Currency, culture);
                }
            }
            //string valorFormatado = valorDiaria.Value.ToString("C", culture);
            return RedirectToPage(new { id, valorDiaria, dataSelecionada = data });
        }


        public IActionResult OnPostCheckout(int casaId, DateTime dataReserva)
        {
            return RedirectToPage("/Checkout", new { id = casaId, data = dataReserva});
        }

        public class Event
        {
            public string Title { get; set; }
            public string Start { get; set; }
            public string End { get; set; }
            public bool AllDay { get; set; }
        }

        public class ApiResponse
        {
            public List<Event> Events { get; set; }
        }
    }
}
