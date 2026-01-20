using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;
using OroIdentityServers.EntityFramework.Extensions;
using OroIdentityServers;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=multitenant.db"));

// Configure OroIdentityServer with multi-tenancy
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>();

// Configure event-driven architecture
builder.Services.AddInMemoryEventBus();
builder.Services.AddEventStore();

// Configure message broker for external integrations (optional)
// Uncomment and configure for RabbitMQ integration
// builder.Services.AddRabbitMqMessageBroker(builder.Configuration);

// Configure encryption service for client secrets (optional but recommended)
builder.Services.AddEncryptionService("your-secure-encryption-key-change-this-in-production");

// Configure multi-tenancy with header-based resolution
builder.Services.AddMultiTenancy(options =>
{
    options.ResolutionStrategy = TenantResolutionStrategy.Header;
    options.HeaderName = "X-Tenant-Id";
});

builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();
builder.Services.AddAutomaticMigrations<ApplicationDbContext>();
builder.Services.AddTokenCleanupService();

// Configure IdentityServer options
builder.Services.AddSingleton(new IdentityServerOptions
{
    Issuer = "http://localhost:5161",
    Audience = "api",
    SecretKey = "your-very-long-secret-key-at-least-32-characters-long"
});

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "http://localhost:5161";
        options.Audience = "api";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:5161",
            ValidateAudience = true,
            ValidAudience = "api",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("your-very-long-secret-key-at-least-32-characters-long"))
        };
    });

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await SeedDataAsync(context);
}

// Configure middleware
app.UseTenantResolution();
app.UseAuthentication();
app.UseOroIdentityServer();

app.Run();

async Task SeedDataAsync(ApplicationDbContext context)
{
    // Seed tenants
    if (!context.Tenants.Any())
    {
        context.Tenants.AddRange(
            new TenantEntity
            {
                TenantId = "tenant1",
                Name = "Tenant 1",
                Domain = "tenant1.example.com",
                Enabled = true,
                Created = DateTime.UtcNow
            },
            new TenantEntity
            {
                TenantId = "tenant2",
                Name = "Tenant 2",
                Domain = "tenant2.example.com",
                Enabled = true,
                Created = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();
    }

    // Seed tenant-specific identity resources
    if (!context.IdentityResources.Any())
    {
        context.IdentityResources.AddRange(
            new IdentityResourceEntity
            {
                TenantId = "tenant1",
                Name = "openid",
                DisplayName = "OpenID",
                Description = "OpenID Connect",
                Enabled = true,
                ShowInDiscoveryDocument = true,
                Created = DateTime.UtcNow
            },
            new IdentityResourceEntity
            {
                TenantId = "tenant1",
                Name = "profile",
                DisplayName = "Profile",
                Description = "User profile information",
                Enabled = true,
                ShowInDiscoveryDocument = true,
                Created = DateTime.UtcNow
            },
            new IdentityResourceEntity
            {
                TenantId = "tenant2",
                Name = "openid",
                DisplayName = "OpenID",
                Description = "OpenID Connect",
                Enabled = true,
                ShowInDiscoveryDocument = true,
                Created = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();
    }

    // Seed tenant-specific clients
    if (!context.Clients.Any())
    {
        var client1 = new ClientEntity
        {
            TenantId = "tenant1",
            ClientId = "tenant1-client",
            ClientSecret = "tenant1-secret",
            ClientName = "Tenant 1 Client",
            Enabled = true,
            Created = DateTime.UtcNow
        };
        context.Clients.Add(client1);
        await context.SaveChangesAsync();

        var client2 = new ClientEntity
        {
            TenantId = "tenant2",
            ClientId = "tenant2-client",
            ClientSecret = "tenant2-secret",
            ClientName = "Tenant 2 Client",
            Enabled = true,
            Created = DateTime.UtcNow
        };
        context.Clients.Add(client2);
        await context.SaveChangesAsync();
    }

    // Seed tenant-specific users
    if (!context.Users.Any())
    {
        context.Users.AddRange(
            new UserEntity
            {
                TenantId = "tenant1",
                Username = "alice",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Email = "alice@tenant1.com",
                EmailConfirmed = true,
                Enabled = true,
                Created = DateTime.UtcNow
            },
            new UserEntity
            {
                TenantId = "tenant1",
                Username = "alice@tenant1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Email = "alice@tenant1.com",
                EmailConfirmed = true,
                Enabled = true,
                Created = DateTime.UtcNow
            },
            new UserEntity
            {
                TenantId = "tenant2",
                Username = "bob@tenant2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Email = "bob@tenant2.com",
                EmailConfirmed = true,
                Enabled = true,
                Created = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();
    }
}

public class ApplicationDbContext : DbContext, IOroIdentityServerDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // OroIdentityServer entities
    public DbSet<ClientEntity> Clients { get; set; } = null!;
    public DbSet<ClientGrantTypeEntity> ClientGrantTypes { get; set; } = null!;
    public DbSet<ClientRedirectUriEntity> ClientRedirectUris { get; set; } = null!;
    public DbSet<ClientScopeEntity> ClientScopes { get; set; } = null!;
    public DbSet<ClientClaimEntity> ClientClaims { get; set; } = null!;

    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<UserClaimEntity> UserClaims { get; set; } = null!;

    public DbSet<PersistedGrantEntity> PersistedGrants { get; set; } = null!;

    public DbSet<IdentityResourceEntity> IdentityResources { get; set; } = null!;
    public DbSet<IdentityResourceClaimEntity> IdentityResourceClaims { get; set; } = null!;
    public DbSet<ApiResourceEntity> ApiResources { get; set; } = null!;
    public DbSet<ApiResourceClaimEntity> ApiResourceClaims { get; set; } = null!;
    public DbSet<ApiResourceScopeEntity> ApiResourceScopes { get; set; } = null!;
    public DbSet<ApiResourceSecretEntity> ApiResourceSecrets { get; set; } = null!;
    public DbSet<ApiScopeEntity> ApiScopes { get; set; } = null!;
    public DbSet<ApiScopeClaimEntity> ApiScopeClaims { get; set; } = null!;

    public DbSet<ConfigurationChangeLogEntity> ConfigurationChangeLogs { get; set; } = null!;
    public DbSet<EventEntity> Events { get; set; } = null!;
    public DbSet<TenantEntity> Tenants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOroIdentityServerEntities();
    }
}