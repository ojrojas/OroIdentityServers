namespace OroIdentityServers.OAuth;

public class TokenRequest
{
    public string GrantType { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
}
