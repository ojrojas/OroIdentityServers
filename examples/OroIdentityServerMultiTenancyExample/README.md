# OroIdentityServerMultiTenancyExample

This example demonstrates how to configure and use OroIdentityServers with multi-tenancy support.

## Features Demonstrated

- Multi-tenant identity server configuration
- Tenant resolution strategies (header, domain, query parameter)
- Tenant-specific client and user management
- Data isolation between tenants

## Configuration

### 1. Configure Multi-Tenancy

```csharp
builder.Services.AddMultiTenancy(options =>
{
    options.ResolutionStrategy = TenantResolutionStrategy.Header;
    options.HeaderName = "X-Tenant-Id";
});
```

### 2. Add Tenant Resolution Middleware

```csharp
app.UseTenantResolution();
```

### 3. Seed Tenants and Data

```csharp
// Seed tenants
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Create tenants
    context.Tenants.AddRange(
        new TenantEntity
        {
            TenantId = "tenant1",
            Name = "Tenant 1",
            Domain = "tenant1.example.com",
            Enabled = true
        },
        new TenantEntity
        {
            TenantId = "tenant2",
            Name = "Tenant 2",
            Domain = "tenant2.example.com",
            Enabled = true
        }
    );

    await context.SaveChangesAsync();

    // Seed tenant-specific data
    await SeedTenantDataAsync(context, "tenant1");
    await SeedTenantDataAsync(context, "tenant2");
}
```

## Testing Multi-Tenancy

### Header-based Resolution

```bash
# Request for tenant1
curl -H "X-Tenant-Id: tenant1" https://your-server/token

# Request for tenant2
curl -H "X-Tenant-Id: tenant2" https://your-server/token
```

### Domain-based Resolution

```bash
# Request for tenant1
curl https://tenant1.example.com/token

# Request for tenant2
curl https://tenant2.example.com/token
```

## Security Considerations

- All data operations are automatically scoped to the current tenant
- Cross-tenant access is prevented at the database level
- Tenant context must be established for all operations
- Use HTTPS in production for header-based resolution