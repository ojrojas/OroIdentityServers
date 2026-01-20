namespace OroIdentityServers.Core;

public class InMemoryClientStore : IClientStore
{
    private readonly List<Client> _clients = [];

    public InMemoryClientStore(IEnumerable<Client> clients)
    {
        _clients.AddRange(clients);
    }

    public Task<Client?> FindClientByIdAsync(string clientId)
    {
        return Task.FromResult(_clients.FirstOrDefault(c => c.ClientId == clientId));
    }
}