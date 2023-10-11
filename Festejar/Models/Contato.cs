using System.ComponentModel.DataAnnotations;

namespace Festejar.Models
{
    public class Contato
    {
        public int ContatoId { get; set; }

        [Required (ErrorMessage ="*Informe o nome")]
        public string Nome { get; set; }
        [Required(ErrorMessage ="*Informe o e-mail")]
        public string Email { get; set; }
        [Required(ErrorMessage ="*Descreva sua duvida")]
        public string Duvidas { get; set; }
    }
}
