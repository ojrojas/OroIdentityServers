

namespace OroIdentityServers.EntityFramework.Events;

/// <summary>
/// Advanced event-driven architecture for microservices
/// </summary>
/// 
/// <summary>
/// Represents an event in the system
/// </summary>
public interface IEvent
{
    string EventType { get; }
    DateTime Timestamp { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    string? UserId { get; }
    object? Data { get; }
}

/// <summary>
/// Base class for all events
/// </summary>
public abstract class EventBase : IEvent
{
    public string EventType => GetType().Name;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? UserId { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Event bus for publishing and subscribing to events
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
    Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
}

/// <summary>
/// Message broker integration for external services
/// </summary>
public interface IMessageBroker
{
    Task PublishAsync(string topic, IEvent @event);
    Task SubscribeAsync(string topic, Func<IEvent, Task> handler);
    Task UnsubscribeAsync(string topic);
}

/// <summary>
/// Event store for event sourcing
/// </summary>
public interface IEventStore
{
    Task SaveEventAsync(IEvent @event);
    Task<IEnumerable<IEvent>> GetEventsAsync(string aggregateId, DateTime? fromDate = null);
    Task<IEnumerable<IEvent>> GetEventsByTypeAsync(string eventType, DateTime? fromDate = null);
}

/// <summary>
/// Event publisher for external integrations
/// </summary>
public interface IEventPublisher
{
    Task PublishToExternalServicesAsync(IEvent @event);
    Task PublishToMessageBrokerAsync(IEvent @event);
    Task PublishToWebhooksAsync(IEvent @event);
}

/// <summary>
/// Event subscriber for external integrations
/// </summary>
public interface IEventSubscriber
{
    Task SubscribeToExternalEventsAsync();
    Task ProcessExternalEventAsync(IEvent @event);
}