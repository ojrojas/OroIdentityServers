# OroIdentityServers

A C# library for building identity servers that support OpenID, OAuth 2.0, and OAuth 2.1 authentication flows. This library can be used to transform a web application into an identity server.

## Project Structure

- **OroIdentityServers**: Main library project that combines all components.
- **OroIdentityServers.Core**: Core classes and interfaces for identity server functionality.
- **OroIdentityServers.OAuth**: OAuth 2.0 and 2.1 specific implementations.
- **OroIdentityServers.OpenId**: OpenID Connect implementations.

## Getting Started

Choose one of the following integration approaches:

### Option 1: Integrate with Existing DbContext (Recommended)

If you already have an EF Core DbContext, integrate OroIdentityServer entities into it:

```csharp
using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.Extensions;

public class ApplicationDbContext : DbContext, IOroIdentityServerDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Your existing entities...
    public DbSet<MyEntity> MyEntities { get; set; }

    // OroIdentityServer entities (added automatically)
    public DbSet<ClientEntity> Clients { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<PersistedGrantEntity> PersistedGrants { get; set; }
    // ... other required entities

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add OroIdentityServer entities to your model
        modelBuilder.AddOroIdentityServerEntities();
    }
}

// In Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)); // Or your preferred provider

builder.Services.AddOroIdentityServerDbContext<ApplicationDbContext>();
builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();
builder.Services.AddAutomaticMigrations<ApplicationDbContext>(); // Optional: auto-apply migrations
builder.Services.AddTokenCleanupService(); // Optional: auto-cleanup expired tokens

// Create migrations in your application
// dotnet ef migrations add InitialCreate
// dotnet ef database update
```

### Option 2: Separate DbContext

For a dedicated identity server database:

```csharp
builder.Services.AddOroIdentityServerDbContext(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();
```

### Option 3: In-Memory Configuration (Development Only)

```csharp
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://localhost:5001";
    options.Clients = new List<Client> { /* ... */ };
    options.Users = new List<User> { /* ... */ };
});
```

## User Management

The library is designed to be user-agnostic. Any class that implements the `IUser` interface can be used as a user in the identity server. The application is responsible for managing user classes, storing user data, and handling password validation.

### Implementing Custom User Classes

To use custom user classes, implement the `IUser` interface:

```csharp
using System.Security.Claims;
using OroIdentityServers.Core;

public class MyUser : IUser
{
    public string Id { get; set; }
    public string Username { get; set; }
    public IEnumerable<Claim> Claims { get; set; }
    
    public bool ValidatePassword(string password)
    {
        // Implement your password validation logic here
        // For example, using BCrypt or other hashing
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }
    
    // Additional properties
    public string PasswordHash { get; set; }
    public string Email { get; set; }
}
```

Then, implement `IUserStore` to provide user lookup:

```csharp
public class MyUserStore : IUserStore
{
    // Implement FindUserByUsernameAsync and FindUserByIdAsync
    // using your data storage (database, etc.)
}
```

The identity server does not handle user registration or password management; that is the responsibility of the application developer.

## Database-Agnostic Storage with Events

OroIdentityServers supports database-agnostic storage using Entity Framework Core, allowing you to use any database provider (SQL Server, PostgreSQL, MySQL, Oracle, SQLite, etc.). The system includes real-time event notifications for configuration changes.

### Setting Up Database Storage

You have two options for integrating OroIdentityServer with your database:

#### Option 1: Separate DbContext (Recommended for new projects)

1. **Install EF Core Provider**: Add the EF Core provider package for your database:
   ```bash
   # For SQL Server
   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
   
   # For PostgreSQL
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   
   # For MySQL
   dotnet add package Pomelo.EntityFrameworkCore.MySql
   
   # For Oracle
   dotnet add package Oracle.EntityFrameworkCore
   ```

2. **Configure DbContext**: Set up the database context with your provider:
   ```csharp
   builder.Services.AddOroIdentityServerDbContext(options =>
   {
       options.UseSqlServer(connectionString);
       // or options.UseNpgsql(connectionString);
       // or options.UseMySql(connectionString);
       // etc.
   });
   ```

3. **Add Stores and Events**: Register the EF stores and event system:
   ```csharp
   builder.Services.AddEntityFrameworkStores();
   builder.Services.AddConfigurationEvents(); // In-memory event handling
   ```

4. **Create Database Migrations**: Generate and apply migrations in your application project:
   ```bash
   # Add Microsoft.EntityFrameworkCore.Design to your application project
   dotnet add package Microsoft.EntityFrameworkCore.Design
   
   # Generate initial migration
   dotnet ef migrations add InitialCreate --project YourApp.csproj --startup-project YourApp.csproj
   
   # Apply migrations
   dotnet ef database update
   ```

#### Option 2: Integrate with Your Existing DbContext (Recommended for existing projects)

If you already have a DbContext in your application, you can add OroIdentityServer entities directly to it:

1. **Install EF Core Provider**: Same as Option 1.

