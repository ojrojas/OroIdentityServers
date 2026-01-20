otnet# OroIdentityServer Configurable Example

This example demonstrates how to use the new configurable OAuth 2.1 / OpenID Connect endpoints architecture in OroIdentityServers.

## Overview

The new architecture allows you to configure OAuth endpoints dynamically instead of using hardcoded paths. This provides flexibility to:

- Change endpoint paths
- Add new endpoints
- Enable/disable specific endpoints
- Decouple authentication logic from specific user entities

## Key Components

### 1. IUserAuthenticationService
Interface for user authentication and management that decouples authentication logic from specific entity implementations.

### 2. OAuthEndpointsConfiguration
Configuration class that manages OAuth endpoint definitions with paths, HTTP methods, and handlers.

### 3. OAuthEndpointsMiddleware
Generic middleware that routes requests to configured endpoints based on path and method matching.

### 4. Endpoint Handlers
Concrete implementations for specific OAuth endpoints:
- `TokenEndpointHandler`: Handles OAuth token requests
- `UserInfoEndpointHandler`: Handles OpenID Connect UserInfo requests

## Configuration

### Basic Setup

```csharp
builder.Services.AddDefaultOAuthEndpoints();
app.UseOAuthEndpoints();
```

### Custom Configuration

```csharp
builder.Services.AddOAuthEndpoints(config =>
{
    config.BasePath = "/connect";

    config.AddEndpoint("token", new OAuthEndpoint
    {
        Path = "/token",
        HttpMethods = ["POST"],
        Handler = null!, // Resolved from DI
        Description = "OAuth 2.1 Token Endpoint"
    });

    config.AddEndpoint("userinfo", new OAuthEndpoint
    {
        Path = "/userinfo",
        HttpMethods = ["GET", "POST"],
        Handler = null!, // Resolved from DI
        Description = "OpenID Connect UserInfo Endpoint"
    });
});
```

## Running the Example

1. Build the project:
```bash
dotnet build
```

2. Run the application:
```bash
dotnet run --project examples/OroIdentityServerConfigurableExample
```

3. Test the endpoints:

### Token Endpoint
```bash
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=test-client&client_secret=test-secret"
```

### UserInfo Endpoint
```bash
curl -H "Authorization: Bearer <access_token>" \
  http://localhost:5000/connect/userinfo
```

## Available Endpoints

- `POST /connect/token` - OAuth 2.1 Token endpoint
- `GET/POST /connect/userinfo` - OpenID Connect UserInfo endpoint

## Architecture Benefits

1. **Configurable Paths**: Change endpoint paths without code changes
2. **Extensible**: Add new endpoints by implementing `IOAuthEndpointHandler`
3. **Decoupled**: Authentication logic separated from entity implementations
4. **Testable**: Each component can be tested independently
5. **Standards Compliant**: Supports OAuth 2.1 and OpenID Connect standards

## Extending the Architecture

### Adding New Endpoints

1. Implement `IOAuthEndpointHandler`:
```csharp
public class CustomEndpointHandler : IOAuthEndpointHandler
{
    public async Task HandleAsync(object context)
    {
        var httpContext = (HttpContext)context;
        // Your endpoint logic here
    }
}
```

2. Register the handler:
```csharp
builder.Services.AddScoped<CustomEndpointHandler>();
```

3. Add to configuration:
```csharp
config.AddEndpoint("custom", new OAuthEndpoint
{
    Path = "/custom",
    HttpMethods = ["GET"],
    Handler = null!, // Resolved from DI
    Description = "Custom endpoint"
});
```

## Security Considerations

- Always validate tokens and client credentials
- Use HTTPS in production
- Implement proper error handling
- Consider rate limiting for endpoints
- Validate all input parameters

## Next Steps

- Implement additional OAuth endpoints (introspect, revoke, etc.)
- Add OpenID Connect discovery endpoint
- Implement proper token storage and management
- Add comprehensive logging and monitoring</content>
<parameter name="filePath">/home/orojasga/Sources/OroIdentityServers/examples/OroIdentityServerConfigurableExample/README.md