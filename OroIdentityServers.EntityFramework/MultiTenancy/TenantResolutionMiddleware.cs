using Microsoft.AspNetCore.Http;

namespace OroIdentityServers.EntityFramework.MultiTenancy;

/// <summary>
/// Middleware for resolving and setting the current tenant context
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantResolver = context.RequestServices.GetRequiredService<ITenantResolver>();
        var tenantId = await tenantResolver.GetCurrentTenantIdAsync();

        if (!string.IsNullOrEmpty(tenantId))
        {
            // Store tenant ID in HttpContext.Items for easy access
            context.Items["TenantId"] = tenantId;

            // Optionally store in user claims for JWT tokens
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claimsIdentity = context.User.Identities.FirstOrDefault();
                if (claimsIdentity != null && !claimsIdentity.HasClaim(c => c.Type == "tenant_id"))
                {
                    claimsIdentity.AddClaim(new System.Security.Claims.Claim("tenant_id", tenantId));
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding tenant resolution middleware
/// </summary>
public static class TenantResolutionMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant resolution middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
}