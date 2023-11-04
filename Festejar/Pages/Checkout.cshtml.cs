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
using static Festejar.Pages.CheckoutModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net;

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

		public enum EpaymentType
		{
			CartaoCredito = 1,
			Pix = 2
		}

		int[] recursoId;
		int[] quantidade;

		[BindProperty]
		public EpaymentType PaymentType { get; set; }

		[BindProperty]
		public PagamentoCartaoCredito PagamentoCartaoCredito { get; set; }

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
					// Carregar os dados do cliente com base no ID do usu�rio logado
					DadosClientes = _context.DadosClientes.FirstOrDefault(dc => dc.UserId == user.Id);
				}
			}

			// Armazenar dados na Sess�o
			HttpContext.Session.SetString("NomeCasa", NomeCasa);
			HttpContext.Session.SetString("DataReserva", DataReserva.ToString(culture));
			HttpContext.Session.SetString("ValorDiaria", ValorDiaria.ToString(culture));
			HttpContext.Session.SetString("Casa_Id", Casa_Id.ToString());
			HttpContext.Session.SetString("qntConvidados", qntConvidados.ToString());
			HttpContext.Session.SetString("Recurso", string.Join(",", Recurso));

			// Armazenar arrays de inteiros na Sess�o
			HttpContext.Session.SetString("recursoId", JsonConvert.SerializeObject(recursoId));
			HttpContext.Session.SetString("quantidade", JsonConvert.SerializeObject(quantidade));
		}

		//Metodo que cria o endere�o/dados do reservista vinculando ao UserId
		public async Task<IActionResult> OnPostCreateDataClient()
		{			
			if (!ModelState.IsValid)
			{
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sess�o
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}

			var user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				return NotFound($"N�o foi poss�vel carregar o usu�rio com o ID '{_userManager.GetUserId(User)}'.");
			}

			//API ASAAS
			try {
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
				return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicita��o HTTP: {ex.Message}" });
			}

			NomeCasa = HttpContext.Session.GetString("NomeCasa");
			DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
			ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
			Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
			qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
			Recurso = HttpContext.Session.GetString("Recurso").Split(',');

			// Recupera arrays de inteiros da Sess�o
			recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
			quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

			return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
		}

		public async Task<IActionResult> OnPostEditDataClient(string nome, string cpf, string telefone, string email, string endereco, string cidade, string estado)
		{
			if (!ModelState.IsValid)
			{
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sess�o
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"N�o foi poss�vel carregar o usu�rio com o ID '{_userManager.GetUserId(User)}'.");
			}
			// Cria um objeto dos dados existentes do cliente a ser editado com base no usuario Id logado
			var dadosCliente = await _context.DadosClientes.FirstOrDefaultAsync(dc => dc.UserId == user.Id);
			if (dadosCliente == null)
			{
				return NotFound();
			}

			// Atualiza apenas as informa�oes do cliente, *UserId e AsaasId n�o � alterado*
			dadosCliente.Nome = nome;
			dadosCliente.Cpf = cpf;
			dadosCliente.Telefone = telefone;
			dadosCliente.Email = email;
			dadosCliente.Cidade = cidade;
			dadosCliente.Estado = estado;
			dadosCliente.Endereco = endereco;

			_context.DadosClientes.Update(dadosCliente);
			await _context.SaveChangesAsync();

			//REQUISI��O PUT PARA O ASAAS TMB ATUALIZAR OS DADOS DO CLIENTE NO EDITAR
			try
			{
				var asaasId = dadosCliente.AsaasId; //Recuperar o AsaasId para indexar na url da requisi��o
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
				return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicita��o HTTP: {ex.Message}" });
			}

			NomeCasa = HttpContext.Session.GetString("NomeCasa");
			DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
			ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
			Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
			qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
			Recurso = HttpContext.Session.GetString("Recurso").Split(',');

			// Recupera arrays de inteiros da Sess�o
			recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
			quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

			return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
		}

		public async Task<IActionResult> OnPostCreateReserva(int casaid, int qntConvidados, DateTime dataConfirm, decimal? valorDiaria, string nomeImpressoCartao, string numeroCartao, string validadeCartao, int codigoSeguranca, bool aceitaTermo)
		{
		
			//Verifica se o forms dos dados do cliente n�o � valido
			if (!ModelState.IsValid)
			{
				foreach (var modelStateEntry in ModelState)
				{
					//Verifica os erros do ModelState
					var key = modelStateEntry.Key;
					var errors = modelStateEntry.Value.Errors;

					if (errors.Any())
					{
						Console.WriteLine($"Erros no campo: {key}");
						foreach (var error in errors)
						{
							var errorMessage = error.ErrorMessage;
							Console.WriteLine($"- {errorMessage}");
						}
					}
				}
				// Se o forms dos dados de cliente � for valido Recuperar dados da Sess�o para exibir no frontend de checkout novamente
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sess�o
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));
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
					return RedirectToPage("./Error", new { errorMessage = $"Ocorreu um erro ao fazer a solicita��o HTTP: {ex.Message}" });
				}

				Reservas.QuantidadePessoas = qntConvidados;
				Reservas.Casa_id = casaid;
				Reservas.DataReserva = data;
				Reservas.usuarioID = user.Id;
				Reservas.Valor = (float)valorDiaria;
				Reservas.StatusPagamento = "Pago";
				Reservas.StatusReserva = "reservado";

				var novaCobranca = new GerarCobranca();
				DadosClientes dadosClientes = new DadosClientes();
				string idUserAutenticado = User.FindFirstValue(ClaimTypes.NameIdentifier);
				dadosClientes = await _context.DadosClientes.FirstOrDefaultAsync(dc => dc.UserId == idUserAutenticado);
				//novaCobranca.CriarCobranca(PaymentType, dadosClientes, Reservas.Valor);

				// Chama o m�todo CriarCobranca que retorna um objeto com o resultado
				var result = await novaCobranca.CriarCobranca(PaymentType, dadosClientes, Reservas.Valor);

				//Verifica se o status da cria��o da cobran�a � 200 (OK) e pega o cobran�aId do ASAAS
				if (result is JsonResult jsonResult && jsonResult.StatusCode == (int)System.Net.HttpStatusCode.OK)
				{		
					string cobrancaCriadaId = jsonResult.Value.ToString();
				}


				//_context.Reservas.Add(Reservas);
				//await _context.SaveChangesAsync();

				int reservaId = Reservas.Id; // Obtenha o ID da reserva que acabou de ser criada
				//recupera da sess�o os IDs dos recursos e as quantidade
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				//intera no loop sobre a quantidade de recursos que est� sendo recuperado para gravar na tabela RecursosReservas 
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

				// Se � aceitar os termos Recuperar dados da Sess�o para exibir no frontend de checkout novamente
				NomeCasa = HttpContext.Session.GetString("NomeCasa");
				DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
				ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
				Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
				qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
				Recurso = HttpContext.Session.GetString("Recurso").Split(',');

				// Recupera arrays de inteiros da Sess�o
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}
		}

	}

	//Classe pega a resposta da API asaas cria��o cliente
	public class AsaasResponse
	{
		public string Id { get; set; }
		public string DateCreated { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
	}

	public class Payment 
	{
        public string CustomerId { get; set; }
        public EpaymentType TipoPagamento { get; set; }
        public float Valor { get; set; }
        public string VencimentoCobranca { get; set; }

	}

	public class PagamentoCartaoCredito : Payment
	{
		//[Required(ErrorMessage = "*Informe o nome impresso no cart�o")]
		public string NomeImpressoCartao { get; set; }

		//[Required(ErrorMessage = "*Informe o numero do cart�o")]
		public string NumeroCartao { get; set; }

		//[Required(ErrorMessage = "*Informe o m�s e ano de expira��o do cart�o")]
		public string ValidadeCartao { get; set; }

		//[Required(ErrorMessage = "*Informe o c�digo de seguran�a do cart�o")]
		public string Cvv { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string IpAdress { get; set; }

		//public async Task<RestResponse> Pagar(EpaymentType type, DadosClientes dadosClientes, float valor, string nomeImpressoCartao, string numeroCartao, string validadeCartao, int codigoSeguranca)
		//{
		//	if (type == EpaymentType.CartaoCredito)
		//	{
				
		//		Valor = valor;
		//		VencimentoCobranca = DateTime.Now;

		//		string[] partes = validadeCartao.Split('/');
		//		ExpiryMonth = partes[0]; // "mes"
		//		ExpiryYear = partes[1]; // "ano"
		//		NomeImpressoCartao = nomeImpressoCartao;
		//		NumeroCartao = numeroCartao;
		//		ValidadeCartao = validadeCartao;
		//		Cvv = codigoSeguranca.ToString();


		//		//fazer o pagamento
		//		try {
		//			var options = new RestClientOptions("https://sandbox.asaas.com/api/v3/payments/");
		//			var client = new RestClient(options);
		//			var request = new RestRequest("");
		//			request.AddHeader("accept", "application/json");
		//			request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
		//			request.AddJsonBody($"{{\"billingType\":\"CREDIT_CARD\",\"creditCard\":{{\"holderName\":\"{NomeImpressoCartao}\",\"number\":\"{NumeroCartao}\",\"expiryMonth\":\"{ExpiryMonth}\",\"expiryYear\":\"`{ExpiryYear}\",\"ccv\":\"{Cvv}\"}},\"creditCardHolderInfo\":{{\"name\":\"{dadosClientes.Nome}\",\"email\":\"{dadosClientes.Email}\",\"cpfCnpj\":\"{dadosClientes.Cpf}\",\"postalCode\":\"85640-000\",\"addressNumber\":\"N/A\",\"addressComplement\":null,\"phone\":\"{dadosClientes.Telefone}\",\"mobilePhone\":\"47998781877\"}},\"customer\":\"{dadosClientes.AsaasId}\",\"dueDate\":\"{VencimentoCobranca}\",\"value\"{Valor},\"description\":\"Pedido 056984\",\"externalReference\":\"056984\"}}", false);
		//			var response = await client.PostAsync(request);
		//		}
		//		catch (Exception ex)
		//		{
		//			// Log da exce��o
		//			Console.WriteLine(ex.ToString());

		//			// Retornar uma mensagem de erro para o usu�rio
		//			// Criar uma nova resposta com a mensagem de erro
		//			var errorResponse = new RestResponse
		//			{
		//				Content = "Ocorreu um erro ao processar o pagamento. Por favor, tente novamente mais tarde."
		//			};

		//			return errorResponse;
		//		}


			
		//	}
		//	return null;
		//}



	}

	public class GerarCobranca : Payment
	{
		public async Task<IActionResult> CriarCobranca(EpaymentType type, DadosClientes dadosClientes, float valor)
		{
			TipoPagamento = type;
			Valor = valor;
			VencimentoCobranca = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"); // Formatando a data para o formato MM/YY

			if (TipoPagamento == EpaymentType.CartaoCredito)
			{
				try 
				{
					var options = new RestClientOptions("https://sandbox.asaas.com/api/v3/payments");
					var client = new RestClient(options);
					var request = new RestRequest("");
					request.AddHeader("accept", "application/json");
					request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
					request.AddJsonBody($"{{\"billingType\":\"CREDIT_CARD\",\"customer\":\"{dadosClientes.AsaasId}\",\"value\":{Valor},\"dueDate\":\"{VencimentoCobranca}\"}}", false);
					var response = await client.PostAsync(request);

				
						dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(response.Content);
						Console.WriteLine("Cobran�a criada com sucesso. ID: " + cobrancaCriada.id);
					return new JsonResult(new { StatusCode = response.StatusCode, CobrancaCriadaId = cobrancaCriada.id })
					{
						StatusCode = (int)response.StatusCode,
						Value = cobrancaCriada.id
					};


				}
				catch (Exception ex) 
				{
					// Tratar a exce��o, se necess�rio, e retornar o StatusCode
					Console.WriteLine("Exce��o ao criar a cobran�a: " + ex.Message);
					return new JsonResult(new { StatusCode = System.Net.HttpStatusCode.InternalServerError, Error = ex.Message });
				}
				
				//if (response.StatusCode == System.Net.HttpStatusCode.OK)
				//{
				//	// Cobran�a criada com sucesso
				//	dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(response.Content);
				//	Console.WriteLine("Cobran�a criada com sucesso. ID: " + cobrancaCriada.id);
				//}
				//else
				//{
				//	// Tratar erro ao criar a cobran�a
				//	Console.WriteLine("Erro ao criar a cobran�a: " + response.Content);
				//}

			}
			return new JsonResult(new { StatusCode = System.Net.HttpStatusCode.BadRequest, Error = "Tipo de pagamento inv�lido" });

		}
	}

}
