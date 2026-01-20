namespace OroIdentityServers.OpenId;

public class OpenIdConnectRequest
{
    public string Scope { get; set; }= string.Empty;
    public string ResponseType { get; set; }= string.Empty;
    public string ClientId { get; set; }= string.Empty;
    public string RedirectUri { get; set; }= string.Empty;
    public string State { get; set; }= string.Empty;
    public string Nonce { get; set; }= string.Empty;
}
