using OroIdentityServers.Core;

namespace OroIdentityServers;

/// <summary>
/// Interface for token service operations
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates an access token for the specified client and user
    /// </summary>
    Task<object> CreateAccessTokenAsync(Client client, IUser? user, string grantType);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    Task<object?> RefreshAccessTokenAsync(string refreshToken, Client client);

    /// <summary>
    /// Validates an access token
    /// </summary>
    Task<bool> ValidateAccessTokenAsync(string token);

    /// <summary>
    /// Revokes an access token
    /// </summary>
    Task RevokeAccessTokenAsync(string token);
}