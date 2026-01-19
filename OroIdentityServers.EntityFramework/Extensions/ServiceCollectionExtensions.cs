using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;
using OroIdentityServers.EntityFramework.Events;
using OroIdentityServers.EntityFramework.Services;
using OroIdentityServers.EntityFramework.Stores;

namespace OroIdentityServers.EntityFramework.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a custom DbContext that implements IOroIdentityServerDbContext.
    /// Use this when integrating OroIdentityServer entities into your existing DbContext.
    /// </summary>
    /// <typeparam name="TDbContext">Your DbContext type that implements IOroIdentityServerDbContext</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddOroIdentityServerDbContext<TDbContext>(
        this IServiceCollection services)
        where TDbContext : class, IOroIdentityServerDbContext
    {
        services.AddScoped<IOroIdentityServerDbContext>(provider =>
            provider.GetRequiredService<TDbContext>());
        return services;
    }

    /// <summary>
    /// Adds the OroIdentityServer DbContext to the service collection.
    /// The database provider must be configured by the caller.
    /// Use this for a separate DbContext dedicated to OroIdentityServer.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="optionsAction">Action to configure the DbContext options</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddOroIdentityServerDbContext(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<OroIdentityServerDbContext>(optionsAction);
        services.AddScoped<IOroIdentityServerDbContext>(provider =>
            provider.GetRequiredService<OroIdentityServerDbContext>());
        return services;
    }

    /// <summary>
    /// Adds the OroIdentityServer stores with Entity Framework implementation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEntityFrameworkStores(
        this IServiceCollection services)
    {
        services.AddScoped<IClientStore, EntityFrameworkClientStore>();
        services.AddScoped<IUserStore, EntityFrameworkUserStore>();
        services.AddScoped<IPersistedGrantStore, EntityFrameworkPersistedGrantStore>();
        return services;
    }

    /// <summary>
    /// Adds configuration change event handling.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="eventHandler">The event handler implementation</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConfigurationEvents(
        this IServiceCollection services,
        IConfigurationChangeNotifier eventHandler)
    {
        services.AddSingleton(eventHandler);
        return services;
    }

    /// <summary>
    /// Adds configuration change event handling with in-memory implementation.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConfigurationEvents(
        this IServiceCollection services)
    {
        services.AddSingleton<IConfigurationChangeNotifier, InMemoryConfigurationChangeNotifier>();
        return services;
    }

    /// <summary>
    /// Adds automatic database migration service.
    /// Use this to automatically apply migrations on application startup.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddAutomaticMigrations<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddHostedService<DatabaseMigrationService<TDbContext>>();
        return services;
    }

    /// <summary>
    /// Adds automatic token cleanup service.
    /// This service periodically removes expired persisted grants.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddTokenCleanupService(
        this IServiceCollection services)
    {
        services.AddHostedService<TokenCleanupService>();
        return services;
    }

    /// <summary>
    /// Adds encryption service for client secrets and sensitive data.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="encryptionKey">The encryption key (should be stored securely)</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEncryptionService(
        this IServiceCollection services,
        string encryptionKey)
    {
        services.AddSingleton<IEncryptionService>(new AesEncryptionService(encryptionKey));
        return services;
    }

    /// <summary>
    /// Adds encryption service for client secrets and sensitive data.
    /// Uses a default encryption key (not recommended for production).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEncryptionService(
        this IServiceCollection services)
    {
        // WARNING: This uses a default key for development only
        // In production, use AddEncryptionService(services, "your-secure-key")
        var defaultKey = "OroIdentityServerDefaultEncryptionKey2024!@#";
        services.AddSingleton<IEncryptionService>(new AesEncryptionService(defaultKey));
        return services;
    }
}

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds OroIdentityServer entities to the model.
    /// Use this in your DbContext's OnModelCreating method.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="schema">Optional schema name for the tables</param>
    /// <returns>The model builder</returns>
    public static ModelBuilder AddOroIdentityServerEntities(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        // Apply schema if specified
        var schemaPrefix = string.IsNullOrEmpty(schema) ? "" : $"{schema}.";

        // Configure entities
        ConfigureClients(modelBuilder, schema);
        ConfigureUsers(modelBuilder, schema);
        ConfigurePersistedGrants(modelBuilder, schema);
        ConfigureResources(modelBuilder, schema);
        ConfigureConfigurationChangeLogs(modelBuilder, schema);

        return modelBuilder;
    }

    private static void ConfigureClients(ModelBuilder modelBuilder, string? schema)
    {
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("Clients", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<ClientGrantTypeEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ClientGrantTypes", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Client).WithMany(c => c.AllowedGrantTypes).HasForeignKey(e => e.ClientId);
        });

        modelBuilder.Entity<ClientRedirectUriEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ClientRedirectUris", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RedirectUri).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.Client).WithMany(c => c.RedirectUris).HasForeignKey(e => e.ClientId);
        });

        modelBuilder.Entity<ClientScopeEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ClientScopes", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Scope).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Client).WithMany(c => c.AllowedScopes).HasForeignKey(e => e.ClientId);
        });

        modelBuilder.Entity<ClientClaimEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ClientClaims", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(250);
            entity.HasOne(e => e.Client).WithMany(c => c.Claims).HasForeignKey(e => e.ClientId);
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder, string? schema)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("Users", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<UserClaimEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("UserClaims", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(250);
            entity.HasOne(e => e.User).WithMany(u => u.Claims).HasForeignKey(e => e.UserId);
        });
    }

    private static void ConfigurePersistedGrants(ModelBuilder modelBuilder, string? schema)
    {
        modelBuilder.Entity<PersistedGrantEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("PersistedGrants", schema);
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubjectId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Data).HasMaxLength(500);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CreationTime).HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            entity.HasIndex(e => new { e.SubjectId, e.ClientId, e.Type });
            entity.HasIndex(e => e.Expiration);
        });
    }

    private static void ConfigureResources(ModelBuilder modelBuilder, string? schema)
    {
        modelBuilder.Entity<IdentityResourceEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("IdentityResources", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<IdentityResourceClaimEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("IdentityResourceClaims", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.IdentityResource).WithMany(r => r.UserClaims).HasForeignKey(e => e.IdentityResourceId);
        });

        modelBuilder.Entity<ApiResourceEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiResources", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<ApiResourceClaimEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiResourceClaims", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiResource).WithMany(r => r.UserClaims).HasForeignKey(e => e.ApiResourceId);
        });

        modelBuilder.Entity<ApiResourceScopeEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiResourceScopes", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Scope).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiResource).WithMany(r => r.Scopes).HasForeignKey(e => e.ApiResourceId);
        });

        modelBuilder.Entity<ApiResourceSecretEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiResourceSecrets", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasOne(e => e.ApiResource).WithMany(r => r.Secrets).HasForeignKey(e => e.ApiResourceId);
        });

        modelBuilder.Entity<ApiScopeEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiScopes", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<ApiScopeClaimEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ApiScopeClaims", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiScope).WithMany(s => s.UserClaims).HasForeignKey(e => e.ApiScopeId);
        });
    }

    private static void ConfigureConfigurationChangeLogs(ModelBuilder modelBuilder, string? schema)
    {
        modelBuilder.Entity<ConfigurationChangeLogEntity>(entity =>
        {
            if (!string.IsNullOrEmpty(schema)) entity.ToTable("ConfigurationChangeLogs", schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangeTime).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.ChangedBy).HasMaxLength(200);
            entity.Property(e => e.ChangeDescription).HasMaxLength(1000);
            entity.Property(e => e.OldValues).HasColumnType("TEXT");
            entity.Property(e => e.NewValues).HasColumnType("TEXT");

            // Index
            entity.HasIndex(e => e.ChangeTime);
        });
    }
}