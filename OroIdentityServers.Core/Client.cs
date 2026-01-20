namespace OroIdentityServers.Core;

public class Client
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public List<string> AllowedGrantTypes { get; set; } = [];
    public List<string> RedirectUris { get; set; } = [];
    public List<string> AllowedScopes { get; set; } = [];
    public List<Claim> Claims { get; set; } = [];
}
