# OroIdentityServer MySQL Example

This example demonstrates how to use OroIdentityServers with MySQL database.

## Prerequisites

- MySQL server running
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
- Entity Framework with MySQL
- Encrypted client secrets
- Event-driven architecture
- Automatic migrations