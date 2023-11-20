using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Festejar.Pages
{
    public class DadosDoClienteModel : PageModel
    {

		private readonly ICasasRepository _casasRepository;
		private readonly AppDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;
		private static readonly HttpClient client = new HttpClient();
		private static readonly CultureInfo culture = new CultureInfo("pt-BR");

        public List<Casas> Casas { get; set; } = new List<Casas>();
        public string NomeCasa { get; set; }
		public DateTime DataReserva { get; set; }
		public decimal ValorDiaria { get; set; }
		public int[] Quantidade { get; set; }
		public string[] Recurso { get; set; }
		public decimal[] ValorRecurso { get; set; }
		public int qntConvidados { get; set; }
		public int Casa_Id { get; set; }
		public string ErroSwal { get; set; }

		public string SucessoSwal { get; set; }

		public enum EpaymentType
		{
			CartaoCredito = 1,
			Pix = 2
		}

		int[] recursoId;
		int[] quantidade;


		[BindProperty]
		public DadosClientes DadosClientes { get; set; }

		public DadosDoClienteModel(ICasasRepository casasRepository, AppDbContext context, UserManager<IdentityUser> userManager)
		{
			_casasRepository = casasRepository;
			_context = context;
			_userManager = userManager;
		}

		public void OnGet(int casaid, DateTime dataReserva, decimal valorDiaria, int convidados, int[] recursoId, int[] quantidade, string? erro)
		{
            Casas = _casasRepository.GetAllCasas();
            if (User.Identity.IsAuthenticated)
            {
                var user = _userManager.GetUserAsync(User).Result;

                if (user != null)
                {
                    // Carregar os dados do cliente com base no ID do usuário logado
                    DadosClientes = _context.DadosClientes.FirstOrDefault(dc => dc.UserId == user.Id);
                }
			}
			if(casaid != 0)
			{
				var casaDeFesta = _casasRepository.Casas.FirstOrDefault(c => c.Id == casaid);
				var recursos = _context.Recursos.Where(r => recursoId.Contains(r.Id)).ToList();
				NomeCasa = casaDeFesta.Titulo;
				DataReserva = dataReserva;
				ValorDiaria = valorDiaria;
				Quantidade = quantidade;
				Casa_Id = casaid;
				qntConvidados = convidados;
				Recurso = recursos.Select(r => r.Titulo).ToArray();
				ErroSwal = erro;

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
		}

        public IActionResult OnGetRedirectCasa(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }

        //Metodo que cria o endereço/dados do reservista vinculando ao UserId
        public async Task<IActionResult> OnPostCreateDataClient()
		{

			if (!ModelState.IsValid)
			{
				//foreach (var modelStateEntry in ModelState)
				//{
				//	//Verifica os erros do ModelState
				//	var key = modelStateEntry.Key;
				//	var errors = modelStateEntry.Value.Errors;

				//	if (errors.Any())
				//	{
				//		Console.WriteLine($"Erros no campo: {key}");
				//		foreach (var error in errors)
				//		{
				//			var errorMessage = error.ErrorMessage;
				//			Console.WriteLine($"- {errorMessage}");
				//		}
				//	}
				//}

				// Se o forms dos dados de cliente ñ for valido Recuperar dados da Sessão para exibir no frontend de checkout novamente
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sessão
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				var casaDeFesta = _casasRepository.Casas.FirstOrDefault(c => c.Id == Casa_Id);
				var recursos = _context.Recursos.Where(r => recursoId.Contains(r.Id)).ToList();


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
				return Page();
			}

			var user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				return NotFound($"Não foi possível carregar o usuário com o ID '{_userManager.GetUserId(User)}'.");
			}

			//API ASAAS
			try
			{
				var options = new RestClientOptions("https://sandbox.asaas.com/api/v3/customers");
				var clients = new RestClient(options);
				var request = new RestRequest("");
				request.AddHeader("accept", "application/json");
				request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
				request.AddJsonBody($"{{\"name\":\"{DadosClientes.Nome}\",\"email\":\"{DadosClientes.Email}\",\"mobilePhone\":\"{DadosClientes.Telefone}\",\"cpfCnpj\":\"{DadosClientes.Cpf}\"}}", false);
				var response = await clients.PostAsync(request);
				var asaasResponse = JsonConvert.DeserializeObject<AsaasResponse>(response.Content);
				if (asaasResponse.Id != null)
				{
					DadosClientes.UserId = user.Id;
					DadosClientes.AsaasId = asaasResponse.Id;
				}
				_context.DadosClientes.Add(DadosClientes);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicitação HTTP: {ex.Message}" });
			}

			NomeCasa = HttpContext.Session.GetString("NomeCasa");
			DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
			ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
			Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
			qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
			Recurso = HttpContext.Session.GetString("Recurso").Split(',');

			// Recupera arrays de inteiros da Sessão
			recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
			quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

			return Page();
		}

		public async Task<IActionResult> OnPostEditDataClient()
		{
			if (!ModelState.IsValid)
			{
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sessão
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Não foi possível carregar o usuário com o ID '{_userManager.GetUserId(User)}'.");
			}
			// Cria um objeto dos dados existentes do cliente a ser editado com base no usuario Id logado
			var dadosCliente = await _context.DadosClientes.FirstOrDefaultAsync(dc => dc.UserId == user.Id);
			if (dadosCliente == null)
			{
				return NotFound();
			}

			// Atualiza apenas as informaçoes do cliente, *UserId e AsaasId não é alterado*
			dadosCliente.Nome = DadosClientes.Nome;
			dadosCliente.Cpf = DadosClientes.Cpf;
			dadosCliente.Telefone = DadosClientes.Telefone;
			dadosCliente.Email = DadosClientes.Email;
			dadosCliente.Cidade = DadosClientes.Cidade;
			dadosCliente.Estado = DadosClientes.Estado;
			dadosCliente.Endereco = DadosClientes.Endereco;

			_context.DadosClientes.Update(dadosCliente);
			await _context.SaveChangesAsync();

			//REQUISIÇÃO PUT PARA O ASAAS TMB ATUALIZAR OS DADOS DO CLIENTE NO EDITAR
			try
			{
				var asaasId = dadosCliente.AsaasId; //Recuperar o AsaasId para indexar na url da requisição
				var url = $"https://sandbox.asaas.com/api/v3/customers/{asaasId}";
				var options = new RestClientOptions(url);
				var client = new RestClient(options);
				var request = new RestRequest("");
				request.AddHeader("accept", "application/json");
				request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
				request.AddJsonBody($"{{\"name\":\"{dadosCliente.Nome}\",\"email\":\"{dadosCliente.Email}\",\"mobilePhone\":\"{dadosCliente.Telefone}\",\"cpfCnpj\":\"{dadosCliente.Cpf}\"}}", false);
				var response = await client.PutAsync(request);

			}
			catch (Exception ex)
			{
				return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicitação HTTP: {ex.Message}" });
			}

			NomeCasa = HttpContext.Session.GetString("NomeCasa");
			DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
			ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
			Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
			qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
			Recurso = HttpContext.Session.GetString("Recurso").Split(',');

			// Recupera arrays de inteiros da Sessão
			recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
			quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

			return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
		}
	}
}
