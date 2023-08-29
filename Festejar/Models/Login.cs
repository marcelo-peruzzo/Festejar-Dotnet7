using System.ComponentModel.DataAnnotations;

namespace Festejar.Models
{
    public class Login
    {
        [Required(ErrorMessage = "Informe o nome")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Informe a senha")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
}
