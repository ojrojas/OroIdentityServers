using System.Threading.Tasks;

namespace OroIdentityServers.Core;

public interface IPersistedGrantStore
{
    Task StoreAuthorizationCodeAsync(string code, string clientId, string userId, string redirectUri, IEnumerable<string> scopes, string? codeChallenge = null, string? codeChallengeMethod = null);
    Task<AuthorizationCodeGrant?> GetAuthorizationCodeAsync(string code);
    Task RemoveAuthorizationCodeAsync(string code);
    Task StoreRefreshTokenAsync(string refreshToken, string clientId, string userId, IEnumerable<string> scopes);
    Task<RefreshTokenGrant?> GetRefreshTokenAsync(string refreshToken);
    Task RemoveRefreshTokenAsync(string refreshToken);
}

public class AuthorizationCodeGrant
{
    public string Code { get; set; }
    public string ClientId { get; set; }
    public string UserId { get; set; }
    public string RedirectUri { get; set; }
    public IEnumerable<string> Scopes { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
}

public class RefreshTokenGrant
{
    public string RefreshToken { get; set; }
    public string ClientId { get; set; }
    public string UserId { get; set; }
    public IEnumerable<string> Scopes { get; set; }
    public DateTime ExpiresAt { get; set; }
}