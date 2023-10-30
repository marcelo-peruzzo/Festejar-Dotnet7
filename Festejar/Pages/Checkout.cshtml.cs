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
using RestSharp;
using Org.BouncyCastle.Asn1.Crmf;
using Microsoft.EntityFrameworkCore;

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

			//API ASAAS
			var options = new RestClientOptions("https://sandbox.asaas.com/api/v3/customers");
			var clients = new RestClient(options);
			var request = new RestRequest("");
			request.AddHeader("accept", "application/json");
			request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
			request.AddJsonBody($"{{\"name\":\"{DadosClientes.Nome}\",\"cpfCnpj\":\"{DadosClientes.Cpf}\"}}", false);
			var response = await clients.PostAsync(request);
			var asaasResponse = JsonConvert.DeserializeObject<AsaasResponse>(response.Content);
			if (asaasResponse.Id != null)
			{
				DadosClientes.UserId = user.Id;
				DadosClientes.AsaasId = asaasResponse.Id;				
			}

				_context.DadosClientes.Add(DadosClientes);
			await _context.SaveChangesAsync();

			return Page();
		}

		public async Task<IActionResult> OnPostCreateReserva(int casaid, int qntConvidados, DateTime dataConfirm, decimal? valorDiaria, bool aceitaTermo)
		{
			//Verifica se o forms dos dados do cliente não é valido
			if (!ModelState.IsValid)
			{
				// Se o forms dos dados de cliente ñ for valido Recuperar dados da Sessão para exibir no frontend de checkout novamente
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sessão
				int[] recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				int[] quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));
				return Page();
			}
			if (aceitaTermo == true)
			{
				int[] recursoId;
				int[] quantidade;
				var user = await _userManager.GetUserAsync(User);
				DateTime data = dataConfirm;
				string start = data.ToString("yyyy-MM-dd");
				string end = data.ToString("yyyy-MM-dd");
				string url = $"https://festejar.firo.com.br/api/RetornaDiasMesByPrioridade?start={start}T00%3A00%3A00-03%3A00&end={end}T00%3A00%3A00-03%3A00";
				try
				{
					var responses = await client.GetAsync(url);
					if (responses.IsSuccessStatusCode)
					{
						var content = await responses.Content.ReadAsStringAsync();
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

				int reservaId = Reservas.Id; // Obtenha o ID da reserva que acabou de ser criada
				//recupera da sessão os IDs dos recursos e as quantidade
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				//intera no loop sobre a quantidade de recursos que está sendo recuperado para gravar na tabela RecursosReservas 
				for (int i = 0; i < recursoId.Length; i++)
				{
					//monta o objeto para ser gravado na tabela RecursosReservas
					var recursosReserva = new RecursosReservas
					{
						ReservaId = reservaId,
						RecursoId = recursoId[i],
						Quantidade = quantidade[i]
					};
					_context.RecursosReservas.Add(recursosReserva);
				}
				await _context.SaveChangesAsync();

				return RedirectToPage("/MinhasReservas");
			}
			else
			{

				// Se ñ aceitar os termos Recuperar dados da Sessão para exibir no frontend de checkout novamente
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

		public async Task<IActionResult> OnPostEditDataClient(string nome, string cpf, string telefone, string email, string endereco, string cidade, string estado)
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
			// Carrega os dados existentes do cliente a ser editado
			var dadosCliente = await _context.DadosClientes.FirstOrDefaultAsync(dc => dc.UserId == user.Id);
			if (dadosCliente == null)
			{
				return NotFound();
			}

			// Atualiza apenas os campos desejados
			dadosCliente.Nome = nome;
			dadosCliente.Cpf = cpf;
			dadosCliente.Telefone = telefone;
			dadosCliente.Email = email;
			dadosCliente.Cidade = cidade;
			dadosCliente.Estado = estado;
			dadosCliente.Endereco = endereco;

			_context.DadosClientes.Update(dadosCliente);
			await _context.SaveChangesAsync();

			return Page();
		}

	}

	public class AsaasResponse
	{
		public string Object { get; set; }
		public string Id { get; set; }
		public string DateCreated { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
	}

}
