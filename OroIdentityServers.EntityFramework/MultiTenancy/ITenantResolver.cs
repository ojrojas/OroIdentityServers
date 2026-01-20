namespace OroIdentityServers.EntityFramework.MultiTenancy;

/// <summary>
/// Interface for resolving the current tenant
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    string? GetCurrentTenantId();

    /// <summary>
    /// Gets the current tenant ID asynchronously
    /// </summary>
    Task<string?> GetCurrentTenantIdAsync();
}

/// <summary>
/// Interface for tenant store operations
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Finds a tenant by ID
    /// </summary>
    Task<Tenant?> FindTenantByIdAsync(string tenantId);

    /// <summary>
    /// Finds a tenant by domain
    /// </summary>
    Task<Tenant?> FindTenantByDomainAsync(string domain);

    /// <summary>
    /// Gets all tenants
    /// </summary>
    Task<IEnumerable<Tenant>> GetAllTenantsAsync();

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    Task CreateTenantAsync(Tenant tenant);

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    Task UpdateTenantAsync(Tenant tenant);

    /// <summary>
    /// Deletes a tenant
    /// </summary>
    Task DeleteTenantAsync(string tenantId);
}

/// <summary>
/// Tenant information
/// </summary>
public class Tenant
{
    /// <summary>
    /// Unique tenant identifier
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Domain name
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Connection string for isolated database
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether the tenant is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether the tenant has its own database
    /// </summary>
    public bool IsIsolated { get; set; } = false;

    /// <summary>
    /// Tenant-specific configuration
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? LastModified { get; set; }
}