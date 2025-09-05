using System.ComponentModel.DataAnnotations;

namespace NexusCore.WebApp.Models.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "O campo E-mail é obrigatório.")]
        [EmailAddress]
        public string Email { get; set; }
    }

}