2. **Modify Your DbContext**: Add OroIdentityServer entities and use the extension method:
   ```csharp
   using OroIdentityServers.EntityFramework.Extensions;
   
   public class ApplicationDbContext : DbContext
   {
       public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
           : base(options)
       {
       }
   
       // OroIdentityServer entities
       public DbSet<ClientEntity> Clients => Set<ClientEntity>();
       public DbSet<UserEntity> Users => Set<UserEntity>();
       // ... add other entities as needed
   
       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           base.OnModelCreating(modelBuilder);
           
           // Add OroIdentityServer entities to your model
           modelBuilder.AddOroIdentityServerEntities();
           
           // Your existing entity configurations...
       }
   }
   ```

3. **Configure Services**: Register your DbContext and OroIdentityServer services:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
   {
       options.UseSqlServer(connectionString);
   });
   
   builder.Services.AddEntityFrameworkStores();
   builder.Services.AddConfigurationEvents();
   ```

4. **Create Migrations**: Generate migrations from your application project:
   ```bash
   dotnet ef migrations add AddIdentityServerEntities --project YourApp.csproj
   dotnet ef database update
   ```

### Complete EF Example (Separate DbContext)

```csharp
using Microsoft.EntityFrameworkCore;
using OroIdentityServers;
using OroIdentityServers.EntityFramework.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure database (choose your provider)
builder.Services.AddOroIdentityServerDbContext(options =>
{
    options.UseSqlServer("your-connection-string-here");
});

// Add EF stores and events
builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();

// Configure Identity Server (clients/users managed via database)
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://your-domain.com";
    options.Audience = "api";
    options.SecretKey = "your-secret-key";
    
    // Empty collections - data comes from database
    options.Clients = new List<Client>();
    options.Users = new List<User>();
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OroIdentityServerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseOroIdentityServer();
app.Run();
```

### Complete EF Example (Integrated DbContext)

```csharp
using Microsoft.EntityFrameworkCore;
using OroIdentityServers;
using OroIdentityServers.EntityFramework.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure your application's DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer("your-connection-string-here");
});

// Add EF stores and events
builder.Services.AddEntityFrameworkStores();
builder.Services.AddConfigurationEvents();

// Configure Identity Server
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://your-domain.com";
    options.Audience = "api";
    options.SecretKey = "your-secret-key";
    
    // Empty collections - data comes from database
    options.Clients = new List<Client>();
    options.Users = new List<User>();
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseOroIdentityServer();
app.Run();

// Your application's DbContext with OroIdentityServer entities
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // OroIdentityServer entities
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();
    public DbSet<ClientGrantTypeEntity> ClientGrantTypes => Set<ClientGrantTypeEntity>();
    public DbSet<ClientRedirectUriEntity> ClientRedirectUris => Set<ClientRedirectUriEntity>();
    public DbSet<ClientScopeEntity> ClientScopes => Set<ClientScopeEntity>();
    public DbSet<ClientClaimEntity> ClientClaims => Set<ClientClaimEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserClaimEntity> UserClaims => Set<UserClaimEntity>();

    public DbSet<PersistedGrantEntity> PersistedGrants => Set<PersistedGrantEntity>();

    // Add other entities as needed...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Add OroIdentityServer entities to your model
        modelBuilder.AddOroIdentityServerEntities();
        
        // Your existing entity configurations...
    }
}
```

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OroIdentityServerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseOroIdentityServer();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### Event-Driven Configuration Changes

The system automatically detects and notifies about configuration changes:

- **Client Configuration Changes**: When client settings are modified
- **User Configuration Changes**: When user data is updated  
- **Flow Changes**: Specific events for grant type modifications
- **Real-time Notifications**: Immediate cache invalidation and system updates

### Managing Data via Stores

Access the stores to manage clients, users, and grants programmatically:

```csharp
// Get services
var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();
var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
var grantStore = scope.ServiceProvider.GetRequiredService<IPersistedGrantStore>();

// Create a client
var client = new Client
{
    ClientId = "my-client",
    ClientSecret = "secret",
    AllowedGrantTypes = new[] { "authorization_code" },
    RedirectUris = new[] { "https://myapp.com/callback" },
    AllowedScopes = new[] { "openid", "profile" }
};

await clientStore.CreateClientAsync(client, "admin");

// Create a user
var user = new UserEntity
{
    Username = "john",
    PasswordHash = "hashed-password",
    Email = "john@example.com"
};

await userStore.CreateUserAsync(user, "admin");
```

## Supported Flows

- Discovery: `GET https://localhost:5001/.well-known/openid-configuration`
- Authorize: `GET https://localhost:5001/connect/authorize?client_id=web-client&response_type=code&redirect_uri=https://localhost:3000/callback&scope=openid profile`
- Token: `POST https://localhost:5001/connect/token` with appropriate parameters.

### Token Endpoint

The library provides a `/connect/token` endpoint that supports multiple grant types:

- `client_credentials`: For machine-to-machine communication.
- `authorization_code`: For web applications.
- `refresh_token`: To refresh expired access tokens.

