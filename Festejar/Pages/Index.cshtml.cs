using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICidadesRepository _cidadeRepository;
        private readonly ICasasRepository _casasRepository;
        private readonly IImagens_casasRepository _imagensCasasRepository;
        public IndexModel(ICidadesRepository cidadeRepository, ICasasRepository casasRepository, IImagens_casasRepository imagensCasasRepository)
        {
            _cidadeRepository = cidadeRepository;
            _casasRepository = casasRepository;
            _imagensCasasRepository = imagensCasasRepository;
        }

        //Lista de cidades consultadas no banco pela interface ICidadesRepository
        public List<Cidades> Cidades { get; set; } = new List<Cidades>();
        public List<Casas> Casas { get; set; } = new List<Casas>();
        public List<Imagens_casas> Imagens_casas { get; set; } = new List<Imagens_casas>();
        public void OnGet()
        {
            Cidades = _cidadeRepository.GetAllCidades();
            Casas = _casasRepository.GetAllCasas();
            Imagens_casas = _imagensCasasRepository.GetAllImagensCasas();
        }

        //Metodo para direcionar até a InternoCasa com base em qual card o usuario clicou na pagina Index.
        public IActionResult OnGetRedirectCasa(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }

    }
}