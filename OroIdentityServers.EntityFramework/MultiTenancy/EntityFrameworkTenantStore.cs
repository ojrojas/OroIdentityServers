using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServers.EntityFramework.MultiTenancy;

/// <summary>
/// Entity Framework implementation of ITenantStore
/// </summary>
public class EntityFrameworkTenantStore : ITenantStore
{
    private readonly IOroIdentityServerDbContext _context;

    public EntityFrameworkTenantStore(IOroIdentityServerDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> FindTenantByIdAsync(string tenantId)
    {
        var entity = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Enabled);

        return entity != null ? MapToTenant(entity) : null;
    }

    public async Task<Tenant?> FindTenantByDomainAsync(string domain)
    {
        var entity = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Domain == domain && t.Enabled);

        return entity != null ? MapToTenant(entity) : null;
    }

    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
    {
        var entities = await _context.Tenants
            .Where(t => t.Enabled)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return entities.Select(MapToTenant);
    }

    public async Task CreateTenantAsync(Tenant tenant)
    {
        var entity = new TenantEntity
        {
            TenantId = tenant.TenantId,
            Name = tenant.Name,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ConnectionString = tenant.ConnectionString,
            Enabled = tenant.Enabled,
            IsIsolated = tenant.IsIsolated,
            Configuration = tenant.Configuration,
            Created = tenant.Created
        };

        _context.Tenants.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTenantAsync(Tenant tenant)
    {
        var entity = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenant.TenantId);

        if (entity == null)
        {
            throw new InvalidOperationException($"Tenant '{tenant.TenantId}' not found");
        }

        entity.Name = tenant.Name;
        entity.Description = tenant.Description;
        entity.Domain = tenant.Domain;
        entity.ConnectionString = tenant.ConnectionString;
        entity.Enabled = tenant.Enabled;
        entity.IsIsolated = tenant.IsIsolated;
        entity.Configuration = tenant.Configuration;
        entity.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTenantAsync(string tenantId)
    {
        var entity = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        if (entity == null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found");
        }

        _context.Tenants.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private static Tenant MapToTenant(TenantEntity entity)
    {
        return new Tenant
        {
            TenantId = entity.TenantId,
            Name = entity.Name,
            Description = entity.Description,
            Domain = entity.Domain,
            ConnectionString = entity.ConnectionString,
            Enabled = entity.Enabled,
            IsIsolated = entity.IsIsolated,
            Configuration = entity.Configuration,
            Created = entity.Created,
            LastModified = entity.LastModified
        };
    }
}