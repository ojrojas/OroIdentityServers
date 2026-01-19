using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OroIdentityServers.EntityFramework.MultiTenancy;
using OroIdentityServers.EntityFramework.Services;
using OroIdentityServers.EntityFramework.Stores;

namespace OroIdentityServers.EntityFramework.Extensions;

/// <summary>
/// Extension methods for configuring multi-tenancy in OroIdentityServers.
/// </summary>
public static class MultiTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Adds multi-tenancy support to the identity server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureTenantResolver">Action to configure the tenant resolver.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        Action<MultiTenancyOptions> configureOptions)
    {
        var options = new MultiTenancyOptions();
        configureOptions(options);

        // Register tenant resolver based on configuration
        switch (options.ResolutionStrategy)
        {
            case TenantResolutionStrategy.Header:
                services.AddScoped<ITenantResolver, HeaderTenantResolver>();
                break;
            case TenantResolutionStrategy.Domain:
                services.AddScoped<ITenantResolver, DomainTenantResolver>();
                break;
            case TenantResolutionStrategy.QueryParameter:
                services.AddScoped<ITenantResolver, QueryParameterTenantResolver>();
                break;
            case TenantResolutionStrategy.Composite:
                services.AddScoped<ITenantResolver, CompositeTenantResolver>();
                break;
            default:
                throw new ArgumentException($"Unsupported tenant resolution strategy: {options.ResolutionStrategy}");
        }

        // Register tenant store
        services.AddScoped<ITenantStore, EntityFrameworkTenantStore>();

        // Configure tenant resolution middleware options
        services.Configure<TenantResolutionOptions>(opts =>
        {
            opts.HeaderName = options.HeaderName;
            opts.QueryParameterName = options.QueryParameterName;
            opts.DomainSuffix = options.DomainSuffix;
        });

        return services;
    }

    /// <summary>
    /// Adds tenant resolution middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }
}

/// <summary>
/// Options for configuring multi-tenancy.
/// </summary>
public class MultiTenancyOptions
{
    /// <summary>
    /// The strategy to use for resolving tenants.
    /// </summary>
    public TenantResolutionStrategy ResolutionStrategy { get; set; } = TenantResolutionStrategy.Header;

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

/// <summary>
/// Tenant resolution strategies.
/// </summary>
public enum TenantResolutionStrategy
{
    /// <summary>
    /// Resolve tenant from HTTP header.
    /// </summary>
    Header,

    /// <summary>
    /// Resolve tenant from domain/subdomain.
    /// </summary>
    Domain,

    /// <summary>
    /// Resolve tenant from query parameter.
    /// </summary>
    QueryParameter,

    /// <summary>
    /// Use composite resolution with fallback strategies.
    /// </summary>
    Composite
}