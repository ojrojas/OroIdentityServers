# OroIdentityServer PostgreSQL Example

This example demonstrates how to use OroIdentityServers with PostgreSQL database.

## Features Demonstrated

- OAuth 2.0 and OpenID Connect 1.0 flows
- Entity Framework integration with PostgreSQL
- Encrypted client secrets (AES)
- Event-driven architecture with in-memory event bus and event store
- Configuration change events
- Automatic migrations and token cleanup

## Prerequisites

- PostgreSQL server running
- .NET 10.0 SDK

## Configuration

Update the connection string in `appsettings.json` or set the environment variable.

## Running

```bash
dotnet run
```

The server will start on http://localhost:5160

## Features

- OAuth 2.0 and OpenID Connect flows
- Entity Framework with PostgreSQL
- Encrypted client secrets
- Event-driven architecture
- Automatic migrations