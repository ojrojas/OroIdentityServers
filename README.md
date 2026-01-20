# OroIdentityServers

A comprehensive, database-agnostic identity server library for ASP.NET Core that implements OAuth 2.0, OAuth 2.1, and OpenID Connect 1.0 standards. Built with extensibility and performance in mind, supporting multiple database providers through Entity Framework Core.

## Features

- **OAuth 2.0 & 2.1 Support**: Complete implementation of authorization code, client credentials, password, and PKCE flows
- **OpenID Connect 1.0**: Full OIDC compliance with discovery, userinfo, and token endpoints
- **Configurable Endpoints**: Dynamic OAuth endpoint configuration instead of hardcoded paths
- **Database Agnostic**: Works with SQL Server, PostgreSQL, MySQL, SQLite, and more via EF Core
- **Entity Framework Integration**: Automatic DbContext extension pattern following OpenIddict conventions
- **Security Features**: BCrypt password hashing, JWT token validation, CORS support, **encrypted client secrets**
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

### Encrypted Client Secrets

OroIdentityServers supports AES encryption for client secrets to enhance security. Client secrets are automatically encrypted when stored in the database and decrypted when retrieved.

```csharp
// Add encryption service with custom key (recommended for production)
builder.Services.AddEncryptionService("your-secure-encryption-key-here");

// Or use default key (development only)
builder.Services.AddEncryptionService();
```

**Security Benefits:**
- Client secrets are encrypted at rest in the database
- Backward compatibility with existing BCrypt-hashed secrets
- AES-256 encryption with PBKDF2 key derivation
- Automatic decryption during client validation

**Migration Note:** Existing client secrets will continue to work. New secrets will be encrypted automatically.

**Complete Example:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure OroIdentityServer
builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>();
builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();

// Add encryption service (recommended)
builder.Services.AddEncryptionService("your-production-encryption-key");

builder.Services.AddOroIdentityServer<ApplicationDbContext>();

var app = builder.Build();

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var seeder = new DatabaseSeeder();
    await seeder.SeedAsync(scope.ServiceProvider);
}

app.UseOroIdentityServer();
app.Run();
```

## Configurable OAuth Endpoints

OroIdentityServers now supports configurable OAuth 2.1 / OpenID Connect endpoints instead of hardcoded paths, providing flexibility to customize endpoint behavior and add new endpoints.

### Key Benefits

- **Flexible Paths**: Change endpoint paths without code modifications
- **Dynamic Configuration**: Enable/disable endpoints based on requirements
- **Extensible**: Add custom endpoints by implementing `IOAuthEndpointHandler`
- **Decoupled Architecture**: Authentication logic separated from entity implementations

### Quick Setup

```csharp
// Add default OAuth endpoints
builder.Services.AddDefaultOAuthEndpoints();

// Use the configurable middleware
app.UseOAuthEndpoints();
```

### Custom Configuration

```csharp
builder.Services.AddOAuthEndpoints(config =>
{
    config.BasePath = "/oauth";

    // Configure token endpoint
    config.AddEndpoint("token", new OAuthEndpoint
    {
        Path = "/token",
        HttpMethods = ["POST"],
        Description = "OAuth 2.1 Token Endpoint"
    });

    // Configure userinfo endpoint
    config.AddEndpoint("userinfo", new OAuthEndpoint
    {
        Path = "/userinfo",
        HttpMethods = ["GET", "POST"],
        Description = "OpenID Connect UserInfo Endpoint"
    });
});
```

### Available Endpoints

- `POST /connect/token` - OAuth 2.1 Token endpoint (client_credentials, password grants)
- `GET/POST /connect/userinfo` - OpenID Connect UserInfo endpoint

See `examples/OroIdentityServerConfigurableExample` for a complete working example.

## Multi-Tenancy Support

OroIdentityServers provides comprehensive multi-tenancy support, enabling you to serve multiple tenants from a single deployment with complete data isolation.

### Features

- **Tenant Resolution**: Multiple strategies for tenant identification (header, domain, query parameter, composite)
- **Data Isolation**: All entities are scoped to specific tenants with automatic filtering
- **Shared Database**: Single database with discriminator columns for cost-effective multi-tenancy
- **Tenant Management**: Full CRUD operations for tenant configuration
- **Domain-based Routing**: Support for tenant-specific domains and subdomains

### Configuration

```csharp
// Configure multi-tenancy with header-based resolution
builder.Services.AddMultiTenancy(options =>
{
    options.ResolutionStrategy = TenantResolutionStrategy.Header;
    options.HeaderName = "X-Tenant-Id";
});

// Or domain-based resolution
builder.Services.AddMultiTenancy(options =>
{
    options.ResolutionStrategy = TenantResolutionStrategy.Domain;
    options.DomainSuffix = ".myapp.com";
});

// Or query parameter resolution
builder.Services.AddMultiTenancy(options =>
{
    options.ResolutionStrategy = TenantResolutionStrategy.QueryParameter;
    options.QueryParameterName = "tenantId";
});

// Add tenant resolution middleware
app.UseTenantResolution();
```

### Tenant Resolution Strategies

1. **Header-based**: `X-Tenant-Id: tenant1`
2. **Domain-based**: `tenant1.myapp.com`
3. **Query Parameter**: `?tenantId=tenant1`
4. **Composite**: Fallback between multiple strategies

### Database Schema

All entities include a `TenantId` foreign key to the `Tenants` table:

```sql
-- Tenants table
CREATE TABLE Tenants (
    Id INT PRIMARY KEY,
    TenantId NVARCHAR(100) UNIQUE NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Domain NVARCHAR(200) NULL,
    ConnectionString NVARCHAR(2000) NULL,
    Enabled BIT DEFAULT 1,
    IsIsolated BIT DEFAULT 0
);

