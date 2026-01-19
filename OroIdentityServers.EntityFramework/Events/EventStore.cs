using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OroIdentityServers.EntityFramework.DbContexts;
using OroIdentityServers.EntityFramework.Entities;

namespace OroIdentityServers.EntityFramework.Events;

/// <summary>
/// Event store implementation using Entity Framework
/// </summary>
public class EntityFrameworkEventStore : IEventStore
{
    private readonly IOroIdentityServerDbContext _context;

    public EntityFrameworkEventStore(IOroIdentityServerDbContext context)
    {
        _context = context;
    }

    public async Task SaveEventAsync(IEvent @event)
    {
        var eventEntity = new EventEntity
        {
            AggregateId = GetAggregateId(@event),
            AggregateType = GetAggregateType(@event),
            EventType = @event.EventType,
            Version = await GetNextVersionAsync(GetAggregateId(@event)),
            Timestamp = @event.Timestamp,
            CorrelationId = @event.CorrelationId,
            CausationId = @event.CausationId,
            UserId = @event.UserId,
            Data = JsonSerializer.Serialize(@event),
            Metadata = null,
            IsProcessed = false
        };

        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId, DateTime? fromDate = null)
    {
        var query = _context.Events
            .Where(e => e.AggregateId == aggregateId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        var eventEntities = await query
            .OrderBy(e => e.Version)
            .ToListAsync();

        return eventEntities.Select(DeserializeEvent);
    }

    public async Task<IEnumerable<IEvent>> GetEventsByTypeAsync(string eventType, DateTime? fromDate = null)
    {
        var query = _context.Events
            .Where(e => e.EventType == eventType);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        var eventEntities = await query
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return eventEntities.Select(DeserializeEvent);
    }

    private static string GetAggregateId(IEvent @event)
    {
        // Extract aggregate ID from event data
        // This is a simplified implementation
        return @event switch
        {
            ClientCreatedEvent e => e.ClientId,
            ClientUpdatedEvent e => e.ClientId,
            ClientDeletedEvent e => e.ClientId,
            UserCreatedEvent e => e.UserId,
            UserUpdatedEvent e => e.UserId,
            UserDeletedEvent e => e.UserId,
            _ => Guid.NewGuid().ToString()
        };
    }

    private static string GetAggregateType(IEvent @event)
    {
        return @event switch
        {
            ClientCreatedEvent or ClientUpdatedEvent or ClientDeletedEvent => "Client",
            UserCreatedEvent or UserUpdatedEvent or UserDeletedEvent => "User",
            _ => "Unknown"
        };
    }

    private async Task<long> GetNextVersionAsync(string aggregateId)
    {
        var lastVersion = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .Select(e => e.Version)
            .FirstOrDefaultAsync();

        return lastVersion + 1;
    }

    private static IEvent DeserializeEvent(EventEntity entity)
    {
        return entity.EventType switch
        {
            nameof(ClientCreatedEvent) => JsonSerializer.Deserialize<ClientCreatedEvent>(entity.Data)!,
            nameof(ClientUpdatedEvent) => JsonSerializer.Deserialize<ClientUpdatedEvent>(entity.Data)!,
            nameof(ClientDeletedEvent) => JsonSerializer.Deserialize<ClientDeletedEvent>(entity.Data)!,
            nameof(UserCreatedEvent) => JsonSerializer.Deserialize<UserCreatedEvent>(entity.Data)!,
            _ => throw new NotSupportedException($"Event type {entity.EventType} is not supported")
        };
    }
}