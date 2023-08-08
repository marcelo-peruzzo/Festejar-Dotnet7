using Festejar.Context;
using Festejar.Models;
using Festejar.Respositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Festejar.Pages
{
    public class CasasModel : PageModel
    {
        private readonly ICidadesRepository _cidadeRepository;
        private readonly ICasasRepository _casasRepository;
        private readonly AppDbContext _context;

        public CasasModel(ICidadesRepository cidadeRepository, ICasasRepository casasRepository, AppDbContext context)
        {
            _cidadeRepository = cidadeRepository;
            _casasRepository = casasRepository;
            _context = context;
        }

        public List<Cidades> Cidades { get; set; } = new List<Cidades>();
        public List<Casas> Casas { get; set; } = new List<Casas>();
        public void OnGet(int cidadeId)
        {
            //var home = new Casas();

            //home.Titulo = _casasRepository.Casas.FirstOrDefault(casa => casa.Cidade_id == cidadeId).Titulo;

            Cidades = _cidadeRepository.GetAllCidades();
            Casas = _casasRepository.GetAllCasas();
            var casaFiltradas = Casas.Where(casa => casa.Cidade_id == cidadeId).ToList();
            var cidadeSelecionada = cidadeId;
            Casas = casaFiltradas;
            Cidades = _cidadeRepository.GetAllCidades();

            //Join entre as tabelas Casa_comodidade e Comodidades com base na igualdade da coluna Id de ambas tabelas...resulta em um objeto que contem casa_id e Titulo e o resultado é agrupado por Casa_id
            var comodidades = _context.Casa_comodidade
                .Where(cc => cc.Casas.Cidade_id == cidadeId)
                .Join(_context.Comodidades,
                    casaComodidade => casaComodidade.Id,
                    comodidades => comodidades.Id,
                    (casaComodidade, comodidades) => new { casaComodidade.Casa_id, comodidades.Titulo })
                .AsEnumerable() // Avalia a consulta no lado do cliente
                .GroupBy(grupo => grupo.Casa_id);

            //objeto IGrouping<int, T> que agrupa as comodidades por Casa_id. Cada grupo tem uma propriedade Key que representa o valor do Casa_id usado no join da variavel comodidades.
            ViewData["Comodidades"] = comodidades;
            //passando o valor de cidadeId PARA viewdata (antigo viewbag dotnet 6 inferior..) para recuperar esse valor na page razor e comparar os dados
            ViewData["CidadeId"] = cidadeId;
        }

        public IActionResult OnPostReservarAgora(int casaId)
        {
            return RedirectToPage("/InternoCasa", new { id = casaId });
        }
    }
}
