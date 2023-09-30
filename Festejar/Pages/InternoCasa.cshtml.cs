using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http;

namespace Festejar.Pages
{
    public class InternoCasaModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly IDiariasRepository _diariasRepository;
        private readonly AppDbContext _context;
        private static readonly HttpClient client = new HttpClient();
        private static readonly CultureInfo culture = new CultureInfo("pt-BR");
        public InternoCasaModel(ICasasRepository casasRepository, IDiariasRepository diariasRepository, AppDbContext context)
        {
            _casasRepository = casasRepository;
            _diariasRepository = diariasRepository;
            _context = context;
        }
        public Casas InternoCasa { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "*Informe a data desejada")]
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
            //join para listar as comodidades da casa
            var comodidades = _context.Casa_comodidade
                .Where(cc => cc.Casa_id == id)
                .Join(_context.Comodidades,
                    casaComodidade => casaComodidade.Id,
                    comodidades => comodidades.Id,
                    (casaComodidade, comodidades) => new { casaComodidade.Casa_id, comodidades.Titulo })
                .AsEnumerable() // Avalia a consulta no lado do cliente
                .GroupBy(grupo => grupo.Casa_id);

            var casasRecursos = _context.Casa_recurso
                .Where(cr => cr.Casa_id == id)
                .Join(_context.Recursos,
                    casaRecurso => casaRecurso.Id,
                    recursos => recursos.Id,
                    (casaRecurso, recursos) => new { casaRecurso.Recurso_id, recursos.Titulo, recursos.Valor, recursos.Quantidade })
                .GroupBy(grupo => grupo.Recurso_id)
                .Select(g => new { Id = g.First().Recurso_id, Titulo = g.First().Titulo, Valor = g.Sum(x => x.Valor), Quantidade = g.Sum(x => x.Quantidade) });




            ViewData["Comodidades"] = comodidades;
            ViewData["CasasRecursos"] = casasRecursos;
        }



        public async Task<IActionResult> OnPost(int id, decimal? valorDiaria)
        {
            DateTime data = DataReserva;
            string start = data.ToString("yyyy-MM-dd");
            string end = data.ToString("yyyy-MM-dd");
            string url = $"https://festejar.firo.com.br/api/RetornaDiasMesByPrioridade?start={start}T00%3A00%3A00-03%3A00&end={end}T00%3A00%3A00-03%3A00";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                if (apiResponse.Events.Count > 0)
                {
                    string title = apiResponse.Events[0].Title;
                    CultureInfo culture = new CultureInfo("pt-BR");
                    culture.NumberFormat.CurrencyDecimalSeparator = ".";
                    valorDiaria = decimal.Parse(title, NumberStyles.Currency, culture);

                }
            }
            //string valorFormatado = valorDiaria.Value.ToString("C", culture);
            return RedirectToPage(new { id, valorDiaria, dataSelecionada = data });
        }

        public IActionResult OnPostCheckout(int casaId, DateTime dataReserva, decimal valorDiaria, int convidados, int criancas, int[] recursoId, int[] quantidade)
        {
            if(User.Identity.IsAuthenticated)
            {
				return RedirectToPage("/Checkout", new { casaid = casaId, dataReserva, valorDiaria, convidados, criancas, recursoId, quantidade });
            }
            else
            {
                return RedirectToPage("Login");
            }
            
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
