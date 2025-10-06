using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NexusCore.Domain.Entities;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NexusCore.WebApp.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public AuthorizationController(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange() 
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                          throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsAuthorizationCodeGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                
                // VERIFICAÇÃO ADICIONADA: Garante que a autenticação do cookie da sessão foi bem-sucedida.
                if (!result.Succeeded)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is no longer valid because the session has expired."
                        }));
                }
                
                var user = await _userManager.GetUserAsync(result.Principal);
                if (user == null)
                {
                    return Forbid(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The authorization code is no longer valid."
                        }));
                }

                var identity = new ClaimsIdentity(result.Principal.Claims,
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: OpenIddictConstants.Claims.Name,
                    roleType: OpenIddictConstants.Claims.Role);

                identity.SetDestinations(GetDestinations);

                return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");

        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("...");

            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            if (!result.Succeeded || result.Principal?.Identity is null || !result.Principal.Identity.IsAuthenticated)
            {
                // Se não estiver logado, desafia o Identity a iniciar o login.
                // O Identity, por sua vez, redirecionará para /Account/Login.
                return Challenge(
                    authenticationSchemes: IdentityConstants.ApplicationScheme,
                    properties: new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.Query.ToList()) });
            }

            // Cria o tíquete de autorização para o OpenIddict
            var user = await _userManager.GetUserAsync(result.Principal) ?? throw new InvalidOperationException("User not found.");
            var claims = new List<Claim> {
                new Claim(OpenIddictConstants.Claims.Subject, user.Id),
                new Claim(OpenIddictConstants.Claims.Email, user.Email).SetDestinations(OpenIddictConstants.Destinations.IdentityToken),
                new Claim(OpenIddictConstants.Claims.Name, user.UserName).SetDestinations(OpenIddictConstants.Destinations.IdentityToken)
            };
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(OpenIddictConstants.Claims.Role, role)));

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            claimsPrincipal.SetScopes(request.GetScopes());
            claimsPrincipal.SetDestinations(GetDestinations);

            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private static IEnumerable<string> GetDestinations(Claim claim) 
        {
            switch (claim.Type)
            {
                case OpenIddictConstants.Claims.Name:
                case OpenIddictConstants.Claims.Email:
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                    yield break;

                case OpenIddictConstants.Claims.Role:
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                    yield break;

                default:
                    yield return OpenIddictConstants.Destinations.AccessToken;
                    yield break;
            }

        }
    }
}