
namespace OroIdentityServers.EntityFramework.Extensions;

/// <summary>
/// Extension methods for configuring event-driven architecture services
/// </summary>
public static class EventServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory event bus to the service collection
    /// </summary>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }

    /// <summary>
    /// Adds RabbitMQ message broker to the service collection
    /// </summary>
    public static IServiceCollection AddRabbitMqMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
        return services;
    }

    /// <summary>
    /// Adds Azure Service Bus message broker to the service collection
    /// </summary>
    public static IServiceCollection AddAzureServiceBusMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureServiceBusOptions>(configuration.GetSection("AzureServiceBus"));
        services.AddSingleton<IMessageBroker, AzureServiceBusMessageBroker>();
        return services;
    }

    /// <summary>
    /// Adds event store using Entity Framework to the service collection
    /// </summary>
    public static IServiceCollection AddEventStore(this IServiceCollection services)
    {
        services.AddScoped<IEventStore, EntityFrameworkEventStore>();
        return services;
    }

    /// <summary>
    /// Adds event publisher with external integrations to the service collection
    /// </summary>
    public static IServiceCollection AddEventPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();

        // Configure webhook URLs from configuration
        var webhookUrls = configuration.GetSection("EventPublisher:WebhookUrls").Get<string[]>() ?? Array.Empty<string>();
        services.AddSingleton<IEnumerable<string>>(webhookUrls);

        return services;
    }

    /// <summary>
    /// Adds event subscriber to the service collection
    /// </summary>
    public static IServiceCollection AddEventSubscriber(this IServiceCollection services)
    {
        services.AddScoped<IEventSubscriber, EventSubscriber>();
        return services;
    }

    /// <summary>
    /// Adds complete event-driven architecture with all components
    /// </summary>
    public static IServiceCollection AddEventDrivenArchitecture(
        this IServiceCollection services,
        IConfiguration configuration,
        EventArchitectureOptions? options = null)
    {
        options ??= new EventArchitectureOptions();

        // Add core event bus
        if (options.UseInMemoryEventBus)
        {
            services.AddInMemoryEventBus();
        }

        // Add message broker based on configuration
        var messageBrokerType = configuration.GetValue<string>("EventArchitecture:MessageBroker");
        switch (messageBrokerType?.ToLower())
        {
            case "rabbitmq":
                services.AddRabbitMqMessageBroker(configuration);
                break;
            case "azureservicebus":
                services.AddAzureServiceBusMessageBroker(configuration);
                break;
            default:
                // Use in-memory if no external broker configured
                services.AddInMemoryEventBus();
                break;
        }

        // Add event store
        if (options.UseEventStore)
        {
            services.AddEventStore();
        }

        // Add publisher and subscriber
        services.AddEventPublisher(configuration);
        services.AddEventSubscriber();

        return services;
    }
}

/// <summary>
/// Options for configuring event-driven architecture
/// </summary>
public class EventArchitectureOptions
{
    /// <summary>
    /// Whether to use in-memory event bus
    /// </summary>
    public bool UseInMemoryEventBus { get; set; } = true;

    /// <summary>
    /// Whether to use event store for event sourcing
    /// </summary>
    public bool UseEventStore { get; set; } = true;

    /// <summary>
    /// Whether to enable external integrations (webhooks, message brokers)
    /// </summary>
    public bool EnableExternalIntegrations { get; set; } = false;
}

/// <summary>
/// Configuration options for RabbitMQ
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ host
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}

/// <summary>
/// Configuration options for Azure Service Bus
/// </summary>
public class AzureServiceBusOptions
{
    /// <summary>
    /// Azure Service Bus connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Azure Service Bus topic name
    /// </summary>
    public string TopicName { get; set; } = "identity-events";
}