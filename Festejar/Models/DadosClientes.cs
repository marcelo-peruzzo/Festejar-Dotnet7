using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Festejar.Models
{
    public class DadosClientes
    {
        public int Id { get; set; }

        [ForeignKey("AspNetUsers")]
		[MaxLength(36)]
		public string UserId { get; set; }

        [Required(ErrorMessage = "*Informe o nome")]
        public string Nome { get; set; }

		[Required(ErrorMessage = "*Informe o CPF")]
		public string Cpf { get; set; }

        public string AsaasId { get; set; }

        [Required(ErrorMessage = "*Informe o numero de telefone")]
        [Phone]
		public string Telefone { get; set; }

		[Required(ErrorMessage = "*Informe o E-mail")]
		public string Email { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string Endereco { get; set; }
    }
}
