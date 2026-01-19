using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OroIdentityServers.EntityFramework.Services;

public class DatabaseMigrationService<TDbContext> : IHostedService
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService<TDbContext>> _logger;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService<TDbContext>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        try
        {
            _logger.LogInformation("Ensuring database is created...");
            
            // Force model creation by accessing the model
            var model = dbContext.Model;
            _logger.LogInformation($"Database model has {model.GetEntityTypes().Count()} entity types");
            
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Database created successfully.");
            
            // Seed the database
            _logger.LogInformation("Seeding database...");
            await SeedDatabaseAsync(dbContext, cancellationToken);
            _logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SeedDatabaseAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        // Seed identity resources
        if (!await dbContext.Set<IdentityResourceEntity>().AnyAsync(cancellationToken))
        {
            await dbContext.Set<IdentityResourceEntity>().AddRangeAsync(new[]
            {
                new IdentityResourceEntity
                {
                    TenantId = "default",
                    Name = "openid",
                    DisplayName = "OpenID",
                    Description = "OpenID Connect",
                    Enabled = true,
                    ShowInDiscoveryDocument = true,
                    Created = DateTime.UtcNow
                },
                new IdentityResourceEntity
                {
                    TenantId = "default",
                    Name = "profile",
                    DisplayName = "Profile",
                    Description = "User profile information",
                    Enabled = true,
                    ShowInDiscoveryDocument = true,
                    Created = DateTime.UtcNow
                }
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Seed API scopes
        if (!await dbContext.Set<ApiScopeEntity>().AnyAsync(cancellationToken))
        {
            await dbContext.Set<ApiScopeEntity>().AddAsync(new ApiScopeEntity
            {
                Name = "api",
                DisplayName = "API Access",
                Description = "Access to API",
                ShowInDiscoveryDocument = true,
                Created = DateTime.UtcNow
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Seed clients
        if (!await dbContext.Set<ClientEntity>().AnyAsync(cancellationToken))
        {
            var webClient = new ClientEntity
            {
                TenantId = "default",
                ClientId = "web-client",
                ClientSecret = "web-secret",
                ClientName = "Web Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            await dbContext.Set<ClientEntity>().AddAsync(webClient, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Add grant types and scopes
            await dbContext.Set<ClientGrantTypeEntity>().AddRangeAsync(new[]
            {
                new ClientGrantTypeEntity { ClientId = webClient.Id, GrantType = "authorization_code" },
                new ClientGrantTypeEntity { ClientId = webClient.Id, GrantType = "refresh_token" }
            }, cancellationToken);
            await dbContext.Set<ClientScopeEntity>().AddRangeAsync(new[]
            {
                new ClientScopeEntity { ClientId = webClient.Id, Scope = "openid" },
                new ClientScopeEntity { ClientId = webClient.Id, Scope = "profile" },
                new ClientScopeEntity { ClientId = webClient.Id, Scope = "api" }
            }, cancellationToken);
            await dbContext.Set<ClientRedirectUriEntity>().AddAsync(
                new ClientRedirectUriEntity { ClientId = webClient.Id, RedirectUri = "http://localhost:5160/callback" },
                cancellationToken);

            var apiClient = new ClientEntity
            {
                TenantId = "default",
                ClientId = "client-credentials-client",
                ClientSecret = "client-secret",
                ClientName = "Client Credentials Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            await dbContext.Set<ClientEntity>().AddAsync(apiClient, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Set<ClientGrantTypeEntity>().AddAsync(
                new ClientGrantTypeEntity { ClientId = apiClient.Id, GrantType = "client_credentials" },
                cancellationToken);
            await dbContext.Set<ClientScopeEntity>().AddAsync(
                new ClientScopeEntity { ClientId = apiClient.Id, Scope = "api" },
                cancellationToken);

            var passwordClient = new ClientEntity
            {
                TenantId = "default",
                ClientId = "password-client",
                ClientSecret = "password-secret",
                ClientName = "Password Client",
                Enabled = true,
                Created = DateTime.UtcNow
            };
            await dbContext.Set<ClientEntity>().AddAsync(passwordClient, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.Set<ClientGrantTypeEntity>().AddRangeAsync(new[]
            {
                new ClientGrantTypeEntity { ClientId = passwordClient.Id, GrantType = "password" },
                new ClientGrantTypeEntity { ClientId = passwordClient.Id, GrantType = "refresh_token" }
            }, cancellationToken);
            await dbContext.Set<ClientScopeEntity>().AddRangeAsync(new[]
            {
                new ClientScopeEntity { ClientId = passwordClient.Id, Scope = "openid" },
                new ClientScopeEntity { ClientId = passwordClient.Id, Scope = "profile" },
                new ClientScopeEntity { ClientId = passwordClient.Id, Scope = "api" }
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Seed users
        if (!await dbContext.Set<UserEntity>().AnyAsync(cancellationToken))
        {
            var user = new UserEntity
            {
                TenantId = "default",
                Username = "alice",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Email = "alice@example.com",
                EmailConfirmed = true,
                Enabled = true,
                Created = DateTime.UtcNow
            };
            await dbContext.Set<UserEntity>().AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Add user claims
            await dbContext.Set<UserClaimEntity>().AddRangeAsync(new[]
            {
                new UserClaimEntity
                {
                    UserId = user.Id,
                    Type = "name",
                    Value = "Alice"
                },
                new UserClaimEntity
                {
                    UserId = user.Id,
                    Type = "email",
                    Value = "alice@example.com"
                }
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}