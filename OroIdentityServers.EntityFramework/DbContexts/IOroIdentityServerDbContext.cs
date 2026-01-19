using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServers.EntityFramework.DbContexts;

/// <summary>
/// Interface that defines the required DbSets for OroIdentityServer functionality.
/// Implement this interface in your DbContext to integrate OroIdentityServer entities.
/// </summary>
public interface IOroIdentityServerDbContext
{
    DbSet<ClientEntity> Clients { get; }
    DbSet<ClientGrantTypeEntity> ClientGrantTypes { get; }
    DbSet<ClientRedirectUriEntity> ClientRedirectUris { get; }
    DbSet<ClientScopeEntity> ClientScopes { get; }
    DbSet<ClientClaimEntity> ClientClaims { get; }

    DbSet<UserEntity> Users { get; }
    DbSet<UserClaimEntity> UserClaims { get; }

    DbSet<PersistedGrantEntity> PersistedGrants { get; }

    DbSet<IdentityResourceEntity> IdentityResources { get; }
    DbSet<IdentityResourceClaimEntity> IdentityResourceClaims { get; }

    DbSet<ApiResourceEntity> ApiResources { get; }
    DbSet<ApiResourceClaimEntity> ApiResourceClaims { get; }
    DbSet<ApiResourceScopeEntity> ApiResourceScopes { get; }
    DbSet<ApiResourceSecretEntity> ApiResourceSecrets { get; }

    DbSet<ApiScopeEntity> ApiScopes { get; }
    DbSet<ApiScopeClaimEntity> ApiScopeClaims { get; }

    DbSet<ConfigurationChangeLogEntity> ConfigurationChangeLogs { get; }

    DbSet<EventEntity> Events { get; }

    DbSet<TenantEntity> Tenants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}