using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers.Core;
using OroIdentityServers.OAuth;
using OroIdentityServers.OpenId;

namespace OroIdentityServers;

public class IdentityServer
{
    public List<Client> Clients { get; } = new();
    public List<TokenRequest> TokenRequests { get; } = new();
    public List<OpenIdConnectRequest> OpenIdRequests { get; } = new();

    public void AddClient(Client client)
    {
        Clients.Add(client);
    }

    // Method to configure the server in an ASP.NET Core application
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddOroIdentityServer(options =>
        {
            options.Clients = new List<Client>
            {
                new Client
                {
                    ClientId = "client1",
                    ClientSecret = "secret1",
                    AllowedGrantTypes = new List<string> { "client_credentials", "authorization_code", "refresh_token" },
                    RedirectUris = new List<string> { "https://localhost:5002/callback" },
                    AllowedScopes = new List<string> { "openid", "profile", "api" }
                }
            };
            options.Users = new List<User>
            {
                new User
                {
                    Id = "user1",
                    Username = "user1",
                    PasswordHash = "password1", // Simplified
                    Claims = new List<string> { "name:user1", "email:user1@example.com" }
                }
            };
        });
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseOroIdentityServer();
    }
}