Example requests:

**Client Credentials:**
```
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=client1&client_secret=secret1
```

**Authorization Code:**
```
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&client_id=client1&client_secret=secret1&code=<auth_code>&redirect_uri=https://localhost:5002/callback
```

**Refresh Token:**
```
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&client_id=client1&client_secret=secret1&refresh_token=<refresh_token>
```

Response:

```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "id_token": "eyJ...",  // Only for authorization_code
  "refresh_token": "..." // For authorization_code and refresh_token
}
```

### Authorize Endpoint

GET `/connect/authorize` for initiating authorization code flow.

Example: `GET /connect/authorize?client_id=client1&response_type=code&redirect_uri=https://localhost:5002/callback&scope=openid profile&state=xyz`

### UserInfo Endpoint

GET `/connect/userinfo` to retrieve user claims (requires Bearer token).

### Discovery Endpoint

GET `/.well-known/openid-configuration` for OpenID Connect metadata.

## Features

- **OAuth 2.1 Compliance**: Full support for OAuth 2.1 flows including PKCE (Proof Key for Code Exchange)
- **Grant Types**: `authorization_code` (with PKCE), `client_credentials`, `password`, `refresh_token`
- **OpenID Connect**: ID tokens, discovery endpoint (`/.well-known/openid-configuration`)
- **Security**: JWT access tokens and ID tokens with configurable signing keys
- **Storage**: In-memory stores for clients, users, and grants (extensible for custom implementations)
- **Authentication**: Cookie-based authentication for web clients, JWT Bearer for API access
- **Examples**: Complete working examples for all supported flows

### OAuth 2.1 Flows Supported

1. **Authorization Code with PKCE** - For web applications and SPAs (recommended)
2. **Client Credentials** - For machine-to-machine communication
3. **Password Grant** - For trusted first-party applications
4. **Refresh Token** - For obtaining new access tokens without re-authentication

### PKCE Security Enhancement

PKCE (RFC 7636) is implemented and required for the authorization code flow, providing protection against authorization code interception attacks in public clients.

## Examples

## Database Setup and Seeding

### Creating Migrations

When integrating with your DbContext, create and apply migrations:

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Apply to database
dotnet ef database update
```

### Seeding Initial Data

Create a seeding service to populate initial clients, users, and resources:

```csharp
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();

        // Seed identity resources
        if (!context.IdentityResources.Any())
        {
            context.IdentityResources.AddRange(
                new IdentityResourceEntity { Name = "openid", DisplayName = "OpenID" },
                new IdentityResourceEntity { Name = "profile", DisplayName = "Profile" }
            );
            await context.SaveChangesAsync();
        }

        // Seed API scopes
        if (!context.ApiScopes.Any())
        {
            context.ApiScopes.Add(new ApiScopeEntity { Name = "api", DisplayName = "API Access" });
            await context.SaveChangesAsync();
        }

        // Seed clients
        await clientStore.CreateClientAsync(new Client
        {
            ClientId = "web-client",
            ClientSecret = "web-secret",
            AllowedGrantTypes = new[] { "authorization_code", "refresh_token" },
            RedirectUris = new[] { "https://localhost:3000/callback" },
            AllowedScopes = new[] { "openid", "profile", "api" }
        });

        // Seed users
        var user = new UserEntity
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Email = "admin@example.com",
            Enabled = true
        };
        await userStore.CreateUserAsync(user);
    }
}

// In Program.cs
var app = builder.Build();

// Seed database
await DatabaseSeeder.SeedAsync(app.Services);

app.UseOroIdentityServer();
```

The `examples/` directory contains complete working applications demonstrating each OAuth flow:

- **OroIdentityServerExample**: Main identity server with web interface
- **OroIdentityServerClientCredentialsExample**: Blazor WebAssembly app using client credentials flow
- **OroIdentityServerPasswordExample**: Console app demonstrating password grant flow
- **OroIdentityServerPKCEExample**: Console app showing authorization code flow with PKCE

To run an example:
```bash
cd examples/OroIdentityServerExample
dotnet run
```

Then in another terminal:
```bash
cd examples/OroIdentityServerPKCEExample
dotnet run
```

## Building

Run `dotnet build` to build the solution.

## Testing

Run `dotnet test` to execute unit tests.

## Future Enhancements

- **UI for Login and Consent**: Implement Razor pages or MVC views for user authentication and consent screens.
- **Database Persistence**: Add Entity Framework implementations for stores.
- **PKCE Support**: Implement Proof Key for Code Exchange for enhanced security.
- **Advanced Security**: Add CORS policies, rate limiting, and CSRF protection.
- **Custom Grant Types**: Allow extension for custom OAuth flows.
- **Events and Hooks**: Add event system for customization.
- **Certificate-Based Signing**: Support for RSA keys and certificates.
- **Multi-Tenancy**: Support for multiple tenants.
- **Integration with ASP.NET Identity**: Seamless integration with existing user management.

Contributions are welcome. Please submit issues and pull requests.