using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class RegistrerLoginModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnGetRedirectLogin()
        {
            return RedirectToPage("Login");
        }

        public IActionResult OnPostCancelar()
        {
            return RedirectToPage("Index");
        }
    }
}
