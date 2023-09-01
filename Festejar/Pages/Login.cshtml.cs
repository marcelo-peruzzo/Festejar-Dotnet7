using Festejar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        [BindProperty]
        public Logins LoginInput { get; set; }
        public LoginModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public void OnGetLogin(string returnUrl)
        {
        }

        public async Task<IActionResult> OnPostLogin()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByNameAsync(LoginInput.UserName);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, LoginInput.Password, false, false);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    if (string.IsNullOrEmpty(LoginInput.ReturnUrl))
                    {
                        return RedirectToAction("Index");
                    }
                    return Redirect(LoginInput.ReturnUrl);
                }
            }

            ModelState.AddModelError("", "*Dados incorretos, falha ao realizar login!");
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
            return RedirectToPage ("RegisterLogin");
        }
    }
}
