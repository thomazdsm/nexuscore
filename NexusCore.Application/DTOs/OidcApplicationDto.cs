using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusCore.Application.DTOs
{
    public class OidcApplicationDto
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string DisplayName { get; set; }
        public string? RedirectUri { get; set; }
        public string? PostLogoutRedirectUri { get; set; }
    }
}
