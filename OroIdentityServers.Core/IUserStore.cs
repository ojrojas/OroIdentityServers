using System.Security.Claims;
using System.Threading.Tasks;

namespace OroIdentityServers.Core;

public interface IUser
{
    string Id { get; }
    string Username { get; }
    IEnumerable<Claim> Claims { get; }
    bool ValidatePassword(string password);
}

public interface IUserStore
{
    Task<IUser?> FindUserByUsernameAsync(string username);
    Task<IUser?> FindUserByIdAsync(string id);
}

public class User : IUser
{
    public required string Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; } // In production, use secure hash
    public List<Claim> Claims { get; set; } = new();

    IEnumerable<Claim> IUser.Claims => Claims;

    public bool ValidatePassword(string password) => PasswordHash == password; // Simplified
}