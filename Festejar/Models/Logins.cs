﻿using System.ComponentModel.DataAnnotations;

namespace Festejar.Models
{
    public class Logins
    {
        [Required(ErrorMessage = "*Informe o e-mail")]
        [Display(Name = "E-mail")]
        [EmailAddress]
        public string UserName { get; set; }

        [Required(ErrorMessage = "*Informe a senha")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

		public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }
}
