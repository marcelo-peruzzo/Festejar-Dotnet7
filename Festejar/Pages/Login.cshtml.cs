using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class LoginModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPostCancelar()
        {
            return RedirectToPage("Index");
        }

        public IActionResult OnGetRedirectRegister()
        {
            return RedirectToPage ("RegisterLogin");
        }
    }
}
