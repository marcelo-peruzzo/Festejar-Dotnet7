using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using System.Globalization;

namespace Festejar.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
		private static readonly HttpClient client = new HttpClient();
		private static readonly CultureInfo culture = new CultureInfo("pt-BR");
		public string NomeCasa { get; set; }
        public DateTime DataReserva { get; set; }
        public decimal ValorDiaria { get; set; }
        public int[] Quantidade { get; set; }
        public string[] Recurso { get; set; }
        public decimal[] ValorRecurso { get; set; }
        public int qntConvidados { get; set; }
        public int Casa_Id { get; set; }

		[BindProperty]
        public DadosClientes DadosClientes { get; set; }

		[BindProperty]
		public Reservas Reservas { get; set; }

		public CheckoutModel(ICasasRepository casasRepository, AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _casasRepository = casasRepository;
            _context = context;
            _userManager = userManager;
        }

        public void OnGet(int casaid, DateTime dataReserva, decimal valorDiaria, int convidados, int criancas, int[] recursoId, int[] quantidade)
        {
            var casaDeFesta = _casasRepository.Casas.FirstOrDefault(c => c.Id == casaid);
            var recursos = _context.Recursos.Where(r => recursoId.Contains(r.Id)).ToList();
            NomeCasa = casaDeFesta.Titulo;
            DataReserva = dataReserva;
            ValorDiaria = valorDiaria;
            Quantidade = quantidade;
            Casa_Id = casaid;
			qntConvidados = convidados + criancas;
			Recurso = recursos.Select(r => r.Titulo).ToArray();


			// Calcular o valor total dos recursos
			ValorRecurso = new decimal[recursoId.Length];
            for (int i = 0; i < recursoId.Length; i++)
            {
                var recurso = recursos.FirstOrDefault(r => r.Id == recursoId[i]);
                if (recurso != null)
                {
                    ValorRecurso[i] = recurso.Valor * quantidade[i];
                }
            }

			if (User.Identity.IsAuthenticated)
			{
				var user = _userManager.GetUserAsync(User).Result;

				if (user != null)
				{
					// Carregar os dados do cliente com base no ID do usuário logado
					DadosClientes = _context.DadosClientes.FirstOrDefault(dc => dc.UserId == user.Id);
				}
			}

			// Armazenar dados na Sessão
			HttpContext.Session.SetString("NomeCasa", NomeCasa);
			HttpContext.Session.SetString("DataReserva", DataReserva.ToString(culture));
			HttpContext.Session.SetString("ValorDiaria", ValorDiaria.ToString(culture));
			HttpContext.Session.SetString("Casa_Id", Casa_Id.ToString());
			HttpContext.Session.SetString("qntConvidados", qntConvidados.ToString());
			HttpContext.Session.SetString("Recurso", string.Join(",", Recurso));

			// Armazenar arrays de inteiros na Sessão
			HttpContext.Session.SetString("recursoId", JsonConvert.SerializeObject(recursoId));
			HttpContext.Session.SetString("quantidade", JsonConvert.SerializeObject(quantidade));
		}

        //Metodo que cria o endereço/dados do reservista vinculando ao UserId
        public async Task<IActionResult> OnPostCreateDataClient()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com o ID '{_userManager.GetUserId(User)}'.");
            }

            DadosClientes.UserId = user.Id;

            _context.DadosClientes.Add(DadosClientes);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

		public async Task<IActionResult> OnPostCreateReserva(int casaid, int qntConvidados, DateTime dataConfirm, decimal? valorDiaria, bool aceitaTermo)
		{
            if(aceitaTermo == true) { 
			var user = await _userManager.GetUserAsync(User);
			DateTime data = dataConfirm;
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

			Reservas.QuantidadePessoas = qntConvidados;
            Reservas.Casa_id = casaid;
            Reservas.DataReserva = data;
            Reservas.usuarioID = user.Id;
            Reservas.Valor = (decimal)valorDiaria;
            Reservas.StatusPagamento = "Pago";
            Reservas.StatusReserva = "reservado";

            _context.Reservas.Add(Reservas);
           await _context.SaveChangesAsync();
			return RedirectToPage("/MinhasReservas");
            }
            else {

				// Recuperar dados da Sessão
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sessão
				int[] recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				int[] quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}
        }

	}
}
