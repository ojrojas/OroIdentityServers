namespace OroIdentityServers.Core;

/// <summary>
/// Base interface for OAuth endpoint handlers
/// </summary>
public interface IOAuthEndpointHandler
{
    Task HandleAsync(object context);
}

/// <summary>
/// Represents an OAuth 2.1 / OpenID Connect endpoint
/// </summary>
public class OAuthEndpoint
{
    public string Path { get; set; } = string.Empty;
    public string[] HttpMethods { get; set; } = ["GET", "POST"];
    public IOAuthEndpointHandler Handler { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Configuration for OAuth 2.1 / OpenID Connect endpoints
/// </summary>
public class OAuthEndpointsConfiguration
{
    private readonly Dictionary<string, OAuthEndpoint> _endpoints = new();

    public string BasePath { get; set; } = "/connect";

    public void AddEndpoint(string name, OAuthEndpoint endpoint)
    {
        _endpoints[name] = endpoint;
    }

    public void RemoveEndpoint(string name)
    {
        _endpoints.Remove(name);
    }

    public OAuthEndpoint? GetEndpoint(string name)
    {
        return _endpoints.GetValueOrDefault(name);
    }

    public IEnumerable<OAuthEndpoint> GetAllEndpoints()
    {
        return _endpoints.Values.Where(e => e.IsEnabled);
    }

    public string GetFullPath(string endpointName)
    {
        var endpoint = GetEndpoint(endpointName);
        return endpoint != null ? $"{BasePath}{endpoint.Path}" : string.Empty;
    }

    // Predefined endpoint builders - these will be implemented in the main project
    public static OAuthEndpoint CreateAuthorizationEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/authorize",
            HttpMethods = ["GET", "POST"],
            Handler = handler,
            Description = "OAuth 2.1 Authorization Endpoint"
        };
    }

    public static OAuthEndpoint CreateTokenEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/token",
            HttpMethods = ["POST"],
            Handler = handler,
            Description = "OAuth 2.1 Token Endpoint"
        };
    }

    public static OAuthEndpoint CreateUserInfoEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/userinfo",
            HttpMethods = ["GET", "POST"],
            Handler = handler,
            Description = "OpenID Connect UserInfo Endpoint"
        };
    }

    public static OAuthEndpoint CreateIntrospectionEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/introspect",
            HttpMethods = ["POST"],
            Handler = handler,
            Description = "OAuth 2.1 Token Introspection Endpoint"
        };
    }

    public static OAuthEndpoint CreateRevocationEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/revocation",
            HttpMethods = ["POST"],
            Handler = handler,
            Description = "OAuth 2.1 Token Revocation Endpoint"
        };
    }

    public static OAuthEndpoint CreateDiscoveryEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/.well-known/openid-configuration",
            HttpMethods = ["GET"],
            Handler = handler,
            Description = "OpenID Connect Discovery Document"
        };
    }

    public static OAuthEndpoint CreateJwksEndpoint(IOAuthEndpointHandler handler)
    {
        return new OAuthEndpoint
        {
            Path = "/.well-known/jwks.json",
            HttpMethods = ["GET"],
            Handler = handler,
            Description = "JSON Web Key Set"
        };
    }
}