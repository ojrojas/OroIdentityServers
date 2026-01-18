# OroIdentityServers

A C# library for building identity servers that support OpenID, OAuth 2.0, and OAuth 2.1 authentication flows. This library can be used to transform a web application into an identity server.

## Project Structure

- **OroIdentityServers**: Main library project that combines all components.
- **OroIdentityServers.Core**: Core classes and interfaces for identity server functionality.
- **OroIdentityServers.OAuth**: OAuth 2.0 and 2.1 specific implementations.
- **OroIdentityServers.OpenId**: OpenID Connect implementations.

## Getting Started

1. Add the library to your ASP.NET Core project.
2. Configure the identity server in your `Program.cs` or `Startup.cs`.

### Complete Example

Create a new ASP.NET Core Web API project and add the library:

```csharp
// In Program.cs
using OroIdentityServers;

var builder = WebApplication.CreateBuilder(args);

// Configure Identity Server
builder.Services.AddOroIdentityServer(options =>
{
    options.Issuer = "https://localhost:5001";
    options.Audience = "api";
    options.SecretKey = "your-very-long-secret-key-at-least-32-characters";
    options.Clients = new List<Client>
    {
        new Client
        {
            ClientId = "web-client",
            ClientSecret = "web-secret",
            AllowedGrantTypes = new List<string> { "authorization_code", "refresh_token" },
            RedirectUris = new List<string> { "https://localhost:3000/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "api" }
        },
        new Client
        {
            ClientId = "api-client",
            ClientSecret = "api-secret",
            AllowedGrantTypes = new List<string> { "client_credentials" },
            AllowedScopes = new List<string> { "api" }
        }
    };
    options.Users = new List<User>
    {
        new User
        {
            Id = "user1",
            Username = "alice",
            PasswordHash = "password", // Use secure hashing in production
            Claims = new List<string> { "name:Alice", "email:alice@example.com" }
        }
    };
});

builder.Services.AddControllers();

var app = builder.Build();

// Use Identity Server middleware
app.UseOroIdentityServer();

app.UseAuthorization();
app.MapControllers();

app.Run();
```

Start the application and test the endpoints:

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