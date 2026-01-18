using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OroIdentityServers.Core;

public class InMemoryUserStore : IUserStore
{
    private readonly List<User> _users = new();

    public InMemoryUserStore(IEnumerable<User> users)
    {
        _users.AddRange(users);
    }

    public Task<IUser?> FindUserByUsernameAsync(string username)
    {
        return Task.FromResult<IUser?>(_users.FirstOrDefault(u => u.Username == username));
    }

    public Task<IUser?> FindUserByIdAsync(string id)
    {
        return Task.FromResult<IUser?>(_users.FirstOrDefault(u => u.Id == id));
    }
}