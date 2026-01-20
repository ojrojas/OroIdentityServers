using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Extensions;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServerMySQLExample;

public class ApplicationDbContext : DbContext, IOroIdentityServerDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Your application entities can go here
    // public DbSet<MyEntity> MyEntities { get; set; }

    // OroIdentityServer entities (added automatically via extension)
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

        // Add OroIdentityServer entities to your model
        modelBuilder.AddOroIdentityServerEntities();
    }
}