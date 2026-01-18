using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OroIdentityServers.Core;

public class InMemoryClientStore : IClientStore
{
    private readonly List<Client> _clients = new();

    public InMemoryClientStore(IEnumerable<Client> clients)
    {
        _clients.AddRange(clients);
    }

    public Task<Client?> FindClientByIdAsync(string clientId)
    {
        return Task.FromResult(_clients.FirstOrDefault(c => c.ClientId == clientId));
    }
}