using Microsoft.EntityFrameworkCore;
using OroIdentityServers;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Extensions;
using OroIdentityServers.EntityFramework.Entities;

public class ProgramEntityFramework
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddCors();

        // Configure OroIdentityServer with Entity Framework and SQL Server
        builder.Services.AddOroIdentityServerDbContext(options =>
        {
            options.UseSqlServer(
                "Server=localhost;Database=OroIdentityServer;Trusted_Connection=True;TrustServerCertificate=True",
                sqlOptions => sqlOptions.MigrationsAssembly("OroIdentityServerExample"));
        });

        builder.Services.AddEntityFrameworkStores();
        builder.Services.AddConfigurationEvents();

// Configure Identity Server
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://localhost:5001";
    options.Audience = "api";
    options.SecretKey = "your-very-long-secret-key-at-least-32-characters";

    // Note: Clients and users are now stored in the database
    // You can manage them through the Entity Framework stores
    options.Clients = new List<Client>(); // Empty - managed via EF stores
    options.Users = new List<User>(); // Empty - managed via EF stores
});

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OroIdentityServerDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed initial data if needed
    await SeedInitialDataAsync(dbContext);
}

async Task SeedInitialDataAsync(OroIdentityServerDbContext dbContext)
{
    // Seed clients
    if (!await dbContext.Clients.AnyAsync())
    {
        var client = new ClientEntity
        {
            TenantId = "default",
            ClientId = "web-client",
            ClientSecret = "web-secret",
            ClientName = "Web Application",
            Enabled = true,
            Created = DateTime.UtcNow
        };

        // Add grant types
        client.AllowedGrantTypes.Add(new ClientGrantTypeEntity { GrantType = "authorization_code" });
        client.AllowedGrantTypes.Add(new ClientGrantTypeEntity { GrantType = "refresh_token" });

        // Add redirect URIs
        client.RedirectUris.Add(new ClientRedirectUriEntity { RedirectUri = "https://localhost:3000/callback" });

        // Add scopes
        client.AllowedScopes.Add(new ClientScopeEntity { Scope = "openid" });
        client.AllowedScopes.Add(new ClientScopeEntity { Scope = "profile" });
        client.AllowedScopes.Add(new ClientScopeEntity { Scope = "api" });

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();
    }

    // Seed users
    if (!await dbContext.Users.AnyAsync())
    {
        var user = new UserEntity
        {
            TenantId = "default",
            Username = "alice",
            PasswordHash = "password", // In production, use proper hashing
            Email = "alice@example.com",
            EmailConfirmed = true,
            Enabled = true,
            Created = DateTime.UtcNow
        };

        // Add claims
        user.Claims.Add(new UserClaimEntity { Type = "name", Value = "Alice" });
        user.Claims.Add(new UserClaimEntity { Type = "email", Value = "alice@example.com" });

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }
}

// Use Identity Server middleware
        app.UseOroIdentityServer();

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}