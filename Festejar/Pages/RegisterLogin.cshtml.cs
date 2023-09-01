using Festejar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class RegistrerLoginModel : PageModel
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        [BindProperty]
        public Logins RegisterInput { get; set; }

        public RegistrerLoginModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostRegister()
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser() { UserName = RegisterInput.UserName };
                var result = await _userManager.CreateAsync(user, RegisterInput.Password);

                if(result.Succeeded)
                {
                    return RedirectToAction("Account"); // FAZER TELA DA CONTA DO USUARIO
                }
                else
                {
                    this.ModelState.AddModelError("Registro", "Falha ao realizar o registro");
                }
            }
            return Page();
        }

        //Metodo que direcionar para a pagina de Login clicada pelas tags <a> no frontend
        public IActionResult OnGetRedirectLogin()
        {
            return RedirectToPage("Login");
        }

        //Metodo que direcionar para a Index clicada pelo botão Cancelar.
        public IActionResult OnPostCancelar()
        {
            return RedirectToPage("Index");
        }
    }
}
