using AutoMapper;
using NexusCore.Application.DTOs;
using NexusCore.WebApp.Models.Product;

namespace NexusCore.WebApp.Mappings
{
    public class ViewModelToDtoMappingProfile : Profile
    {
        public ViewModelToDtoMappingProfile()
        {
            CreateMap<ProductViewModel, ProductDto>();

            // Aplicação OIDC (direto da VM → Dto)
            CreateMap<ProductViewModel, OidcApplicationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.OpenIddictApplicationId.HasValue
                    ? src.OpenIddictApplicationId.Value.ToString()
                    : null))
                .ForMember(dest => dest.RedirectUri, opt => opt.MapFrom(src => src.RedirectUri))
                .ForMember(dest => dest.PostLogoutRedirectUri, opt => opt.MapFrom(src => src.PostLogoutRedirectUri))
                .ForMember(dest => dest.ClientId, opt => opt.Ignore()) // não vem da tela
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore()); // não vem da tela
        }
    }
}
