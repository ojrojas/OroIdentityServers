using System.Security.Claims;

namespace OroIdentityServers.Core;

/// <summary>
/// Interface for user authentication and management
/// </summary>
public interface IUserAuthenticationService
{
    Task<ClaimsPrincipal?> AuthenticateUserAsync(string username, string password);
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromClaimsAsync(ClaimsPrincipal principal);
    Task<bool> ValidatePasswordAsync(IUser user, string password);
    Task<bool> ValidateClientSecretAsync(Client client, string clientSecret);
}

/// <summary>
/// Default implementation of user authentication service
/// </summary>
public class DefaultUserAuthenticationService : IUserAuthenticationService
{
    private readonly IUserStore _userStore;

    public DefaultUserAuthenticationService(IUserStore userStore)
    {
        _userStore = userStore;
    }

    public async Task<ClaimsPrincipal?> AuthenticateUserAsync(string username, string password)
    {
        var user = await _userStore.FindUserByUsernameAsync(username);
        if (user == null)
            return null;

        if (!await ValidatePasswordAsync(user, password))
            return null;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(user.Claims);

        var identity = new ClaimsIdentity(claims, "password");
        return new ClaimsPrincipal(identity);
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        // This will be implemented by token validation middleware
        throw new NotImplementedException();
    }

    public Task<string?> GetUserIdFromClaimsAsync(ClaimsPrincipal principal)
    {
        return Task.FromResult(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? principal.FindFirst("sub")?.Value
                             ?? principal.FindFirst("sub")?.Value);
    }

    public Task<bool> ValidatePasswordAsync(IUser user, string password)
    {
        return Task.FromResult(user.ValidatePassword(password));
    }

    public Task<bool> ValidateClientSecretAsync(Client client, string clientSecret)
    {
        // Simple validation - in production, this should use proper hashing
        return Task.FromResult(client.ClientSecret == clientSecret);
    }
}