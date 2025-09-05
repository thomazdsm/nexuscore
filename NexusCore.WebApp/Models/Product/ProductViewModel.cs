using Microsoft.AspNetCore.Mvc.Rendering;
using NexusCore.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace NexusCore.WebApp.Models.Product
{
    public class ProductViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [Display(Name = "Nome do Produto")]
        public string Name { get; set; }

        [Display(Name = "Descrição")]
        public string Description { get; set; }

        //[Required(ErrorMessage = "A URL de redirecionamento é obrigatória.")]
        [Url]
        [Display(Name = "URL de Redirecionamento (após login)")]
        public string? RedirectUri { get; set; }

        //[Required(ErrorMessage = "A URL de pós-logout é obrigatória.")]
        [Url]
        [Display(Name = "URL de Redirecionamento (após logout)")]
        public string? PostLogoutRedirectUri { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Aplicação OpenIddict Vinculada")]
        public Guid? OpenIddictApplicationId { get; set; }
        public OidcApplicationDto? LinkedApplication { get; set; }
        public SelectList? AvailableApplications { get; set; }
    }
}
