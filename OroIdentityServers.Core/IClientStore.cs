namespace OroIdentityServers.Core;

public interface IClientStore
{
    Task<Client?> FindClientByIdAsync(string clientId);
}