using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NexusCore.Domain.Entities;
using NexusCore.Infra.Data.Context;
using OpenIddict.Abstractions;
using System;
using System.Threading.Tasks;

namespace NexusCore.Infra.Data.Seed
{
    public static class SeedData
    {
        public static async Task EnsureSeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var appManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Cadastra os escopos... (código dos escopos sem alteração)
            if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.OpenId) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.OpenId });
            }
            if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Profile) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Profile });
            }
            if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Email) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Email });
            }
            if (await scopeManager.FindByNameAsync(OpenIddictConstants.Scopes.Roles) is null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Roles });
            }

            // Cria a aplicação cliente "Academe"
            if (await appManager.FindByClientIdAsync("academe-client") is null)
            {
                await appManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = "academe-client",
                    ClientSecret = "E84B5A4B-A353-4859-8A33-241088629555",
                    DisplayName = "Academe SaaS",
                    RedirectUris = { new Uri("https://nexuscore.local/signin-oidc") },
                    PostLogoutRedirectUris = { new Uri("https://nexuscore.local/signout-callback-oidc") },
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
                });
            }

            // ... (o resto do código para criar roles e usuários continua igual) ...
            const string adminRole = "admin";
            if (await roleManager.FindByNameAsync(adminRole) is null)
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
            }
            
            var adminUser = await userManager.FindByNameAsync("admin@nexuscore.com");
            if (adminUser is null)
            {
                adminUser = new ApplicationUser {
                    UserName = "admin@nexuscore.com",
                    Email = "admin@nexuscore.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "NexusCore@123");
                if (!result.Succeeded)
                {
                    throw new Exception("Não foi possível criar o usuário admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, adminRole))
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
        }
    }
}