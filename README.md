# OroIdentityServers

A comprehensive, database-agnostic identity server library for ASP.NET Core that implements OAuth 2.0, OAuth 2.1, and OpenID Connect 1.0 standards. Built with extensibility and performance in mind, supporting multiple database providers through Entity Framework Core.

## Features

- **OAuth 2.0 & 2.1 Support**: Complete implementation of authorization code, client credentials, password, and PKCE flows
- **OpenID Connect 1.0**: Full OIDC compliance with discovery, userinfo, and token endpoints
- **Database Agnostic**: Works with SQL Server, PostgreSQL, MySQL, SQLite, and more via EF Core
- **Entity Framework Integration**: Automatic DbContext extension pattern following OpenIddict conventions
- **Security Features**: BCrypt password hashing, JWT token validation, CORS support
- **Extensible Architecture**: Pluggable stores, middleware pipeline, and event system
- **Production Ready**: Automatic migrations, token cleanup, audit logging, and configuration change tracking

## Quick Start

### 1. Install the Package

```bash
dotnet add package OroIdentityServers.EntityFramework
```

### 2. Configure Services

```csharp
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    // or options.UseNpgsql(connectionString);
    // or options.UseMySql(connectionString);
    // or options.UseSqlite(connectionString);
});

builder.Services.AddOroIdentityServer<ApplicationDbContext>();
```

### 3. Configure Middleware

```csharp
app.UseOroIdentityServer();
```

### 4. Create Database Context

```csharp
public class ApplicationDbContext : DbContext, IOroIdentityServerDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Required DbSets
    public DbSet<Client> Clients { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<PersistedGrant> PersistedGrants { get; set; }
    public DbSet<IdentityResource> IdentityResources { get; set; }
    public DbSet<ApiResource> ApiResources { get; set; }
    public DbSet<ApiScope> ApiScopes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOroIdentityServerEntities();
    }
}
```

## Database Providers

### SQL Server

```csharp
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});
```

### PostgreSQL

```csharp
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});
```

### MySQL

```csharp
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});
```

### SQLite

```csharp
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});
```

## Examples

### Client Credentials Flow

```csharp
// Configure client
var client = new Client
{
    ClientId = "client_credentials_client",
    ClientSecret = BCrypt.Net.BCrypt.HashPassword("secret"),
    AllowedGrantTypes = new[] { "client_credentials" },
    AllowedScopes = new[] { "api1" }
};

// Request token
var tokenRequest = new TokenRequest
{
    GrantType = "client_credentials",
    ClientId = "client_credentials_client",
    ClientSecret = "secret",
    Scope = "api1"
};
```

### Authorization Code Flow with PKCE

```csharp
// Configure client
var client = new Client
{
    ClientId = "web_client",
    ClientSecret = BCrypt.Net.BCrypt.HashPassword("secret"),
    AllowedGrantTypes = new[] { "authorization_code", "refresh_token" },
    AllowedScopes = new[] { "openid", "profile", "api1" },
    RedirectUris = new[] { "https://localhost:5001/callback" },
    RequirePkce = true
};

// Authorization request
GET /authorize?
    client_id=web_client&
    response_type=code&
    scope=openid profile api1&
    redirect_uri=https://localhost:5001/callback&
    code_challenge=challenge_value&
    code_challenge_method=S256
```

### Password Flow

```csharp
// Configure client
var client = new Client
{
    ClientId = "password_client",
    ClientSecret = BCrypt.Net.BCrypt.HashPassword("secret"),
    AllowedGrantTypes = new[] { "password" },
    AllowedScopes = new[] { "api1" }
};

// Request token
var tokenRequest = new TokenRequest
{
    GrantType = "password",
    ClientId = "password_client",
    ClientSecret = "secret",
    Username = "user@example.com",
    Password = "password",
    Scope = "api1"
};
```

## Working Examples

The repository includes several working examples:

