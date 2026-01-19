namespace OroIdentityServers;

public class DiscoveryEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdentityServerOptions _options;

    public DiscoveryEndpointMiddleware(RequestDelegate next, IdentityServerOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/.well-known/openid-configuration" && context.Request.Method == "GET")
        {
            await HandleDiscoveryRequestAsync(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleDiscoveryRequestAsync(HttpContext context)
    {
        var discovery = new
        {
            issuer = _options.Issuer,
            authorization_endpoint = $"{_options.Issuer}/connect/authorize",
            token_endpoint = $"{_options.Issuer}/connect/token",
            userinfo_endpoint = $"{_options.Issuer}/connect/userinfo",
            jwks_uri = $"{_options.Issuer}/.well-known/jwks",
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            scopes_supported = new[] { "openid", "profile", "email" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post" },
            claims_supported = new[] { "sub", "name", "email" }
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(discovery));
    }
}