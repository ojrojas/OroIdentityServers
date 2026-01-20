using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OroIdentityServers.Core;

namespace OroIdentityServers;

public class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;

    public TokenService(string issuer, string audience, string secretKey)
    {
        _issuer = issuer;
        _audience = audience;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey.PadRight(32, '0'))); // Ensure at least 256 bits
    }

    public async Task<object> CreateAccessTokenAsync(Client client, IUser? user, string grantType)
    {
        var scopes = new List<string> { "openid" }; // Default scope
        var accessToken = GenerateAccessToken(client, user?.Id ?? "client", scopes);
        var refreshToken = GenerateRefreshToken();

        return new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = refreshToken,
            scope = string.Join(" ", scopes)
        };
    }

    public async Task<object?> RefreshAccessTokenAsync(string refreshToken, Client client)
    {
        // In a real implementation, you would validate the refresh token from storage
        // For now, we'll create a new token
        var scopes = new List<string> { "openid" };
        var accessToken = GenerateAccessToken(client, "refreshed_user", scopes);
        var newRefreshToken = GenerateRefreshToken();

        return new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            refresh_token = newRefreshToken,
            scope = string.Join(" ", scopes)
        };
    }

    public async Task<bool> ValidateAccessTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                IssuerSigningKey = _key
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task RevokeAccessTokenAsync(string token)
    {
        // In a real implementation, you would add the token to a blacklist
        // For now, this is a no-op
    }

    public string GenerateAccessToken(Client client, string userId, IEnumerable<string> scopes)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Iss, _issuer),
            new Claim(JwtRegisteredClaimNames.Aud, _audience),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("client_id", client.ClientId)
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateIdToken(IUser user, string clientId, string nonce)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Iss, _issuer),
            new Claim(JwtRegisteredClaimNames.Aud, clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString()),
            new Claim("nonce", nonce)
        };

        claims.AddRange(user.Claims);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: clientId,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public string GenerateAuthorizationCode()
    {
        var randomBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("/", "").Replace("+", "").Substring(0, 16);
    }
}