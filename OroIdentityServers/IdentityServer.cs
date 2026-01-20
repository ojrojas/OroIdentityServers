namespace OroIdentityServers;

public class IdentityServer
{
    public List<Client> Clients { get; } = [];
    public List<TokenRequest> TokenRequests { get; } = [];
    public List<OpenIdConnectRequest> OpenIdRequests { get; } = [];

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
                    AllowedGrantTypes = ["client_credentials", "authorization_code", "refresh_token"],
                    RedirectUris = ["https://localhost:5002/callback"],
                    AllowedScopes = ["openid", "profile", "api"]
                }
            };
            options.Users = new List<User>
            {
                new User
                {
                    Id = "user1",
                    Username = "user1",
                    PasswordHash = "password1", // Simplified
                    Claims = [new Claim("name", "user1"), new Claim("email", "user1@example.com")]
                }
            };
        });
    }

    public static void Configure(IApplicationBuilder app)
    {
        app.UseOroIdentityServer();
    }
}
