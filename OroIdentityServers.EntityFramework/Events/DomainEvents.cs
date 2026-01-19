namespace OroIdentityServers.EntityFramework.Events;

/// <summary>
/// Client-related events
/// </summary>
public class ClientCreatedEvent : EventBase
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public IEnumerable<string> GrantTypes { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Scopes { get; set; } = Array.Empty<string>();
    public IEnumerable<string> RedirectUris { get; set; } = Array.Empty<string>();
}

public class ClientUpdatedEvent : EventBase
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public IEnumerable<string>? AddedGrantTypes { get; set; }
    public IEnumerable<string>? RemovedGrantTypes { get; set; }
    public IEnumerable<string>? AddedScopes { get; set; }
    public IEnumerable<string>? RemovedScopes { get; set; }
}

public class ClientDeletedEvent : EventBase
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
}

/// <summary>
/// User-related events
/// </summary>
public class UserCreatedEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}

public class UserUpdatedEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string? OldUsername { get; set; }
    public string? NewUsername { get; set; }
    public string? OldEmail { get; set; }
    public string? NewEmail { get; set; }
    public IEnumerable<string>? AddedRoles { get; set; }
    public IEnumerable<string>? RemovedRoles { get; set; }
}

public class UserDeletedEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
}

/// <summary>
/// Token-related events
/// </summary>
public class AccessTokenIssuedEvent : EventBase
{
    public string TokenId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public IEnumerable<string> Scopes { get; set; } = Array.Empty<string>();
    public string GrantType { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RefreshTokenIssuedEvent : EventBase
{
    public string TokenId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public IEnumerable<string> Scopes { get; set; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; set; }
}

public class TokenRevokedEvent : EventBase
{
    public string TokenId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Authentication events
/// </summary>
public class UserLoginEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

public class UserLogoutEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

/// <summary>
/// Authorization events
/// </summary>
public class AuthorizationGrantedEvent : EventBase
{
    public string ClientId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public IEnumerable<string> GrantedScopes { get; set; } = Array.Empty<string>();
    public string GrantType { get; set; } = string.Empty;
}

public class AuthorizationDeniedEvent : EventBase
{
    public string ClientId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}