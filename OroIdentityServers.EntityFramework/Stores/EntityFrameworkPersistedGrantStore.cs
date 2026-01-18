using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServers.EntityFramework.Stores;

public class EntityFrameworkPersistedGrantStore : IPersistedGrantStore
{
    private readonly IOroIdentityServerDbContext _context;
    private readonly IDistributedCache? _cache;

    public EntityFrameworkPersistedGrantStore(
        IOroIdentityServerDbContext context,
        IDistributedCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<AuthorizationCodeGrant?> GetAuthorizationCodeAsync(string code)
    {
        var grant = await _context.PersistedGrants
            .FirstOrDefaultAsync(g => g.Key == code && g.Type == "authorization_code");

        if (grant == null || grant.Expiration < DateTime.UtcNow)
        {
            return null;
        }

        // Parse data field to extract redirect URI and scopes
        var dataParts = grant.Data?.Split('|') ?? Array.Empty<string>();
        var redirectUri = dataParts.Length > 0 ? dataParts[0] : "";
        var scopes = dataParts.Length > 1 ? dataParts[1].Split(' ') : Array.Empty<string>();
        var codeChallenge = dataParts.Length > 2 ? dataParts[2] : null;
        var codeChallengeMethod = dataParts.Length > 3 ? dataParts[3] : null;

        return new AuthorizationCodeGrant
        {
            Code = grant.Key,
            ClientId = grant.ClientId,
            UserId = grant.SubjectId,
            RedirectUri = redirectUri,
            Scopes = scopes,
            ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddMinutes(5),
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };
    }

    public async Task StoreAuthorizationCodeAsync(string code, string clientId, string userId, string redirectUri, IEnumerable<string> scopes, string? codeChallenge = null, string? codeChallengeMethod = null)
    {
        var data = $"{redirectUri}|{string.Join(" ", scopes)}|{codeChallenge}|{codeChallengeMethod}";

        var grant = new PersistedGrantEntity
        {
            Key = code,
            Type = "authorization_code",
            SubjectId = userId,
            ClientId = clientId,
            Data = data,
            Expiration = DateTime.UtcNow.AddMinutes(5), // Standard 5 minute expiration
            CreationTime = DateTime.UtcNow
        };

        _context.PersistedGrants.Add(grant);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAuthorizationCodeAsync(string code)
    {
        var grant = await _context.PersistedGrants
            .FirstOrDefaultAsync(g => g.Key == code && g.Type == "authorization_code");

        if (grant != null)
        {
            _context.PersistedGrants.Remove(grant);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<RefreshTokenGrant?> GetRefreshTokenAsync(string refreshToken)
    {
        var grant = await _context.PersistedGrants
            .FirstOrDefaultAsync(g => g.Key == refreshToken && g.Type == "refresh_token");

        if (grant == null || grant.Expiration < DateTime.UtcNow)
        {
            return null;
        }

        return new RefreshTokenGrant
        {
            RefreshToken = grant.Key,
            ClientId = grant.ClientId,
            UserId = grant.SubjectId,
            Scopes = grant.Data?.Split(' ') ?? Array.Empty<string>(),
            ExpiresAt = grant.Expiration ?? DateTime.UtcNow.AddDays(30)
        };
    }

    public async Task StoreRefreshTokenAsync(string refreshToken, string clientId, string userId, IEnumerable<string> scopes)
    {
        var grant = new PersistedGrantEntity
        {
            Key = refreshToken,
            Type = "refresh_token",
            SubjectId = userId,
            ClientId = clientId,
            CreationTime = DateTime.UtcNow,
            Expiration = DateTime.UtcNow.AddDays(30), // Default 30 days
            Data = string.Join(" ", scopes)
        };

        _context.PersistedGrants.Add(grant);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRefreshTokenAsync(string refreshToken)
    {
        var grant = await _context.PersistedGrants
            .FirstOrDefaultAsync(g => g.Key == refreshToken && g.Type == "refresh_token");

        if (grant != null)
        {
            _context.PersistedGrants.Remove(grant);
            await _context.SaveChangesAsync();
        }
    }
}