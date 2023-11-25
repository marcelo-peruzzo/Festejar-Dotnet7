using Festejar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

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
        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl, int[]? recursoId, int[]? quantidade)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            // Armazenar arrays de inteiros na Sessão
            HttpContext.Session.SetString("recursoId", JsonConvert.SerializeObject(recursoId));
            HttpContext.Session.SetString("quantidade", JsonConvert.SerializeObject(quantidade));

        }

        public async Task<IActionResult> OnPostLogin(string returnUrl, int[]? recursoId, int[]? quantidade)
        {
            if (ModelState.IsValid)
            {
                var recursoIdInSession = HttpContext.Session.GetString("recursoId");
                var quantidadeInSession = HttpContext.Session.GetString("quantidade");
                ReturnUrl = returnUrl;

                //verifica se existe recursos selecionados e a quantidade.
                if (!string.IsNullOrEmpty(recursoIdInSession) && !string.IsNullOrEmpty(quantidadeInSession))
                {
                    recursoId = JsonConvert.DeserializeObject<int[]>(recursoIdInSession);
                    quantidade = JsonConvert.DeserializeObject<int[]>(quantidadeInSession);

                    // Armazenar arrays de inteiros em TempData que usa a sessão por baixo dos panos
                    TempData["recursoId"] = JsonConvert.SerializeObject(recursoId);
                    TempData["quantidade"] = JsonConvert.SerializeObject(quantidade);
                }
              
                var result = await _signInManager.PasswordSignInAsync(LoginInput.UserName, LoginInput.Password, LoginInput.RememberMe, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario logado");

                    return LocalRedirect(ReturnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dados incorretos, falha ao realizar login!");
                    return Page();
                }
            }
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
