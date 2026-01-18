using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers.Core;

namespace OroIdentityServers;

public static class IdentityServerExtensions
{
    public static IServiceCollection AddOroIdentityServer(this IServiceCollection services, Action<IdentityServerOptions> configure)
    {
        var options = new IdentityServerOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IClientStore>(options.ClientStore ?? new InMemoryClientStore(options.Clients));
        services.AddSingleton<IUserStore>(options.UserStore ?? new InMemoryUserStore(options.Users));
        services.AddSingleton<IPersistedGrantStore>(options.PersistedGrantStore ?? new InMemoryPersistedGrantStore());
        services.AddSingleton(new TokenService(options.Issuer, options.Audience, options.SecretKey));

        return services;
    }

    public static IApplicationBuilder UseOroIdentityServer(this IApplicationBuilder app)
    {
        app.UseMiddleware<DiscoveryEndpointMiddleware>();
        app.UseMiddleware<AuthorizeEndpointMiddleware>();
        app.UseMiddleware<TokenEndpointMiddleware>();
        app.UseMiddleware<UserInfoEndpointMiddleware>();
        return app;
    }
}

public class IdentityServerOptions
{
    public string Issuer { get; set; } = "https://localhost:5001";
    public string Audience { get; set; } = "api";
    public string SecretKey { get; set; } = "supersecretkey";
    public IEnumerable<Client> Clients { get; set; } = new List<Client>();
    public IEnumerable<User> Users { get; set; } = new List<User>();
    public IClientStore? ClientStore { get; set; }
    public IUserStore? UserStore { get; set; }
    public IPersistedGrantStore? PersistedGrantStore { get; set; }
}