using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace OroIdentityServers.EntityFramework.Events;

/// <summary>
/// In-memory event bus implementation
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);

        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers
                .OfType<Func<TEvent, Task>>()
                .Select(handler => handler(@event));

            await Task.WhenAll(tasks);
        }
    }

    public Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        var handlerList = _handlers.GetOrAdd(eventType, _ => new List<Delegate>());
        handlerList.Add(handler);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);

        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// RabbitMQ message broker implementation
/// </summary>
public class RabbitMqMessageBroker : IMessageBroker
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<string, List<Func<IEvent, Task>>> _subscriptions = new();

    public RabbitMqMessageBroker(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task PublishAsync(string topic, IEvent @event)
    {
        // TODO: Implement RabbitMQ publishing
        // This would use RabbitMQ.Client to publish messages
        await Task.CompletedTask;
    }

    public async Task SubscribeAsync(string topic, Func<IEvent, Task> handler)
    {
        var handlers = _subscriptions.GetOrAdd(topic, _ => new List<Func<IEvent, Task>>());
        handlers.Add(handler);

        // TODO: Implement RabbitMQ subscription
        // This would set up RabbitMQ consumer
        await Task.CompletedTask;
    }

    public async Task UnsubscribeAsync(string topic)
    {
        _subscriptions.TryRemove(topic, out _);

        // TODO: Implement RabbitMQ unsubscription
        await Task.CompletedTask;
    }
}

/// <summary>
/// Azure Service Bus message broker implementation
/// </summary>
public class AzureServiceBusMessageBroker : IMessageBroker
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<string, List<Func<IEvent, Task>>> _subscriptions = new();

    public AzureServiceBusMessageBroker(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task PublishAsync(string topic, IEvent @event)
    {
        // TODO: Implement Azure Service Bus publishing
        // This would use Azure.Messaging.ServiceBus to publish messages
        await Task.CompletedTask;
    }

    public async Task SubscribeAsync(string topic, Func<IEvent, Task> handler)
    {
        var handlers = _subscriptions.GetOrAdd(topic, _ => new List<Func<IEvent, Task>>());
        handlers.Add(handler);

        // TODO: Implement Azure Service Bus subscription
        await Task.CompletedTask;
    }

    public async Task UnsubscribeAsync(string topic)
    {
        _subscriptions.TryRemove(topic, out _);

        // TODO: Implement Azure Service Bus unsubscription
        await Task.CompletedTask;
    }
}