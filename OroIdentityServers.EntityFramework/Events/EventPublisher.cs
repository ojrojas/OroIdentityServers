

namespace OroIdentityServers.EntityFramework.Events;

/// <summary>
/// Event publisher for external integrations
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IEventBus _eventBus;
    private readonly IMessageBroker? _messageBroker;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IEnumerable<string> _webhookUrls;

    public EventPublisher(
        IEventBus eventBus,
        IMessageBroker? messageBroker = null,
        IHttpClientFactory? httpClientFactory = null,
        IEnumerable<string>? webhookUrls = null)
    {
        _eventBus = eventBus;
        _messageBroker = messageBroker;
        _httpClientFactory = httpClientFactory;
        _webhookUrls = webhookUrls ?? Array.Empty<string>();
    }

    public async Task PublishToExternalServicesAsync(IEvent @event)
    {
        // Publish to internal event bus
        await _eventBus.PublishAsync(@event);

        // Publish to message broker if configured
        if (_messageBroker != null)
        {
            await PublishToMessageBrokerAsync(@event);
        }

        // Publish to webhooks if configured
        if (_webhookUrls.Any())
        {
            await PublishToWebhooksAsync(@event);
        }
    }

    public async Task PublishToMessageBrokerAsync(IEvent @event)
    {
        if (_messageBroker == null)
            return;

        var topic = GetTopicForEvent(@event);
        await _messageBroker.PublishAsync(topic, @event);
    }

    public async Task PublishToWebhooksAsync(IEvent @event)
    {
        if (_httpClientFactory == null || !_webhookUrls.Any())
            return;

        var client = _httpClientFactory.CreateClient();
        var payload = JsonSerializer.Serialize(@event);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var tasks = _webhookUrls.Select(url =>
            client.PostAsync(url, content));

        await Task.WhenAll(tasks);
    }

    private static string GetTopicForEvent(IEvent @event)
    {
        return @event switch
        {
            ClientCreatedEvent or ClientUpdatedEvent or ClientDeletedEvent => "identity.clients",
            UserCreatedEvent or UserUpdatedEvent or UserDeletedEvent => "identity.users",
            AccessTokenIssuedEvent or RefreshTokenIssuedEvent or TokenRevokedEvent => "identity.tokens",
            UserLoginEvent or UserLogoutEvent => "identity.authentication",
            AuthorizationGrantedEvent or AuthorizationDeniedEvent => "identity.authorization",
            _ => "identity.events"
        };
    }
}

/// <summary>
/// Event subscriber for external integrations
/// </summary>
public class EventSubscriber : IEventSubscriber
{
    private readonly IEventBus _eventBus;
    private readonly IMessageBroker? _messageBroker;

    public EventSubscriber(IEventBus eventBus, IMessageBroker? messageBroker = null)
    {
        _eventBus = eventBus;
        _messageBroker = messageBroker;
    }

    public async Task SubscribeToExternalEventsAsync()
    {
        if (_messageBroker == null)
            return;

        // Subscribe to external topics
        await _messageBroker.SubscribeAsync("external.identity.clients", HandleExternalClientEventAsync);
        await _messageBroker.SubscribeAsync("external.identity.users", HandleExternalUserEventAsync);
        await _messageBroker.SubscribeAsync("external.identity.tokens", HandleExternalTokenEventAsync);
    }

    public async Task ProcessExternalEventAsync(IEvent @event)
    {
        // Process external events and potentially trigger internal actions
        // This could be used for cross-service synchronization

        switch (@event)
        {
            case ClientCreatedEvent clientEvent:
                // Handle external client creation
                break;
            case UserCreatedEvent userEvent:
                // Handle external user creation
                break;
            // Add more cases as needed
        }

        await Task.CompletedTask;
    }

    private async Task HandleExternalClientEventAsync(IEvent @event)
    {
        await ProcessExternalEventAsync(@event);
    }

    private async Task HandleExternalUserEventAsync(IEvent @event)
    {
        await ProcessExternalEventAsync(@event);
    }

    private async Task HandleExternalTokenEventAsync(IEvent @event)
    {
        await ProcessExternalEventAsync(@event);
    }
}