using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Client;

namespace Festejar.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly ICasasRepository _casasRepository;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public string NomeCasa { get; set; }
        public DateTime DataReserva { get; set; }
        public decimal ValorDiaria { get; set; }
        public int[] Quantidade { get; set; }
        public string[] Recurso { get; set; }
        public decimal[] ValorRecurso { get; set; }
        public int qntConvidados { get; set; }

        [BindProperty]
        public DadosClientes DadosClientes { get; set; }

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

    }
}
