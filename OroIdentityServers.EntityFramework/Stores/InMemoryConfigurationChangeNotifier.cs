namespace OroIdentityServers.EntityFramework.Stores;

public class InMemoryConfigurationChangeNotifier : IConfigurationChangeNotifier
{
    private readonly ConcurrentDictionary<string, List<ConfigurationChangedEvent>> _events = new();
    private readonly ConcurrentDictionary<string, ClientFlowsChangedEvent> _clientFlowsEvents = new();

    public async Task NotifyConfigurationChangedAsync(ConfigurationChangedEvent @event)
    {
        // Store the event
        var events = _events.GetOrAdd(@event.EntityType, _ => []);
        events.Add(@event);

        // Keep only recent events (last 1000)
        if (events.Count > 1000)
        {
            events.RemoveRange(0, events.Count - 1000);
        }

        // If it's a client configuration change, update flows event
        if (@event is ClientConfigurationChangedEvent clientEvent)
        {
            await UpdateClientFlowsEventAsync(clientEvent);
        }

        // TODO: Publish to external event bus, message queue, etc.
        // For example: await _eventBus.PublishAsync(@event);
    }

    public async Task<ClientFlowsChangedEvent?> GetClientFlowsChangedEventAsync(string clientId)
    {
        if (_clientFlowsEvents.TryGetValue(clientId, out var flowsEvent))
        {
            return flowsEvent;
        }

        return null;
    }

    public async Task<IEnumerable<ConfigurationChangedEvent>> GetRecentChangesAsync(DateTime since)
    {
        var allEvents = _events.Values.SelectMany(events => events)
            .Where(e => e.ChangeTime >= since)
            .OrderByDescending(e => e.ChangeTime);

        return allEvents;
    }

    private async Task UpdateClientFlowsEventAsync(ClientConfigurationChangedEvent clientEvent)
    {
        // This is a simplified implementation
        // In a real scenario, you'd analyze the old/new values to determine what flows changed

        var flowsEvent = new ClientFlowsChangedEvent
        {
            EntityId = clientEvent.EntityId,
            ChangeTime = clientEvent.ChangeTime,
            ChangedBy = clientEvent.ChangedBy,
            ChangeDescription = "Client flows configuration changed",
            OldValues = clientEvent.OldValues,
            NewValues = clientEvent.NewValues
        };

        _clientFlowsEvents[clientEvent.EntityId] = flowsEvent;
    }

    // Method to clear old events (should be called periodically)
    public void CleanupOldEvents(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;

        foreach (var events in _events.Values)
        {
            events.RemoveAll(e => e.ChangeTime < cutoff);
        }

        foreach (var key in _clientFlowsEvents.Keys.ToList())
        {
            if (_clientFlowsEvents[key].ChangeTime < cutoff)
            {
                _clientFlowsEvents.TryRemove(key, out _);
            }
        }
    }
}