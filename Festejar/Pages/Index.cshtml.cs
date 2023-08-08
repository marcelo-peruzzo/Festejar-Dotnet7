using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ICidadesRepository _cidadeRepository;

        public IndexModel(ICidadesRepository cidadeRepository)
        {
            _cidadeRepository = cidadeRepository;
        }

        //Lista de cidades consultadas no banco pela interface ICidadesRepository
        public List<Cidades> Cidades { get; set; } = new List<Cidades>();
        public void OnGet()
        {
            Cidades = _cidadeRepository.GetAllCidades();
        }
    }
}