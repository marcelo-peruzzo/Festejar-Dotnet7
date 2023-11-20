using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
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
    public class MinhasReservasModel : PageModel
    {
		private readonly ICasasRepository _casasRepository;
		private readonly AppDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;

        public List<Casas> Casas { get; set; } = new List<Casas>();
        [BindProperty]
		public List<Reservas> Reservas { get; set; }

		//[BindProperty]
		//public List<Casas> Casa { get; set; }

		[BindProperty]
		public List<RecursosReservas> RecursosReservas { get; set; }

		public MinhasReservasModel(ICasasRepository casasRepository, AppDbContext context, UserManager<IdentityUser> userManager)
		{
			_casasRepository = casasRepository;
			_context = context;
			_userManager = userManager;
		}


        public async Task<IActionResult> OnGet()
        {
            Casas = _casasRepository.GetAllCasas();
            if (User.Identity.IsAuthenticated)
            {               
                var user = _userManager.GetUserAsync(User).Result;
                var userId = user.Id;

                var reserva = await _context.Reservas.Where(r => r.usuarioID == userId).ToListAsync();

				Reservas = new List<Reservas>();
				//Casas = new List<Casas>();

				if (reserva != null && reserva.Any())
                {
					foreach (var reservas in reserva)
					{
						var casaDeFesta = _casasRepository.Casas.FirstOrDefault();
						Casas.Add(new Casas { Titulo = casaDeFesta.Titulo, Endereco = casaDeFesta.Endereco });
						Reservas.Add(new Reservas { Id = reservas.Id, DataReserva = reservas.DataReserva, StatusReserva = reservas.StatusReserva, QuantidadePessoas = reservas.QuantidadePessoas });
					}
				}
				return Page();
            }
            else
            {
				return RedirectToPage("Login");
            }
        }
        public IActionResult OnGetRedirectCasa(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }
    }
}
