using System.ComponentModel.DataAnnotations;

namespace NexusCore.WebApp.Models.Account
{
    public class ProfileViewModel
    {
        [Display(Name = "Nome")]
        public string? FirstName { get; set; }

        [Display(Name = "Sobrenome")]
        public string? LastName { get; set; }

        public string Email { get; set; }
    }

}
