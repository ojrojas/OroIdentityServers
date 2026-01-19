using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OroIdentityServers.Core;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;
using OroIdentityServers.EntityFramework.Events;
using OroIdentityServers.EntityFramework.Services;
using OroIdentityServers.EntityFramework.MultiTenancy;

namespace OroIdentityServers.EntityFramework.Stores;

public class EntityFrameworkClientStore : IClientStore
{
    private readonly IOroIdentityServerDbContext _context;
    private readonly IDistributedCache? _cache;
    private readonly IConfigurationChangeNotifier? _eventNotifier;
    private readonly IEncryptionService? _encryptionService;
    private readonly IEventPublisher? _eventPublisher;
    private readonly ITenantResolver? _tenantResolver;
    private readonly ConcurrentDictionary<string, Client> _clientCache = new();

    public EntityFrameworkClientStore(
        IOroIdentityServerDbContext context,
        IDistributedCache? cache = null,
        IConfigurationChangeNotifier? eventNotifier = null,
        IEncryptionService? encryptionService = null,
        IEventPublisher? eventPublisher = null,
        ITenantResolver? tenantResolver = null)
    {
        _context = context;
        _cache = cache;
        _eventNotifier = eventNotifier;
        _encryptionService = encryptionService;
        _eventPublisher = eventPublisher;
        _tenantResolver = tenantResolver;
    }

    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        // Try cache first
        if (_clientCache.TryGetValue(clientId, out var cachedClient))
        {
            return cachedClient;
        }

        // Try distributed cache
        if (_cache != null)
        {
            var cachedData = await _cache.GetStringAsync($"client:{clientId}");
            if (!string.IsNullOrEmpty(cachedData))
            {
                cachedClient = System.Text.Json.JsonSerializer.Deserialize<Client>(cachedData);
                if (cachedClient != null)
                {
                    _clientCache.TryAdd(clientId, cachedClient);
                    return cachedClient;
                }
            }
        }

