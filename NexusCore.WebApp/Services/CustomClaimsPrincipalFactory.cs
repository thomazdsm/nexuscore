using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexusCore.Domain.Entities;
using NexusCore.Infra.Data.Context;

namespace NexusCore.WebApp.Services
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        private readonly AppDbContext _context;

        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            AppDbContext context)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // Gera as claims padrão (email, id, etc.)
            var identity = await base.GenerateClaimsAsync(user);

            // Busca o perfil do usuário no banco de dados
            var userProfile = await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);

            if (userProfile != null)
            {
                // Concatena o nome e sobrenome
                var fullName = $"{userProfile.FirstName} {userProfile.LastName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    // Adiciona a nova claim de nome completo à identidade
                    identity.AddClaim(new Claim("FullName", fullName));
                }
            }

            return identity;
        }
    }
}