### SQLite Example (`OroIdentityServerExample`)
- Uses SQLite for development/testing
- Demonstrates EF integration with automatic migrations
- Includes user interface for testing flows
- OpenID Connect discovery endpoint: `/.well-known/openid-configuration`

### PostgreSQL Example (`OroIdentityServerPostgreSQLExample`)
- Production-ready PostgreSQL configuration
- Demonstrates Npgsql provider setup
- Includes connection pooling and optimization

### MySQL Example (`OroIdentityServerMySQLExample`)
- MySQL 8.0+ compatible configuration
- Uses Pomelo.EntityFrameworkCore.MySql provider
- Optimized for MySQL-specific features

### Client Credentials Example (`OroIdentityServerClientCredentialsExample`)
- Blazor WebAssembly client application
- Demonstrates machine-to-machine authentication
- API protection with JWT validation

### Password Flow Example (`OroIdentityServerPasswordExample`)
- Console application demonstrating password grant
- Useful for testing and legacy system integration

### PKCE Example (`OroIdentityServerPKCEExample`)
- Demonstrates Proof Key for Code Exchange
- Enhanced security for public clients
- SPA and mobile app authentication

## API Documentation

### Endpoints

- `/.well-known/openid-configuration` - OpenID Connect discovery
- `/authorize` - Authorization endpoint
- `/token` - Token endpoint
- `/userinfo` - User info endpoint
- `/introspect` - Token introspection
- `/revoke` - Token revocation

### Configuration Options

```csharp
builder.Services.AddOroIdentityServer<ApplicationDbContext>(options =>
{
    options.Issuer = new Uri("https://localhost:5000");
    options.AccessTokenLifetime = TimeSpan.FromHours(1);
    options.RefreshTokenLifetime = TimeSpan.FromDays(30);
    options.AllowPasswordFlow = true;
    options.AllowClientCredentialsFlow = true;
    options.AllowAuthorizationCodeFlow = true;
    options.AllowRefreshTokenFlow = true;
    options.RequirePkce = true;
});
```

## User Management

### Creating Users

```csharp
var user = new User
{
    Subject = Guid.NewGuid().ToString(),
    Username = "user@example.com",
    Email = "user@example.com",
    EmailConfirmed = true,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
    Claims = new[]
    {
        new UserClaim { Type = "name", Value = "John Doe" },
        new UserClaim { Type = "given_name", Value = "John" },
        new UserClaim { Type = "family_name", Value = "Doe" }
    }
};

await userStore.CreateAsync(user);
```

### User Authentication

```csharp
var user = await userStore.FindByUsernameAsync(username);
if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
{
    // Authentication successful
    return user;
}
```

## Testing

Run unit tests:

```bash
dotnet test
```

The test suite includes:
- Unit tests for all stores and services
- Integration tests for OAuth flows
- Database provider compatibility tests
- Security and performance benchmarks

## Security Considerations

- Always use HTTPS in production
- Store secrets securely (Azure Key Vault, AWS Secrets Manager)
- Implement proper CORS policies
- Enable PKCE for public clients
- Regularly rotate signing keys
- Monitor token usage and implement rate limiting
- Use strong password policies
- Implement account lockout mechanisms

## Performance

- Database connection pooling enabled by default
- Automatic token cleanup service removes expired tokens
- EF Core query optimization with compiled queries
- In-memory caching for frequently accessed data
- Async/await throughout for non-blocking I/O

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

MIT License - see LICENSE file for details.

## Support

- Issues: [GitHub Issues](https://github.com/your-repo/issues)
- Documentation: [Wiki](https://github.com/your-repo/wiki)
- Discussions: [GitHub Discussions](https://github.com/your-repo/discussions)

## Roadmap

- [ ] Enhanced UI components for login/consent
- [ ] SAML 2.0 support
- [ ] Multi-tenant architecture
- [ ] Advanced audit logging
- [ ] Integration with ASP.NET Core Identity
- [ ] Docker containerization
- [ ] Kubernetes deployment templates
- [ ] Performance monitoring and metrics
- [ ] FAPI (Financial-grade API) compliance