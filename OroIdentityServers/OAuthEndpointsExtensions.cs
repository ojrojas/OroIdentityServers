using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers;
using OroIdentityServers.Core;

namespace OroIdentityServers;

/// <summary>
/// Extension methods for configuring OAuth endpoints
/// </summary>
public static class OAuthEndpointsExtensions
{
    /// <summary>
    /// Adds OAuth endpoints to the application
    /// </summary>
    public static IServiceCollection AddOAuthEndpoints(this IServiceCollection services, Action<OAuthEndpointsConfiguration> configure)
    {
        var config = new OAuthEndpointsConfiguration();
        configure(config);

        services.AddSingleton(config);
        services.AddScoped<IOAuthEndpointHandler, UserInfoEndpointHandler>();
        services.AddScoped<IOAuthEndpointHandler, TokenEndpointHandler>();

        return services;
    }

    /// <summary>
    /// Uses OAuth endpoints middleware
    /// </summary>
    public static IApplicationBuilder UseOAuthEndpoints(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<OAuthEndpointsConfiguration>();
        app.UseMiddleware<OAuthEndpointsMiddleware>(config, app.ApplicationServices);
        return app;
    }

    /// <summary>
    /// Adds default OAuth endpoints configuration
    /// </summary>
    public static IServiceCollection AddDefaultOAuthEndpoints(this IServiceCollection services)
    {
        return services.AddOAuthEndpoints(config =>
        {
            config.BasePath = "/connect";

            // Token endpoint
            config.AddEndpoint("token", new OAuthEndpoint
            {
                Path = "/token",
                HttpMethods = ["POST"],
                Handler = null!, // Will be resolved from DI
                Description = "OAuth 2.1 Token Endpoint"
            });

            // UserInfo endpoint
            config.AddEndpoint("userinfo", new OAuthEndpoint
            {
                Path = "/userinfo",
                HttpMethods = ["GET", "POST"],
                Handler = null!, // Will be resolved from DI
                Description = "OpenID Connect UserInfo Endpoint"
            });

            // Discovery endpoint (if implemented)
            // config.AddEndpoint("discovery", "/.well-known/openid-configuration", new DiscoveryEndpointHandler());

            // Introspection endpoint (if implemented)
            // config.AddEndpoint("introspect", "/introspect", new IntrospectionEndpointHandler());

            // Revocation endpoint (if implemented)
            // config.AddEndpoint("revocation", "/revocation", new RevocationEndpointHandler());
        });
    }
}