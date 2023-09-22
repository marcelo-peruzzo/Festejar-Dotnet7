using System.ComponentModel.DataAnnotations.Schema;

namespace Festejar.Models
{
    public class DadosClientes
    {
        public int Id { get; set; }

        [ForeignKey("AspNetUsers")]
        public int UserId { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Endereco { get; set; }
    }
}
