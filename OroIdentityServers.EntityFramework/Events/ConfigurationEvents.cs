using System.Threading.Tasks;

namespace OroIdentityServers.EntityFramework.Events;

public interface IConfigurationChangeNotifier
{
    Task NotifyConfigurationChangedAsync(ConfigurationChangedEvent @event);
    Task<ClientFlowsChangedEvent?> GetClientFlowsChangedEventAsync(string clientId);
    Task<IEnumerable<ConfigurationChangedEvent>> GetRecentChangesAsync(DateTime since);
}

public abstract class ConfigurationChangedEvent
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // "Created", "Updated", "Deleted"
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
    public string? ChangedBy { get; set; }
    public string? ChangeDescription { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}

public class ClientFlowsChangedEvent : ConfigurationChangedEvent
{
    public ClientFlowsChangedEvent()
    {
        EntityType = "Client";
        ChangeType = "FlowsChanged";
    }

    public string ClientId => EntityId;
    public IEnumerable<string>? AddedGrantTypes { get; set; }
    public IEnumerable<string>? RemovedGrantTypes { get; set; }
    public IEnumerable<string>? AddedScopes { get; set; }
    public IEnumerable<string>? RemovedScopes { get; set; }
}

public class ClientConfigurationChangedEvent : ConfigurationChangedEvent
{
    public ClientConfigurationChangedEvent()
    {
        EntityType = "Client";
    }

    public string ClientId => EntityId;
}

public class UserConfigurationChangedEvent : ConfigurationChangedEvent
{
    public UserConfigurationChangedEvent()
    {
        EntityType = "User";
    }

    public string UserId => EntityId;
}

public class ApiResourceConfigurationChangedEvent : ConfigurationChangedEvent
{
    public ApiResourceConfigurationChangedEvent()
    {
        EntityType = "ApiResource";
    }

    public string ApiResourceId => EntityId;
}

public class IdentityResourceConfigurationChangedEvent : ConfigurationChangedEvent
{
    public IdentityResourceConfigurationChangedEvent()
    {
        EntityType = "IdentityResource";
    }

    public string IdentityResourceId => EntityId;
}