using Festejar.Areas.Identity.Pages.Account;
using Festejar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Festejar.Pages
{
    public class RegistrerLoginModel : PageModel
    {

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegistrerLoginModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public RegisterLogins RegisterInput { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, RegisterInput.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, RegisterInput.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, RegisterInput.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("O usuário criou uma nova conta com senha.");

                    var userId = await _userManager.GetUserIdAsync(user);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                    
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Se chegamos até aqui, algo falhou, reexibir o formulário
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Não é possível criar uma instância de '{nameof(IdentityUser)}'. " +
                    $"Certifique-se de que '{nameof(IdentityUser)}' não seja uma classe abstrata e tenha um construtor sem parâmetros ou, alternativamente, " +
                    $"substituir a página de registro em /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("A UI padrão requer um repositório de usuários com suporte por email.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
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
