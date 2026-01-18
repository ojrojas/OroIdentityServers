using System.Threading.Tasks;

namespace OroIdentityServers.Core;

public interface IUserStore
{
    Task<User?> FindUserByUsernameAsync(string username);
    Task<User?> FindUserByIdAsync(string id);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}

public class User
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; } // In production, use secure hash
    public List<string> Claims { get; set; } = new();
}