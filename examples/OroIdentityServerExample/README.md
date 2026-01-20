# OroIdentityServer Example

This example demonstrates the core functionality of OroIdentityServers with SQLite database.

## Features Demonstrated

- OAuth 2.0 and OpenID Connect 1.0 flows
- Entity Framework integration with automatic migrations
- Encrypted client secrets (AES)
- Event-driven architecture with in-memory event bus and event store
- Configuration change events
- Automatic token cleanup
- User interface for testing authentication flows

## Prerequisites

- .NET 10.0 SDK

## Running

```bash
dotnet run
```

The server will start on http://localhost:5160

## Optional: Message Broker Integration

To enable RabbitMQ integration for external services:

1. Install and start RabbitMQ
2. Uncomment the RabbitMQ configuration in Program.cs
3. Update connection settings in appsettings.json

## Testing

- Open http://localhost:5160 in your browser
- Use the web interface to test login and token flows
- API endpoints are available at /connect/*