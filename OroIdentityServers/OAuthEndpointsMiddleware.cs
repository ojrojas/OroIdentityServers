using Microsoft.AspNetCore.Http;
using OroIdentityServers.Core;

namespace OroIdentityServers;

/// <summary>
/// Generic middleware for handling OAuth 2.1 / OpenID Connect endpoints
/// </summary>
public class OAuthEndpointsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly OAuthEndpointsConfiguration _endpointsConfig;
    private readonly IServiceProvider _serviceProvider;

    public OAuthEndpointsMiddleware(RequestDelegate next, OAuthEndpointsConfiguration endpointsConfig, IServiceProvider serviceProvider)
    {
        _next = next;
        _endpointsConfig = endpointsConfig;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = FindMatchingEndpoint(context);
        if (endpoint != null)
        {
            var handler = GetHandlerForEndpoint(endpoint);
            if (handler != null)
            {
                await handler.HandleAsync(context);
                return;
            }
        }

        await _next(context);
    }

    private OAuthEndpoint? FindMatchingEndpoint(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;
        var requestMethod = context.Request.Method;

        if (requestPath == null || !requestPath.StartsWith(_endpointsConfig.BasePath))
            return null;

        var relativePath = requestPath.Substring(_endpointsConfig.BasePath.Length);

        foreach (var endpoint in _endpointsConfig.GetAllEndpoints())
        {
            if (endpoint.Path == relativePath && endpoint.HttpMethods.Contains(requestMethod))
            {
                return endpoint;
            }
        }

        return null;
    }

    private IOAuthEndpointHandler? GetHandlerForEndpoint(OAuthEndpoint endpoint)
    {
        // For now, we'll resolve handlers by path. In a more sophisticated implementation,
        // we could use endpoint.Handler directly or have a registry
        if (endpoint.Path == "/token")
        {
            return _serviceProvider.GetRequiredService<TokenEndpointHandler>();
        }
        else if (endpoint.Path == "/userinfo")
        {
            return _serviceProvider.GetRequiredService<UserInfoEndpointHandler>();
        }

        return null;
    }
}