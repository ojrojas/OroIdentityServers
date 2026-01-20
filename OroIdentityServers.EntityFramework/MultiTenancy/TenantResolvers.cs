using Microsoft.AspNetCore.Http;

namespace OroIdentityServers.EntityFramework.MultiTenancy;

/// <summary>
/// Tenant resolver that uses HTTP headers to determine the current tenant
/// </summary>
public class HeaderTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;

    public HeaderTenantResolver(IHttpContextAccessor httpContextAccessor, string headerName = "X-Tenant-Id")
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = headerName;
    }

    public string? GetCurrentTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request?.Headers?.TryGetValue(_headerName, out var tenantId) == true)
        {
            return tenantId.ToString();
        }

        // Fallback to claim
        return context?.User?.FindFirst("tenant_id")?.Value;
    }

    public Task<string?> GetCurrentTenantIdAsync()
    {
        return Task.FromResult(GetCurrentTenantId());
    }
}

/// <summary>
/// Tenant resolver that uses domain/subdomain to determine the current tenant
/// </summary>
public class DomainTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _defaultTenantId;

    public DomainTenantResolver(IHttpContextAccessor httpContextAccessor, string defaultTenantId = "default")
    {
        _httpContextAccessor = httpContextAccessor;
        _defaultTenantId = defaultTenantId;
    }

    public string? GetCurrentTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request?.Host.Host == null)
        {
            return _defaultTenantId;
        }

        var host = context.Request.Host.Host.ToLower();

        // Extract subdomain (e.g., tenant1.example.com -> tenant1)
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }

        // If no subdomain, use default tenant
        return _defaultTenantId;
    }

    public Task<string?> GetCurrentTenantIdAsync()
    {
        return Task.FromResult(GetCurrentTenantId());
    }
}

/// <summary>
/// Tenant resolver that uses query parameters
/// </summary>
public class QueryParameterTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _parameterName;

    public QueryParameterTenantResolver(IHttpContextAccessor httpContextAccessor, string parameterName = "tenantId")
    {
        _httpContextAccessor = httpContextAccessor;
        _parameterName = parameterName;
    }

    public string? GetCurrentTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request?.Query?.TryGetValue(_parameterName, out var tenantId) == true)
        {
            return tenantId.ToString();
        }

        return null;
    }

    public Task<string?> GetCurrentTenantIdAsync()
    {
        return Task.FromResult(GetCurrentTenantId());
    }
}

/// <summary>
/// Composite tenant resolver that tries multiple strategies
/// </summary>
public class CompositeTenantResolver : ITenantResolver
{
    private readonly IEnumerable<ITenantResolver> _resolvers;

    public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    public string? GetCurrentTenantId()
    {
        foreach (var resolver in _resolvers)
        {
            var tenantId = resolver.GetCurrentTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }

        return null;
    }

    public async Task<string?> GetCurrentTenantIdAsync()
    {
        foreach (var resolver in _resolvers)
        {
            var tenantId = await resolver.GetCurrentTenantIdAsync();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }

        return null;
    }
}