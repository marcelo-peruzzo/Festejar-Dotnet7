using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
        private readonly IImagens_casasRepository _imagensCasasRepository;
        private static readonly HttpClient client = new HttpClient();
        private static readonly CultureInfo culture = new CultureInfo("pt-BR");
        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public DadosClientes DadosClientes { get; set; }
        public InternoCasaModel(ICasasRepository casasRepository, UserManager<IdentityUser> userManager, IDiariasRepository diariasRepository, AppDbContext context, IImagens_casasRepository imagensCasasRepository)
        {
            _casasRepository = casasRepository;
            _diariasRepository = diariasRepository;
            _context = context;
            _imagensCasasRepository = imagensCasasRepository;
            _userManager = userManager;
        }
        public Casas InternoCasa { get; set; }
        public List<Casas> Casas { get; set; } = new List<Casas>();
        public List<Imagens_casas> Imagens_casas { get; set; } = new List<Imagens_casas>();
        public string ErroSwal { get; set; }
        [BindProperty]
        public DateTime DataReserva { get; set; }
        public int[] RecrusosReload { get; set; }
        public int[] QuantidadeRecursos { get; set; }
        public string[] ImagensRecursos { get; set; }

        public void OnGet(int id, decimal? valorDiaria, DateTime? dataSelecionada, int[]? recursoId, int[]? quantidade, string? erro)
        {
            Casas = _casasRepository.GetAllCasas();
            var casa = _casasRepository.Casas.FirstOrDefault(casas => casas.Id == id);
            Imagens_casas = _imagensCasasRepository.Imagens_casas.Where(imagem => imagem.Casa_id == id).OrderBy(imagem => imagem.Ordem).ToList();
            ErroSwal = erro;

            foreach (var imagem in Imagens_casas)
            {
                imagem.Caminho = "https://festejar.firo.com.br/storage/" + imagem.Caminho;
            }

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

            //Join para recuperar os recursos das casas acessadas
            var casasRecursos = _context.Casa_recurso
                .Where(cr => cr.Casa_id == id)
                .Join(_context.Recursos,
                    casaRecurso => casaRecurso.Recurso_id, 
                    recursos => recursos.Id,
                    (casaRecurso, recursos) => new { casaRecurso.Recurso_id, recursos.Titulo, recursos.Valor, recursos.Quantidade })
                .GroupBy(grupo => grupo.Recurso_id)
                .Select(g => new { Id = g.First().Recurso_id, Titulo = g.First().Titulo, Valor = g.Sum(x => x.Valor), Quantidade = g.Sum(x => x.Quantidade) });


            //Join para recuperar as imagens de cada recurso das casas acessadas
            var fotosRecursos = _context.Casa_recurso
                .Where(cr => cr.Casa_id == id)
                .Join(_context.Recursos,
                    casaRecurso => casaRecurso.Recurso_id,
                    recursos => recursos.Id,
                    (casaRecurso, recursos) => new { casaRecurso.Recurso_id, recursos.Foto })
                .Select(g => new { Id = g.Recurso_id, Foto = g.Foto });

            ImagensRecursos = fotosRecursos.Select(fr => fr.Foto).ToArray();

            for (int i = 0; i < ImagensRecursos.Length; i++)
            {
                ImagensRecursos[i] = "https://festejar.firo.com.br/storage/" + ImagensRecursos[i];
            }



            //caso o usuario selecione algum recurso e tente reservar uma casa sem fazer login... aqui recupero o tempData dos recursos selecionados passados para razorPage login
            if (TempData["recursoId"] != null && TempData["quantidade"] != null)
            {
                recursoId = JsonConvert.DeserializeObject<int[]>(TempData["recursoId"].ToString());
                quantidade = JsonConvert.DeserializeObject<int[]>(TempData["quantidade"].ToString());
            }

            //verifica se o usuario selecionou algum recurso ao recarregar a pagina selecionando alguma data no metodo PostCheckout
            if (recursoId != null && recursoId.Length > 0 && quantidade != null && quantidade.Length > 0)
            {
                RecrusosReload = recursoId;
                QuantidadeRecursos = quantidade;
            }

            var reservasFuturas = _context.Reservas
                .Where(r => r.Casa_id == casa.Id && r.DataReserva > DateTime.Now)
                .Select(r => r.DataReserva)
                .ToList();

            ViewData["ReservasFuturas"] = reservasFuturas;
            ViewData["Comodidades"] = comodidades;
            ViewData["CasasRecursos"] = casasRecursos;
            
        }

        // metodo envia o usuario para casa selecionada no menu "Casas"
        public IActionResult OnGetRedirectCasa(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }

        //metodo que consulta a API com valores de cada data, e retorna o id da casa, valor da diaria, data selecionada e os recursos selecionados
        public async Task<IActionResult> OnPost(int id, decimal? valorDiaria, int[] recursoId, int[] quantidade)
        {
            DateTime data = DataReserva;
            string start = data.ToString("yyyy-MM-dd");
            string end = data.ToString("yyyy-MM-dd");
            string url = $"https://festejar.firo.com.br/api/RetornaDiasMesByPrioridade?start={start}T00%3A00%3A00-03%3A00&end={end}T00%3A00%3A00-03%3A00";
            try
            {
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
            }
            catch (Exception ex)
            {
                return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicitação HTTP: {ex.Message}" });
            }
            return RedirectToPage(new { id, valorDiaria, dataSelecionada = data, recursoId, quantidade });
        }

        public IActionResult OnPostCheckout(int casaId, DateTime dataReserva, decimal valorDiaria, int convidados, int[] recursoId, int[] quantidade, string returnUrl)
        {

            if (User.Identity.IsAuthenticated)
            {
                var user = _userManager.GetUserAsync(User).Result;

                // Carregar os dados do cliente com base no ID do usuário logado
                DadosClientes = _context.DadosClientes.FirstOrDefault(dc => dc.UserId == user.Id);

                //verifica se o usuario selecionou alguma data e também ja retorna os recursos caso selecionados
                if (dataReserva == DateTime.MinValue)
                {
                    ErroSwal = "Para prosseguir selecione uma dada";
                    return RedirectToPage("/InternoCasa", new { id = casaId, erro = ErroSwal, recursoId, quantidade });
                }

                if (DadosClientes != null)
                {
                    return RedirectToPage("/Checkout", new { casaid = casaId, dataReserva, valorDiaria, convidados, recursoId, quantidade });
                }
                else
                {
                    return RedirectToPage("/DadosDoCliente", new { casaid = casaId, dataReserva, valorDiaria, convidados, recursoId, quantidade });
                }
            }
            else
            {
                return RedirectToPage("Login", new { returnUrl = returnUrl, recursoId, quantidade });
            }

        }

    }
}
