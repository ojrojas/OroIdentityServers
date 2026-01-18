namespace OroIdentityServers.OAuth;

public class TokenRequest
{
    public string GrantType { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Code { get; set; }
    public string RedirectUri { get; set; }
    public string RefreshToken { get; set; }
    public List<string> Scopes { get; set; } = new();
}
