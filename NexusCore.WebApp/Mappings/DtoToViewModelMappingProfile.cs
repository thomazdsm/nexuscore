using AutoMapper;
using NexusCore.Application.DTOs;
using NexusCore.WebApp.Models.Product;

namespace NexusCore.WebApp.Mappings
{
    public class DtoToViewModelMappingProfile : Profile
    {
        public DtoToViewModelMappingProfile()
        {
            CreateMap<ProductDto, ProductViewModel>();

            // VM a partir do OIDC (quando carrega no Edit)
            CreateMap<OidcApplicationDto, ProductViewModel>()
                .ForMember(dest => dest.OpenIddictApplicationId, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.Id) ? (Guid?)null : Guid.Parse(src.Id)))
                .ForMember(dest => dest.RedirectUri, opt => opt.MapFrom(src => src.RedirectUri))
                .ForMember(dest => dest.PostLogoutRedirectUri, opt => opt.MapFrom(src => src.PostLogoutRedirectUri));
        }
    }
}
