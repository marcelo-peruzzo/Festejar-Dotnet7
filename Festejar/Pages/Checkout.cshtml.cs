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
using System.Net.Http.Headers;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure;

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
		public decimal ValorTotal { get; set; }
		public int qntConvidados { get; set; }
		public int Casa_Id { get; set; }
		public string ErroSwal { get; set; }

		public string SucessoSwal { get; set; }

		public enum EpaymentType
		{
			CREDIT_CARD = 1,
			PIX = 2
		}

		int[] recursoId;
		int[] quantidade;

		[BindProperty]
		public EpaymentType PaymentType { get; set; }

		[BindProperty]
		public PagamentoCartaoCredito PagamentoCartaoCredito { get; set; }

		public DadosClientes DadosClientes { get; set; }

		[BindProperty]
		public Reservas Reservas { get; set; }

		public CheckoutModel(ICasasRepository casasRepository, AppDbContext context, UserManager<IdentityUser> userManager)
		{
			_casasRepository = casasRepository;
			_context = context;
			_userManager = userManager;
		}

		public List<Casas> Casas { get; set; } = new List<Casas>();

		public void OnGet(int casaid, DateTime dataReserva, decimal valorDiaria, int convidados, int[] recursoId, int[] quantidade, string? erro, string? encodedImagePix, string? payloadPix)
		{
			Casas = _casasRepository.GetAllCasas();
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

			ViewData["EncodedImage"] = encodedImagePix;
			ViewData["Payload"] = payloadPix;

			if (recursoId != null && recursoId.Length > 0 && quantidade != null && quantidade.Length > 0)
			{
				// Calcular o valor total dos recursos
				decimal somaValorRecurso = 0;
				ValorRecurso = new decimal[recursoId.Length];
				for (int i = 0; i < recursoId.Length; i++)
				{
					var recurso = recursos.FirstOrDefault(r => r.Id == recursoId[i]);
					if (recurso != null)
					{
						ValorRecurso[i] = recurso.Valor * quantidade[i];
						somaValorRecurso += ValorRecurso[i];
					}
				}
				ValorTotal = somaValorRecurso + valorDiaria;
			}
			else
			{
				ValorTotal = valorDiaria;

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



		public async Task<IActionResult> OnPostCreateReserva(int casaid, int qntConvidados, DateTime dataConfirm, decimal? valorDiaria, bool aceitaTermo)
		{
			if (User.Identity.IsAuthenticated)
			{
				var user = _userManager.GetUserAsync(User).Result;

				if (user != null)
				{
					// Carregar os dados do cliente com base no ID do usuário logado
					DadosClientes = _context.DadosClientes.FirstOrDefault(dc => dc.UserId == user.Id);
				}
			}

			//Verifica se o forms dos dados do cliente não é valido
			if (!ModelState.IsValid && PaymentType == EpaymentType.CREDIT_CARD)
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
				Reservas.Valor = (float)valorDiaria;
				Reservas.StatusPagamento = "Pago";
				Reservas.StatusReserva = "reservado";

				var novaCobranca = new GerarCobranca();
				DadosClientes dadosClientes = new DadosClientes();
				string idUserAutenticado = User.FindFirstValue(ClaimTypes.NameIdentifier);
				dadosClientes = await _context.DadosClientes.FirstOrDefaultAsync(dc => dc.UserId == idUserAutenticado);


				// Chama o método CriarCobranca que retorna um objeto com o resultado
				var result = await novaCobranca.CriarCobranca(PaymentType, dadosClientes, Reservas.Valor);

				//Verifica se o status da criação da cobrança é 200 (OK) e pega o cobrançaId do ASAAS
				if (result is JsonResult jsonResult && jsonResult.StatusCode == (int)System.Net.HttpStatusCode.OK)
				{
					var resultadoJsonCobranca = jsonResult.Value as Newtonsoft.Json.Linq.JToken;
					var jsonString = resultadoJsonCobranca.Root.ToString();
					var dadosJson = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
					var billingType = dadosJson["billingType"];
					string cobrancaCriadaId = jsonResult.Value.ToString();
					string cobrancaId = cobrancaCriadaId;

					var billingTypeString = billingType.ToString();
					EpaymentType billingTypeEnum;

					if (Enum.TryParse(billingTypeString, out billingTypeEnum))
					{
						if (billingTypeEnum == EpaymentType.PIX)
						{
							//GERAR QRCode
							var pagarPix = new PagamentoPix();
							var resultadoPagamentoPix = await pagarPix.GerarQRCode(cobrancaId); // Chama o método GerarQRCode()
							var encodedImagePix = resultadoPagamentoPix.EncodedImage;
							var payloadPix = resultadoPagamentoPix.Payload;

							//ViewData["EncodedImage"] = encodedImage;
							//ViewData["Payload"] = payload;

							//montando objeto checkout quando ocorre erro ao efetuar pagamento
							NomeCasa = HttpContext.Session.GetString("NomeCasa");
							DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
							ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
							Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
							qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
							Recurso = HttpContext.Session.GetString("Recurso").Split(',');

							recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
							quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));
							// Retorna a view com os dados
							return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade, encodedImagePix, payloadPix});
						}
						else if (billingTypeEnum == EpaymentType.CREDIT_CARD)
						{
							var pagarCartao = new PagamentoCartaoCredito();
							//Aqui eu instancio um novo objeto de PagamentoCartaoCredito para obter os dados do cartao enviados do formulario
							var dadosDoCartao = new PagamentoCartaoCredito();
			
							//Aqui eu passo os valores do forms para o objeto criado anteriormente
							dadosDoCartao.NomeImpressoCartao = PagamentoCartaoCredito.NomeImpressoCartao;
							dadosDoCartao.NumeroCartao = PagamentoCartaoCredito.NumeroCartao;
							dadosDoCartao.ValidadeCartao = PagamentoCartaoCredito.ValidadeCartao;
							dadosDoCartao.Cvv = PagamentoCartaoCredito.Cvv;
							//chamo o metodo pagar da classe "PagamentoCartaoCredito" passando como parametro
							//os objetos dadosClientes e dadosDoCartao (com os dados requeridos para efetuar um pagamento pelo ASAAS)
							var (sucesso, respostaPagamento) = await pagarCartao.Pagar(PaymentType, dadosClientes, dadosDoCartao, cobrancaId);

							if (sucesso == true)
							{
								var sucessoPgmt = respostaPagamento;
								Console.WriteLine(respostaPagamento);
								_context.Reservas.Add(Reservas);
								await _context.SaveChangesAsync();
							}
							else

							{
								dynamic errorResponse = JsonConvert.DeserializeObject(respostaPagamento);
								string descricaoErro = errorResponse.errors[0].description;
								ErroSwal = descricaoErro;

								//montando objeto checkout quando ocorre erro ao efetuar pagamento
								NomeCasa = HttpContext.Session.GetString("NomeCasa");
								DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
								ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
								Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
								qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
								Recurso = HttpContext.Session.GetString("Recurso").Split(',');

								// Recupera arrays dos recursos e da quantidade mantidos na Sessão
								recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
								quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

								return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade, erro = ErroSwal });
							}
						}
					}
				}
				else
				{
					//erro ao gerar cobrança
					NomeCasa = HttpContext.Session.GetString("NomeCasa");
					DataReserva = DateTime.Parse(HttpContext.Session.GetString("DataReserva"), culture);
					ValorDiaria = decimal.Parse(HttpContext.Session.GetString("ValorDiaria"), culture);
					Casa_Id = int.Parse(HttpContext.Session.GetString("Casa_Id"));
					qntConvidados = int.Parse(HttpContext.Session.GetString("qntConvidados"));
					Recurso = HttpContext.Session.GetString("Recurso").Split(',');

					// Recupera arrays de inteiros da Sessão
					recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
					quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

					ErroSwal = "Ocorreu um erro ao gerar a cobrança";
					return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade, erro = ErroSwal });
				}


				//_context.Reservas.Add(Reservas);
				//await _context.SaveChangesAsync();

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
				recursoId = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("recursoId"));
				quantidade = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("quantidade"));

				return RedirectToPage(new { casaid = Casa_Id, dataReserva = DataReserva, valorDiaria = ValorDiaria, convidados = qntConvidados, recursoId, quantidade });
			}
		}

	}

	//Classe pega a resposta da API asaas criação cliente
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
		public string CobrancaId { get; set; }
		public EpaymentType TipoPagamento { get; set; }
		public float Valor { get; set; }
		public string VencimentoCobranca { get; set; }

	}

	public class PagamentoCartaoCredito : Payment
	{
		[Required(ErrorMessage = "*Informe o nome impresso no cartão")]
		public string NomeImpressoCartao { get; set; }

		[Required(ErrorMessage = "*Informe o numero do cartão")]
		public string NumeroCartao { get; set; }
		public string ExpiryMonth { get; set; }
		public string ExpiryYear { get; set; }

		[Required(ErrorMessage = "*Informe o código de segurança do cartão")]
		public string Cvv { get; set; }

		[Required(ErrorMessage = "*Informe o mês e ano de expiração do cartão")]
		public string ValidadeCartao { get; set; }
		//public string IpAdress { get; set; }

		public async Task<(bool, string)> Pagar(EpaymentType type, DadosClientes dadosClientes, PagamentoCartaoCredito dadosDoCartao, string cobrancaId)
		{
			if (type == EpaymentType.CREDIT_CARD)
			{
				string[] partes = dadosDoCartao.ValidadeCartao.Split('/');
				CobrancaId = cobrancaId;
				ExpiryMonth = partes[0]; // "mes"
				ExpiryYear = partes[1]; // "ano"
				NomeImpressoCartao = dadosDoCartao.NomeImpressoCartao;
				NumeroCartao = dadosDoCartao.NumeroCartao;
				Cvv = dadosDoCartao.Cvv;

				try
				{
					var client = new HttpClient();
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.Add("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");

					var json = $"{{\"creditCard\":{{\"holderName\":\"{NomeImpressoCartao}\",\"number\":\"{NumeroCartao}\",\"expiryMonth\":\"{ExpiryMonth}\",\"expiryYear\":\"{ExpiryYear}\",\"ccv\":\"{Cvv}\"}},\"creditCardHolderInfo\":{{\"name\":\"{dadosClientes.Nome}\",\"email\":\"{dadosClientes.Email}\",\"cpfCnpj\":\"{dadosClientes.Cpf}\",\"postalCode\":\"85640-000\",\"addressNumber\":\"277\",\"addressComplement\":null,\"phone\":\"{dadosClientes.Telefone}\",\"mobilePhone\":\"47998781877\"}}}}";
					var data = new StringContent(json, Encoding.UTF8, "application/json");

					HttpResponseMessage response = await client.PostAsync($"https://sandbox.asaas.com/api/v3/payments/{CobrancaId}/payWithCreditCard", data);

					if (response.IsSuccessStatusCode)
					{
						// Cobrança criada com sucesso
						string responseContent = await response.Content.ReadAsStringAsync();
						dynamic pagamentoCriado = JsonConvert.DeserializeObject<dynamic>(responseContent);
						Console.WriteLine("Pagamento efetuado com sucesso. ID: " + pagamentoCriado.id);
						return (true, pagamentoCriado.id);
					}
					else
					{
						// Tratar erros aqui
						string errorContent = await response.Content.ReadAsStringAsync();
						Console.WriteLine("Erro ao efetuado o pagamento: " + errorContent);
						return (false, errorContent);
					}

				}
				catch (Exception ex)
				{
					Console.WriteLine("Exceção ao fazer a requisição: " + ex.Message);
					return (false, "Erro ao criar a cobrança: " + ex.Message);
				}
			}

			return (false, "Erro ao selecionar forma de pagamento ");
		}
	}


	public class PagamentoPix : Payment
	{
		public string ChavePix { get; set; }
		public string FormatoPix { get; set; }
		public string EncodedImage { get; set; }
		public string Payload { get; set; }
		public HttpStatusCode StatusCode { get; set; }
		public string CobrancaCriadaId { get; set; }

		public async Task<PagamentoPix> GerarQRCode(string cobrancaId)
		{
			CobrancaId = cobrancaId;
			try
			{

				var client = new HttpClient();
				var request = new HttpRequestMessage
				{
					Method = HttpMethod.Get,
					RequestUri = new Uri($"https://sandbox.asaas.com/api/v3/payments/{CobrancaId}/pixQrCode"),
					Headers =
						{
							{ "accept", "application/json" },
							{ "access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==" },
						},
				};
				using (var responsePix = await client.SendAsync(request))
				{
					responsePix.EnsureSuccessStatusCode();
					var body = await responsePix.Content.ReadAsStringAsync();
					Console.WriteLine(body);
					dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(body);

					this.StatusCode = responsePix.StatusCode;
					this.CobrancaCriadaId = cobrancaCriada.id;
					this.EncodedImage = cobrancaCriada.encodedImage;
					this.Payload = cobrancaCriada.payload;

					return this;
				}

				//var clientPix = new HttpClient();
				//var requestPix = new HttpRequestMessage
				//{
				//	Method = HttpMethod.Get,
				//	RequestUri = new Uri("https://sandbox.asaas.com/api/v3/payments/pay_sy1jl5abns8yq8v8/pixQrCode"),
				//	Headers =
				//	{
				//		{ "accept", "application/json" },
				//		{ "access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==" },
				//	},
				//	Content = new StringContent($"{{\"addressKey\":\"221e9827-b6b1-4592-9344-5b9a14a4ef2f\",\"value\":{Valor},\"format\":\"ALL\",\"expirationDate\":\"2024-05-05 14:20:50\",\"allowsMultiplePayments\":false}}")
				//	{
				//		Headers =
				//		{
				//			ContentType = new MediaTypeHeaderValue("application/json")
				//		}
				//	}
				//};
				//using (var responsePix = await clientPix.SendAsync(requestPix))
				//{
				//	responsePix.EnsureSuccessStatusCode();
				//	var body = await responsePix.Content.ReadAsStringAsync();
				//	Console.WriteLine(body);
				//	dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(body);

				//	this.StatusCode = responsePix.StatusCode;
				//	this.CobrancaCriadaId = cobrancaCriada.id;
				//	this.EncodedImage = cobrancaCriada.encodedImage;
				//	this.Payload = cobrancaCriada.payload;

				//	return this;
				//}

			}
			catch (Exception ex)
			{
				// Tratar a exceção
				Console.WriteLine("Exceção ao criar a cobrança: " + ex.Message);
				this.StatusCode = System.Net.HttpStatusCode.InternalServerError;
				return this;
			}
		}
	}







	//Objeto para criar nova COBRANÇA
	public class GerarCobranca : Payment
	{
		public async Task<IActionResult> CriarCobranca(EpaymentType type, DadosClientes dadosClientes, float valor)
		{
			var options = new RestClientOptions("https://sandbox.asaas.com/api/v3/payments");
			var client = new RestClient(options);
			var request = new RestRequest("");

			TipoPagamento = type;
			Valor = valor;
			VencimentoCobranca = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"); // Formatando a data para o formato MM/YY

			if (TipoPagamento == EpaymentType.CREDIT_CARD)
			{
				try
				{
					request.AddHeader("accept", "application/json");
					request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
					request.AddJsonBody($"{{\"billingType\":\"CREDIT_CARD\",\"customer\":\"{dadosClientes.AsaasId}\",\"value\":{Valor},\"dueDate\":\"{VencimentoCobranca}\"}}", false);
					var response = await client.PostAsync(request);


					dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(response.Content);
					Console.WriteLine("Cobrança criada com sucesso. ID: " + cobrancaCriada.id);
					return new JsonResult(new { StatusCode = response.StatusCode, CobrancaCriadaId = cobrancaCriada.id })
					{
						StatusCode = (int)response.StatusCode,
						Value = cobrancaCriada.id
					};


				}
				catch (Exception ex)
				{
					// Tratar a exceção
					Console.WriteLine("Exceção ao criar a cobrança: " + ex.Message);
					return new JsonResult(new { StatusCode = System.Net.HttpStatusCode.InternalServerError, Error = ex.Message });
				}

				//if (response.StatusCode == System.Net.HttpStatusCode.OK)
				//{
				//	// Cobrança criada com sucesso
				//	dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(response.Content);
				//	Console.WriteLine("Cobrança criada com sucesso. ID: " + cobrancaCriada.id);
				//}
				//else
				//{
				//	// Tratar erro ao criar a cobrança
				//	Console.WriteLine("Erro ao criar a cobrança: " + response.Content);
				//}

			}
			if (TipoPagamento == EpaymentType.PIX)
			{

				try
				{
					request.AddHeader("accept", "application/json");
					request.AddHeader("access_token", "$aact_YTU5YTE0M2M2N2I4MTliNzk0YTI5N2U5MzdjNWZmNDQ6OjAwMDAwMDAwMDAwMDAwNjgyODU6OiRhYWNoX2M2MDM0YTVjLWZiOTktNDgzNy1iMjdiLTZiOTE1M2MzYTNmNQ==");
					request.AddJsonBody($"{{\"billingType\":\"PIX\",\"customer\":\"{dadosClientes.AsaasId}\",\"value\":{Valor},\"dueDate\":\"{VencimentoCobranca}\"}}", false);
					var response = await client.PostAsync(request);


					dynamic cobrancaCriada = JsonConvert.DeserializeObject<dynamic>(response.Content);
					Console.WriteLine("Cobrança criada com sucesso. ID: " + cobrancaCriada.id);
					return new JsonResult(new { StatusCode = response.StatusCode, CobrancaCriadaId = cobrancaCriada.id })
					{
						StatusCode = (int)response.StatusCode,
						Value = cobrancaCriada.id
					};


				}
				catch (Exception ex)
				{
					// Tratar a exceção
					Console.WriteLine("Exceção ao criar a cobrança: " + ex.Message);
					return new JsonResult(new { StatusCode = System.Net.HttpStatusCode.InternalServerError, Error = ex.Message });
				}
				
			}
			return new JsonResult(new { StatusCode = System.Net.HttpStatusCode.BadRequest, Error = "Tipo de pagamento inválido" });

		}
	}

}