        // Load from database
        var tenantId = await GetCurrentTenantIdAsync();
        var clientEntity = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.Enabled &&
                                    (tenantId == null || c.TenantId == tenantId));

        if (clientEntity == null)
        {
            return null;
        }

        var client = MapToClient(clientEntity);

        // Cache the result
        _clientCache.TryAdd(clientId, client);
        if (_cache != null)
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(client);
            await _cache.SetStringAsync($"client:{clientId}", serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
        }

        return client;
    }

    private Client MapToClient(ClientEntity entity)
    {
        return new Client
        {
            ClientId = entity.ClientId,
            ClientSecret = _encryptionService?.Decrypt(entity.ClientSecret) ?? entity.ClientSecret,
            ClientName = entity.ClientName,
            AllowedGrantTypes = entity.AllowedGrantTypes.Select(gt => gt.GrantType).ToList(),
            RedirectUris = entity.RedirectUris.Select(ru => ru.RedirectUri).ToList(),
            AllowedScopes = entity.AllowedScopes.Select(s => s.Scope).ToList(),
            Claims = entity.Claims.Select(c => new System.Security.Claims.Claim(c.Type, c.Value)).ToList()
        };
    }

    // Methods for managing clients with event notifications
    public async Task CreateClientAsync(Client client, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        var entity = new ClientEntity
        {
            TenantId = tenantId,
            ClientId = client.ClientId,
            ClientSecret = _encryptionService?.Encrypt(client.ClientSecret) ?? client.ClientSecret,
            ClientName = client.ClientName,
            Description = client.ClientName,
            Enabled = true,
            Created = DateTime.UtcNow
        };

        // Add grant types
        foreach (var grantType in client.AllowedGrantTypes)
        {
            entity.AllowedGrantTypes.Add(new ClientGrantTypeEntity
            {
                GrantType = grantType
            });
        }

        // Add redirect URIs
        foreach (var uri in client.RedirectUris)
        {
            entity.RedirectUris.Add(new ClientRedirectUriEntity
            {
                RedirectUri = uri
            });
        }

        // Add scopes
        foreach (var scope in client.AllowedScopes)
        {
            entity.AllowedScopes.Add(new ClientScopeEntity
            {
                Scope = scope
            });
        }

        // Add claims
        foreach (var claim in client.Claims)
        {
            entity.Claims.Add(new ClientClaimEntity
            {
                Type = claim.Type,
                Value = claim.Value
            });
        }

        _context.Clients.Add(entity);
        await _context.SaveChangesAsync();

        // Log configuration change
        await LogConfigurationChangeAsync("Client", entity.Id.ToString(), "Create", null,
            System.Text.Json.JsonSerializer.Serialize(entity), changedBy);

        // Invalidate cache
        await InvalidateClientCacheAsync(client.ClientId);

        // Notify event
        if (_eventNotifier != null)
        {
            var @event = new ClientConfigurationChangedEvent
            {
                EntityId = client.ClientId,
                ChangeType = "Created",
                ChangedBy = changedBy,
                ChangeDescription = $"Client '{client.ClientId}' was created",
                NewValues = client
            };
            await _eventNotifier.NotifyConfigurationChangedAsync(@event);
        }

        // Publish domain event
        if (_eventPublisher != null)
        {
            var domainEvent = new ClientCreatedEvent
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                UserId = changedBy,
                Timestamp = DateTime.UtcNow
            };
            await _eventPublisher.PublishToExternalServicesAsync(domainEvent);
        }
    }

    public async Task UpdateClientAsync(Client client, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        var entity = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.ClientId == client.ClientId && c.TenantId == tenantId);

        if (entity == null)
        {
            throw new InvalidOperationException($"Client '{client.ClientId}' not found");
        }

        var oldClient = MapToClient(entity);

        // Update basic properties
        entity.ClientSecret = client.ClientSecret;
        entity.ClientName = client.ClientName;
        entity.Description = client.ClientName;
        entity.LastModified = DateTime.UtcNow;

        // Update grant types
        _context.ClientGrantTypes.RemoveRange(entity.AllowedGrantTypes);
        foreach (var grantType in client.AllowedGrantTypes)
        {
            entity.AllowedGrantTypes.Add(new ClientGrantTypeEntity
            {
                GrantType = grantType
            });
        }

        // Update redirect URIs
        _context.ClientRedirectUris.RemoveRange(entity.RedirectUris);
        foreach (var uri in client.RedirectUris)
        {
            entity.RedirectUris.Add(new ClientRedirectUriEntity
            {
                RedirectUri = uri
            });
        }

        // Update scopes
        _context.ClientScopes.RemoveRange(entity.AllowedScopes);
        foreach (var scope in client.AllowedScopes)
        {
            entity.AllowedScopes.Add(new ClientScopeEntity
            {
                Scope = scope
            });
        }

        // Update claims
        _context.ClientClaims.RemoveRange(entity.Claims);
        foreach (var claim in client.Claims)
        {
            entity.Claims.Add(new ClientClaimEntity
            {
                Type = claim.Type,
                Value = claim.Value
            });
        }

        await _context.SaveChangesAsync();

        // Log configuration change
        await LogConfigurationChangeAsync("Client", entity.Id.ToString(), "Update",
            System.Text.Json.JsonSerializer.Serialize(oldClient),
            System.Text.Json.JsonSerializer.Serialize(client), changedBy);

        // Invalidate cache
        await InvalidateClientCacheAsync(client.ClientId);

        // Notify event
        if (_eventNotifier != null)
        {
            var @event = new ClientConfigurationChangedEvent
            {
                EntityId = client.ClientId,
                ChangeType = "Updated",
                ChangedBy = changedBy,
                ChangeDescription = $"Client '{client.ClientId}' was updated",
                OldValues = oldClient,
                NewValues = client
            };
            await _eventNotifier.NotifyConfigurationChangedAsync(@event);
        }

        // Publish domain event
        if (_eventPublisher != null)
        {
            var domainEvent = new ClientUpdatedEvent
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                UserId = changedBy,
                Timestamp = DateTime.UtcNow
            };
            await _eventPublisher.PublishToExternalServicesAsync(domainEvent);
        }
    }

    public async Task DeleteClientAsync(string clientId, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        var entity = await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.TenantId == tenantId);

        if (entity != null)
        {
            var oldClient = MapToClient(entity);

            _context.Clients.Remove(entity);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateClientCacheAsync(clientId);

            // Notify event
            if (_eventNotifier != null)
            {
                var @event = new ClientConfigurationChangedEvent
                {
                    EntityId = clientId,
                    ChangeType = "Deleted",
                    ChangedBy = changedBy,
                    ChangeDescription = $"Client '{clientId}' was deleted",
                    OldValues = oldClient
                };
                await _eventNotifier.NotifyConfigurationChangedAsync(@event);
            }

            // Publish domain event
            if (_eventPublisher != null)
            {
                var domainEvent = new ClientDeletedEvent
                {
                    ClientId = clientId,
                    ClientName = oldClient?.ClientName,
                    UserId = changedBy,
                    Timestamp = DateTime.UtcNow
                };
                await _eventPublisher.PublishToExternalServicesAsync(domainEvent);
            }
        }
    }

    private async Task InvalidateClientCacheAsync(string clientId)
    {
        _clientCache.TryRemove(clientId, out _);
        if (_cache != null)
        {
            await _cache.RemoveAsync($"client:{clientId}");
        }
    }

    private async Task LogConfigurationChangeAsync(string entityType, string entityId,
        string changeType, string? oldValues, string? newValues, string? changedBy)
    {
        var logEntry = new ConfigurationChangeLogEntity
        {
            EntityType = entityType,
            EntityId = entityId,
            ChangeType = changeType,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedBy = changedBy,
            ChangeTime = DateTime.UtcNow,
            ChangeDescription = $"{changeType} operation on {entityType} '{entityId}'"
        };

        _context.ConfigurationChangeLogs.Add(logEntry);
        await _context.SaveChangesAsync();
    }

    private async Task<string?> GetCurrentTenantIdAsync()
    {
        return _tenantResolver != null ? await _tenantResolver.GetCurrentTenantIdAsync() : null;
    }
}