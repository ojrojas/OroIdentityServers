namespace OroIdentityServers.Core;

public class InMemoryPersistedGrantStore : IPersistedGrantStore
{
    private readonly ConcurrentDictionary<string, AuthorizationCodeGrant> _authCodes = new();
    private readonly ConcurrentDictionary<string, RefreshTokenGrant> _refreshTokens = new();

    public Task StoreAuthorizationCodeAsync(string code, string clientId, string userId, string redirectUri, IEnumerable<string> scopes, string? codeChallenge = null, string? codeChallengeMethod = null)
    {
        var grant = new AuthorizationCodeGrant
        {
            Code = code,
            ClientId = clientId,
            UserId = userId,
            RedirectUri = redirectUri,
            Scopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };
        _authCodes[code] = grant;
        return Task.CompletedTask;
    }

    public Task<AuthorizationCodeGrant?> GetAuthorizationCodeAsync(string code)
    {
        _authCodes.TryGetValue(code, out var grant);
        return Task.FromResult(grant != null && grant.ExpiresAt > DateTime.UtcNow ? grant : null);
    }

    public Task RemoveAuthorizationCodeAsync(string code)
    {
        _authCodes.TryRemove(code, out _);
        return Task.CompletedTask;
    }

    public Task StoreRefreshTokenAsync(string refreshToken, string clientId, string userId, IEnumerable<string> scopes)
    {
        var grant = new RefreshTokenGrant
        {
            RefreshToken = refreshToken,
            ClientId = clientId,
            UserId = userId,
            Scopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _refreshTokens[refreshToken] = grant;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenGrant?> GetRefreshTokenAsync(string refreshToken)
    {
        _refreshTokens.TryGetValue(refreshToken, out var grant);
        return Task.FromResult(grant != null && grant.ExpiresAt > DateTime.UtcNow ? grant : null);
    }

    public Task RemoveRefreshTokenAsync(string refreshToken)
    {
        _refreshTokens.TryRemove(refreshToken, out _);
        return Task.CompletedTask;
    }
}