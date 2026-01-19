using Microsoft.EntityFrameworkCore;
using OroIdentityServers;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.Extensions;
using OroIdentityServers.EntityFramework.Entities;

public class ProgramIntegratedDbContext
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddCors();

        // Configure your application's DbContext with OroIdentityServer entities
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                "Server=localhost;Database=YourApplicationDb;Trusted_Connection=True;TrustServerCertificate=True",
                sqlOptions => sqlOptions.MigrationsAssembly("YourApplication"));
        });

// Add OroIdentityServer stores (they will use your DbContext)
builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();

// Configure Identity Server
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://localhost:5001";
    options.Audience = "api";
    options.SecretKey = "your-very-long-secret-key-at-least-32-characters";

    // Clients and users are managed through your DbContext
    options.Clients = new List<Client>(); // Empty - managed via EF stores
    options.Users = new List<User>(); // Empty - managed via EF stores
});

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed initial data if needed
    await SeedInitialDataAsync(dbContext);
}

async Task SeedInitialDataAsync(ApplicationDbContext dbContext)
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

    // You can also seed your own application data here
    // if (!await dbContext.YourEntities.AnyAsync())
    // {
    //     // Seed your application data
    // }
}

// Use Identity Server middleware
app.UseOroIdentityServer();

app.UseAuthorization();
app.MapControllers();

        app.Run();
    }
}

// Your application's DbContext that includes OroIdentityServer entities
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // OroIdentityServer entities
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ClientGrantTypeEntity> ClientGrantTypes => Set<ClientGrantTypeEntity>();
    public DbSet<ClientRedirectUriEntity> ClientRedirectUris => Set<ClientRedirectUriEntity>();
    public DbSet<ClientScopeEntity> ClientScopes => Set<ClientScopeEntity>();
    public DbSet<ClientClaimEntity> ClientClaims => Set<ClientClaimEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserClaimEntity> UserClaims => Set<UserClaimEntity>();

    public DbSet<PersistedGrantEntity> PersistedGrants => Set<PersistedGrantEntity>();

    public DbSet<IdentityResourceEntity> IdentityResources => Set<IdentityResourceEntity>();
    public DbSet<IdentityResourceClaimEntity> IdentityResourceClaims => Set<IdentityResourceClaimEntity>();

    public DbSet<ApiResourceEntity> ApiResources => Set<ApiResourceEntity>();
    public DbSet<ApiResourceClaimEntity> ApiResourceClaims => Set<ApiResourceClaimEntity>();
    public DbSet<ApiResourceScopeEntity> ApiResourceScopes => Set<ApiResourceScopeEntity>();
    public DbSet<ApiResourceSecretEntity> ApiResourceSecrets => Set<ApiResourceSecretEntity>();

    public DbSet<ApiScopeEntity> ApiScopes => Set<ApiScopeEntity>();
    public DbSet<ApiScopeClaimEntity> ApiScopeClaims => Set<ApiScopeClaimEntity>();

    public DbSet<ConfigurationChangeLogEntity> ConfigurationChangeLogs => Set<ConfigurationChangeLogEntity>();

    // Your application's entities
    // public DbSet<YourEntity> YourEntities => Set<YourEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add OroIdentityServer entities to your model
        // You can specify a schema if desired: modelBuilder.AddOroIdentityServerEntities("identity");
        modelBuilder.AddOroIdentityServerEntities();

        // Configure your own entities here
        // modelBuilder.Entity<YourEntity>(entity =>
        // {
        //     // Your entity configuration
        // });
    }
}