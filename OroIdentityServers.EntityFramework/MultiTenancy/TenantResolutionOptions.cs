namespace OroIdentityServers.EntityFramework.MultiTenancy;

/// <summary>
/// Options for tenant resolution middleware.
/// </summary>
public class TenantResolutionOptions
{
    /// <summary>
    /// The header name to use for header-based tenant resolution.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// The query parameter name to use for query parameter-based tenant resolution.
    /// </summary>
    public string QueryParameterName { get; set; } = "tenantId";

    /// <summary>
    /// The domain suffix to use for domain-based tenant resolution.
    /// </summary>
    public string DomainSuffix { get; set; } = ".example.com";
}