-- All other tables have TenantId FK
ALTER TABLE Clients ADD TenantId NVARCHAR(100) NOT NULL;
ALTER TABLE Clients ADD FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
```

### Usage Example

```csharp
// Tenant context is automatically resolved from HTTP request
// All operations are scoped to the current tenant

// Create tenant-specific client
var client = new Client
{
    ClientId = "tenant-client",
    ClientName = "Tenant Client"
};

await clientStore.CreateClientAsync(client);

// Retrieve tenant-specific users only
var users = await userStore.GetAllUsersAsync(); // Returns only current tenant's users
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

The repository includes several working examples demonstrating different OAuth 2.0 and OpenID Connect flows:

### OroIdentityServerExample
A complete ASP.NET Core web application with SQLite database demonstrating:
- Authorization Code flow with PKCE
- Password flow
- Client Credentials flow
- OpenID Connect user authentication
- Encrypted client secrets
- Event-driven architecture
- Multi-tenancy support
- Automatic database migrations and seeding

**Features:**
- Interactive login page with database validation
- Token endpoint with encrypted client secret validation
- User info endpoint
- Discovery endpoint
- Token cleanup service

**Run the example:**
```bash
cd examples/OroIdentityServerExample
dotnet run
```
Navigate to `http://localhost:5160` for the demo application.

### OroIdentityServerPostgreSQLExample
Full PostgreSQL implementation with connection string configuration and database setup.

### OroIdentityServerMultiTenancyExample
Demonstrates advanced multi-tenancy features with tenant resolution strategies.

### OroIdentityServerPasswordExample
Simple password flow implementation.

### OroIdentityServerPKCEExample
PKCE (Proof Key for Code Exchange) flow demonstration.

### OroIdentityServerClientCredentialsExample
Blazor WebAssembly client credentials flow example.

### OroIdentityServerProtectedAPI
ASP.NET Core API protected by OroIdentityServers tokens.

### Code Examples

#### Client Credentials Flow

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

### Multi-Tenancy Example (`OroIdentityServerMultiTenancyExample`)
- Demonstrates multi-tenant architecture
- Header-based tenant resolution
- Tenant-specific clients, users, and resources
- Event-driven configuration changes

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

### Protected API Example (`OroIdentityServerProtectedAPI`)
- ASP.NET Core Web API protected by OroIdentityServer
- JWT Bearer authentication
- Demonstrates API protection with token validation

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
- **Encrypt client secrets** using the built-in AES encryption service

## Performance

- Database connection pooling enabled by default
- Automatic token cleanup service removes expired tokens
- EF Core query optimization with compiled queries
- In-memory caching for frequently accessed data
- Async/await throughout for non-blocking I/O

## Event-Driven Architecture

OroIdentityServers includes a comprehensive event-driven architecture for microservices integration, enabling real-time notifications and cross-service communication.

### Features

- **Event Bus**: In-memory event bus for local event distribution
- **Message Brokers**: Support for RabbitMQ and Azure Service Bus
- **Event Sourcing**: Persistent event storage with Entity Framework
- **Domain Events**: Rich domain events for all identity operations
- **Webhook Integration**: HTTP webhook notifications for external services
- **Event Store**: Query historical events for audit and replay

### Domain Events

The following domain events are automatically published:

- **Client Events**: `ClientCreatedEvent`, `ClientUpdatedEvent`, `ClientDeletedEvent`
- **User Events**: `UserCreatedEvent`, `UserUpdatedEvent`, `UserDeletedEvent`
- **Token Events**: `AccessTokenIssuedEvent`, `RefreshTokenIssuedEvent`, `TokenRevokedEvent`
- **Authentication Events**: `UserLoginEvent`, `UserLogoutEvent`
- **Authorization Events**: `AuthorizationGrantedEvent`, `AuthorizationDeniedEvent`

### Configuration

```csharp
// Add in-memory event bus (default)
builder.Services.AddInMemoryEventBus();

// Add RabbitMQ message broker
builder.Services.AddRabbitMqMessageBroker(builder.Configuration);

// Add Azure Service Bus message broker
builder.Services.AddAzureServiceBusMessageBroker(builder.Configuration);

// Add event store for event sourcing
builder.Services.AddEventStore();

// Add event publisher with webhooks
builder.Services.AddEventPublisher(builder.Configuration);

// Complete event-driven setup
builder.Services.AddEventDrivenArchitecture(builder.Configuration);
```

### Webhook Configuration

Configure webhook URLs in `appsettings.json`:

```json
{
  "EventPublisher": {
    "WebhookUrls": [
      "https://api.example.com/webhooks/identity-events",
      "https://service.example.com/events"
    ]
  }
}
```

### Message Broker Configuration

#### RabbitMQ
```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

#### Azure Service Bus
```json
{
  "AzureServiceBus": {
    "ConnectionString": "your-connection-string",
    "TopicName": "identity-events"
  }
}
```

### Event Store Queries

```csharp
// Get events for a specific aggregate
var events = await eventStore.GetEventsAsync(clientId);

// Get events by type
var tokenEvents = await eventStore.GetEventsByTypeAsync("AccessTokenIssuedEvent");

// Replay events for event sourcing
foreach (var @event in events)
{
    await eventHandler.HandleAsync(@event);
}
```

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