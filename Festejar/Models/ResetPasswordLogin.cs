using System.ComponentModel.DataAnnotations;

namespace Festejar.Models
{
    public class ResetPasswordLogin
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }


        [Required]
        [StringLength(100, ErrorMessage = "O {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Confirme a senha")]
        [Compare("Password", ErrorMessage = "A senha e a senha de confirmação não coincidem.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
