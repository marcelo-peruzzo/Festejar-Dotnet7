using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICidadesRepository _cidadeRepository;
        private readonly ICasasRepository _casasRepository;
        private readonly IImagens_casasRepository _imagensCasasRepository;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        public IndexModel(ICidadesRepository cidadeRepository, ICasasRepository casasRepository, IImagens_casasRepository imagensCasasRepository, SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _cidadeRepository = cidadeRepository;
            _casasRepository = casasRepository;
            _imagensCasasRepository = imagensCasasRepository;
            _signInManager = signInManager;
            _logger = logger;
        }

        //Lista de cidades consultadas no banco pela interface ICidadesRepository
        public List<Cidades> Cidades { get; set; } = new List<Cidades>();
        public List<Casas> Casas { get; set; } = new List<Casas>();
        public List<Imagens_casas> Imagens_casas { get; set; } = new List<Imagens_casas>();
        public void OnGet()
        {
            Cidades = _cidadeRepository.GetAllCidades();
            Casas = _casasRepository.GetAllCasas();
            Imagens_casas = _imagensCasasRepository.GetAllImagensCasas().OrderBy(imagem => imagem.Ordem).ToList();
            foreach (var imagem in Imagens_casas)
            {
                imagem.Caminho = "https://festejar.firo.com.br/storage/" + imagem.Caminho;
            }
        }

        //Metodo para direcionar até a InternoCasa com base em qual card o usuario clicou na pagina Index.
        public IActionResult OnGetRedirectCasa(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuário desconectado.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }

    }
}