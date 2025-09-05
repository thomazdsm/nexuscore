using AutoMapper;
using NexusCore.Application.DTOs;
using NexusCore.Application.Interfaces;
using NexusCore.Domain.Entities;
using NexusCore.Domain.Interfaces;
using OpenIddict.Abstractions;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

namespace NexusCore.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IOpenIddictApplicationManager _appManager;

        public ProductService(
            IProductRepository productRepository, 
            IMapper mapper,
            IOpenIddictApplicationManager appManager)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _appManager = appManager;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task CreateProductAsync(ProductDto productDto, string redirectUri, string postLogoutRedirectUri)
        {
            // Gera um ClientId único a partir do nome do produto.
            var clientId = productDto.Name.ToLower().Replace(" ", "-") + "-client";

            // 1. Cria a aplicação OIDC
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                DisplayName = productDto.Name,
                RedirectUris = { new Uri(redirectUri) },
                PostLogoutRedirectUris = { new Uri(postLogoutRedirectUri) },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles
                }
            };

            await _appManager.CreateAsync(descriptor);

            // 2. Busca a aplicação recém-criada para obter seu ID
            var oidcApp = await _appManager.FindByClientIdAsync(clientId);
            if (oidcApp == null)
            {
                throw new InvalidOperationException("Falha ao criar ou encontrar a aplicação OIDC após a criação.");
            }

            // O tipo do identificador agora é um objeto, precisamos extrair o valor.
            var oidcAppId = await _appManager.GetIdAsync(oidcApp);
            if (oidcAppId == null)
            {
                throw new InvalidOperationException("Não foi possível obter o ID da aplicação OIDC criada.");
            }

            // 3. Cria a entidade de negócio Product e a vincula
            var product = _mapper.Map<Product>(productDto);
            product.OpenIddictApplicationId = Guid.Parse(oidcAppId); // Converte e vincula o ID

            await _productRepository.CreateAsync(product);
        }

        public async Task UpdateProductAsync(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            await _productRepository.UpdateAsync(product);
        }

        public async Task UpdateProductWithApplicationAsync(ProductDto productDto, OidcApplicationDto? appDto)
        {
            // Atualiza produto
            var product = _mapper.Map<Product>(productDto);
            await _productRepository.UpdateAsync(product);

            // Atualiza a aplicação OIDC vinculada (se houver)
            if (product.OpenIddictApplicationId.HasValue && appDto != null)
            {
                var existingApp = await _appManager.FindByIdAsync(product.OpenIddictApplicationId.Value.ToString());
                if (existingApp == null)
                    throw new InvalidOperationException("Aplicação OIDC vinculada não encontrada.");

                var descriptor = new OpenIddictApplicationDescriptor();

                await _appManager.PopulateAsync(descriptor, existingApp);

                descriptor.DisplayName = product.Name;

                descriptor.RedirectUris.Clear();
                descriptor.RedirectUris.Add(new Uri(appDto.RedirectUri));

                descriptor.PostLogoutRedirectUris.Clear();
                descriptor.PostLogoutRedirectUris.Add(new Uri(appDto.PostLogoutRedirectUri));

                await _appManager.UpdateAsync(existingApp, descriptor);
            }
        }

        public async Task DeleteProductAsync(Guid id)
        {
            await _productRepository.DeleteAsync(id);
        }

        public async Task<List<object>> GetUnassignedOidcApplicationsAsync()
        {
            var allProducts = await _productRepository.GetAllAsync();
            var assignedAppIds = allProducts
                .Where(p => p.OpenIddictApplicationId.HasValue)
                .Select(p => p.OpenIddictApplicationId.Value.ToString())
                .ToList();

            var unassignedApps = new List<object>();

            // A forma correta de iterar sobre um IAsyncEnumerable
            await foreach (var app in _appManager.ListAsync())
            {
                var appId = await _appManager.GetIdAsync(app);
                if (appId != null && !assignedAppIds.Contains(appId))
                {
                    unassignedApps.Add(new
                    {
                        Id = Guid.Parse(appId),
                        DisplayName = await _appManager.GetDisplayNameAsync(app)
                    });
                }
            }

            return unassignedApps;
        }

        public async Task<OidcApplicationDto?> GetLinkedOidcApplicationAsync(Guid productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product?.OpenIddictApplicationId == null)
            {
                return null;
            }

            var linkedApp = await _appManager.FindByIdAsync(product.OpenIddictApplicationId.Value.ToString());
            if (linkedApp == null)
            {
                return null;
            }

            return new OidcApplicationDto
            {
                Id = await _appManager.GetIdAsync(linkedApp),
                ClientId = await _appManager.GetClientIdAsync(linkedApp),
                DisplayName = await _appManager.GetDisplayNameAsync(linkedApp),
                RedirectUri = string.Join(", ", await _appManager.GetRedirectUrisAsync(linkedApp)),
                PostLogoutRedirectUri = string.Join(", ", await _appManager.GetPostLogoutRedirectUrisAsync(linkedApp))
            };
        }

        public async Task<OidcApplicationDto?> GetOidcApplicationAsync(string OicdId)
        {

            var OidcApplication = await _appManager.FindByIdAsync(OicdId);
            if (OidcApplication == null)
            {
                return null;
            }

            return new OidcApplicationDto
            {
                Id = await _appManager.GetIdAsync(OidcApplication),
                ClientId = await _appManager.GetClientIdAsync(OidcApplication),
                DisplayName = await _appManager.GetDisplayNameAsync(OidcApplication),
                RedirectUri = string.Join(", ", await _appManager.GetRedirectUrisAsync(OidcApplication)),
                PostLogoutRedirectUri = string.Join(", ", await _appManager.GetPostLogoutRedirectUrisAsync(OidcApplication))
            };
        }

        // NOVO MÉTODO PARA DESVINCULAR
        public async Task UnlinkOidcApplicationAsync(Guid productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product != null)
            {
                product.OpenIddictApplicationId = null;
                await _productRepository.UpdateAsync(product);
            }
        }
    }
}