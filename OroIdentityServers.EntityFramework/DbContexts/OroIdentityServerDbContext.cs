using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServers.EntityFramework.DbContexts;

public class OroIdentityServerDbContext : DbContext, IOroIdentityServerDbContext
{
    public OroIdentityServerDbContext(DbContextOptions<OroIdentityServerDbContext> options)
        : base(options)
    {
    }

    // Clients
    public DbSet<ClientEntity> Clients { get; set; } = null!;
    public DbSet<ClientGrantTypeEntity> ClientGrantTypes { get; set; } = null!;
    public DbSet<ClientRedirectUriEntity> ClientRedirectUris { get; set; } = null!;
    public DbSet<ClientScopeEntity> ClientScopes { get; set; } = null!;
    public DbSet<ClientClaimEntity> ClientClaims { get; set; } = null!;

    // Users
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<UserClaimEntity> UserClaims { get; set; } = null!;

    // Persisted Grants
    public DbSet<PersistedGrantEntity> PersistedGrants { get; set; } = null!;

    // Resources
    public DbSet<IdentityResourceEntity> IdentityResources { get; set; } = null!;
    public DbSet<IdentityResourceClaimEntity> IdentityResourceClaims { get; set; } = null!;
    public DbSet<ApiResourceEntity> ApiResources { get; set; } = null!;
    public DbSet<ApiResourceClaimEntity> ApiResourceClaims { get; set; } = null!;
    public DbSet<ApiResourceScopeEntity> ApiResourceScopes { get; set; } = null!;
    public DbSet<ApiResourceSecretEntity> ApiResourceSecrets { get; set; } = null!;
    public DbSet<ApiScopeEntity> ApiScopes { get; set; } = null!;
    public DbSet<ApiScopeClaimEntity> ApiScopeClaims { get; set; } = null!;

    // Configuration Change Log
    public DbSet<ConfigurationChangeLogEntity> ConfigurationChangeLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entities
        ConfigureClients(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigurePersistedGrants(modelBuilder);
        ConfigureResources(modelBuilder);
        ConfigureConfigurationChangeLogs(modelBuilder);
    }

    private void ConfigureClients(ModelBuilder modelBuilder)
    {
        // Client
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientSecret).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClientName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        // ClientGrantType
        modelBuilder.Entity<ClientGrantTypeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GrantType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.AllowedGrantTypes)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClientRedirectUri
        modelBuilder.Entity<ClientRedirectUriEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RedirectUri).IsRequired().HasMaxLength(2000);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.RedirectUris)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClientScope
        modelBuilder.Entity<ClientScopeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Scope).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.AllowedScopes)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ClientClaim
        modelBuilder.Entity<ClientClaimEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(250);
            entity.HasOne(e => e.Client)
                .WithMany(c => c.Claims)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUsers(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        // UserClaim
        modelBuilder.Entity<UserClaimEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(250);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigurePersistedGrants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PersistedGrantEntity>(entity =>
        {
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

    private void ConfigureResources(ModelBuilder modelBuilder)
    {
        // IdentityResource
        modelBuilder.Entity<IdentityResourceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.ShowInDiscoveryDocument).HasDefaultValue(true);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        // IdentityResourceClaim
        modelBuilder.Entity<IdentityResourceClaimEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.IdentityResource)
                .WithMany(ir => ir.UserClaims)
                .HasForeignKey(e => e.IdentityResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiResource
        modelBuilder.Entity<ApiResourceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        // ApiResourceClaim
        modelBuilder.Entity<ApiResourceClaimEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiResource)
                .WithMany(ar => ar.UserClaims)
                .HasForeignKey(e => e.ApiResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiResourceScope
        modelBuilder.Entity<ApiResourceScopeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Scope).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiResource)
                .WithMany(ar => ar.Scopes)
                .HasForeignKey(e => e.ApiResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiResourceSecret
        modelBuilder.Entity<ApiResourceSecretEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasOne(e => e.ApiResource)
                .WithMany(ar => ar.Secrets)
                .HasForeignKey(e => e.ApiResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiScope
        modelBuilder.Entity<ApiScopeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.ShowInDiscoveryDocument).HasDefaultValue(true);
            entity.Property(e => e.Created).HasDefaultValueSql("GETUTCDATE()");
        });

        // ApiScopeClaim
        modelBuilder.Entity<ApiScopeClaimEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.ApiScope)
                .WithMany(s => s.UserClaims)
                .HasForeignKey(e => e.ApiScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureConfigurationChangeLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationChangeLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChangeTime).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ChangedBy).HasMaxLength(200);
            entity.Property(e => e.ChangeDescription).HasMaxLength(1000);
            entity.Property(e => e.OldValues).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValues).HasColumnType("nvarchar(max)");

            // Index
            entity.HasIndex(e => e.ChangeTime);
        });
    }
}