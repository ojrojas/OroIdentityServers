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

    public Task<User?> FindUserByUsernameAsync(string username)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Username == username));
    }

    public Task<User?> FindUserByIdAsync(string id)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    public Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username == username);
        return Task.FromResult(user != null && user.PasswordHash == password); // Simplified
    }
}