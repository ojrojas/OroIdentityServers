namespace OroIdentityServers.OpenId;

public class OpenIdConnectRequest
{
    public string Scope { get; set; }
    public string ResponseType { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }
    public string State { get; set; }
    public string Nonce { get; set; }
}
