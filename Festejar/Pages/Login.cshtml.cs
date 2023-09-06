using Festejar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        [BindProperty]
        public Logins LoginInput { get; set; }
        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public void OnGetLogin(string returnUrl)
        {
        }

        public async Task<IActionResult> OnPostLogin(string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                returnUrl ??= Url.Content("~/");

                var result = await _signInManager.PasswordSignInAsync(LoginInput.UserName, LoginInput.Password, LoginInput.RememberMe, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario logado");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dados incorretos, falha ao realizar login!");
                    return Page();
                }
                    //if (string.IsNullOrEmpty(LoginInput.ReturnUrl))
                    //    {
                    //        return RedirectToPage("Index");
                    //    }
                    //    return Redirect(LoginInput.ReturnUrl);
            }
            //ModelState.AddModelError("", "Dados incorretos, falha ao realizar login!");
            return Page();
        }


            //Metodo que direcionar para a index clicada pelo botão Sair no forms de login.
            public IActionResult OnPostCancelar()
            {
                return RedirectToPage("Index");
            }

            //Metodo que direcionar para a pagina RegisterLogin clicada pelas tags <a> no frontend
            public IActionResult OnGetRedirectRegister()
            {
                return RedirectToPage("RegisterLogin");
            }
        }
    } 
