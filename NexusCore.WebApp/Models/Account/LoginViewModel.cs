using System.ComponentModel.DataAnnotations;

namespace NexusCore.WebApp.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "O E-mail não é válido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Lembrar de mim")]
        public bool RememberMe { get; set; }

        // Esta propriedade armazenará a URL para onde o usuário deve ser redirecionado após o login
        public string? ReturnUrl { get; set; }
    }

}
