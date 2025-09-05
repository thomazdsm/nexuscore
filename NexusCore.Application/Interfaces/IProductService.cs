using NexusCore.Application.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NexusCore.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<ProductDto> GetProductByIdAsync(Guid id);
        Task CreateProductAsync(ProductDto productDto, string redirectUri, string postLogoutRedirectUri);
        Task UpdateProductAsync(ProductDto productDto);
        Task UpdateProductWithApplicationAsync(ProductDto productDto, OidcApplicationDto? appDto);
        Task DeleteProductAsync(Guid id);
        Task<OidcApplicationDto?> GetLinkedOidcApplicationAsync(Guid productId);
        Task<OidcApplicationDto?> GetOidcApplicationAsync(string OicdId);
        Task UnlinkOidcApplicationAsync(Guid productId);
        Task<List<object>> GetUnassignedOidcApplicationsAsync();
    }
}