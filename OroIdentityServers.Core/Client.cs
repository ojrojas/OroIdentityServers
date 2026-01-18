using System.Security.Claims;

namespace OroIdentityServers.Core;

public class Client
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string? ClientName { get; set; }
    public List<string> AllowedGrantTypes { get; set; } = new();
    public List<string> RedirectUris { get; set; } = new();
    public List<string> AllowedScopes { get; set; } = new();
    public List<Claim> Claims { get; set; } = new();
}
