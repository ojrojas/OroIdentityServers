using Microsoft.Extensions.Caching.Distributed;

namespace OroIdentityServers.EntityFramework.Stores;

public class EntityFrameworkUserStore : IUserStore
{
    private readonly IOroIdentityServerDbContext _context;
    private readonly IDistributedCache? _cache;
    private readonly IConfigurationChangeNotifier? _eventNotifier;
    private readonly ITenantResolver? _tenantResolver;
    private readonly ConcurrentDictionary<string, IUser> _userCache = new();

    public EntityFrameworkUserStore(
        IOroIdentityServerDbContext context,
        IDistributedCache? cache = null,
        IConfigurationChangeNotifier? eventNotifier = null,
        ITenantResolver? tenantResolver = null)
    {
        _context = context;
        _cache = cache;
        _eventNotifier = eventNotifier;
        _tenantResolver = tenantResolver;
    }

    public async Task<IUser?> FindUserByUsernameAsync(string username)
    {
        // Try cache first
        if (_userCache.TryGetValue(username, out var cachedUser))
        {
            return cachedUser;
        }

        // Try distributed cache
        if (_cache != null)
        {
            var cachedData = await _cache.GetStringAsync($"user:{username}");
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedUserEntity = System.Text.Json.JsonSerializer.Deserialize<UserEntity>(cachedData);
                if (cachedUserEntity != null)
                {
                    var iUser = MapToIUser(cachedUserEntity);
                    _userCache.TryAdd(username, iUser);
                    return iUser;
                }
            }
        }

        // Load from database
        var tenantId = await GetCurrentTenantIdAsync();
        var userEntity = await _context.Users
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Username == username && u.Enabled &&
                                    (tenantId == null || u.TenantId == tenantId));

        if (userEntity == null)
        {
            return null;
        }

        var user = MapToIUser(userEntity);

        // Cache the result
        _userCache.TryAdd(username, user);
        if (_cache != null)
        {
            var serialized = System.Text.Json.JsonSerializer.Serialize(userEntity);
            await _cache.SetStringAsync($"user:{username}", serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });
        }

        return user;
    }

    public async Task<IUser?> FindUserByIdAsync(string id)
    {
        // Load from database
        var tenantId = await GetCurrentTenantIdAsync();
        var userEntity = await _context.Users
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Id.ToString() == id && u.Enabled &&
                                    (tenantId == null || u.TenantId == tenantId));

        return userEntity != null ? MapToIUser(userEntity) : null;
    }

    private IUser MapToIUser(UserEntity entity)
    {
        return new User
        {
            Id = entity.Id.ToString(),
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            Claims = entity.Claims.Select(c => new System.Security.Claims.Claim(c.Type, c.Value)).ToList()
        };
    }

    // Methods for managing users with event notifications
    public async Task CreateUserAsync(UserEntity user, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        user.TenantId = tenantId;
        user.Created = DateTime.UtcNow;
        user.Enabled = true;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Invalidate cache
        await InvalidateUserCacheAsync(user.Username);

        // Notify event
        if (_eventNotifier != null)
        {
            var @event = new UserConfigurationChangedEvent
            {
                EntityId = user.Id.ToString(),
                ChangeType = "Created",
                ChangedBy = changedBy,
                ChangeDescription = $"User '{user.Username}' was created",
                NewValues = user
            };
            await _eventNotifier.NotifyConfigurationChangedAsync(@event);
        }
    }

    public async Task UpdateUserAsync(UserEntity user, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        var existingUser = await _context.Users
            .Include(u => u.Claims)
            .FirstOrDefaultAsync(u => u.Id == user.Id && u.TenantId == tenantId);

        if (existingUser == null)
        {
            throw new InvalidOperationException($"User with ID '{user.Id}' not found");
        }

        var oldUser = MapToIUser(existingUser);

        // Update properties
        existingUser.Username = user.Username;
        existingUser.Email = user.Email;
        existingUser.EmailConfirmed = user.EmailConfirmed;
        existingUser.Enabled = user.Enabled;
        existingUser.LastModified = DateTime.UtcNow;

        // Update claims
        _context.UserClaims.RemoveRange(existingUser.Claims);
        foreach (var claim in user.Claims)
        {
            existingUser.Claims.Add(new UserClaimEntity
            {
                Type = claim.Type,
                Value = claim.Value
            });
        }

        await _context.SaveChangesAsync();

        // Invalidate cache
        await InvalidateUserCacheAsync(user.Username);

        // Notify event
        if (_eventNotifier != null)
        {
            var @event = new UserConfigurationChangedEvent
            {
                EntityId = user.Id.ToString(),
                ChangeType = "Updated",
                ChangedBy = changedBy,
                ChangeDescription = $"User '{user.Username}' was updated",
                OldValues = oldUser,
                NewValues = MapToIUser(user)
            };
            await _eventNotifier.NotifyConfigurationChangedAsync(@event);
        }
    }

    public async Task DeleteUserAsync(int userId, string? changedBy = null)
    {
        var tenantId = await GetCurrentTenantIdAsync() ?? throw new InvalidOperationException("Tenant context is required");
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user != null)
        {
            var oldUser = MapToIUser(user);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateUserCacheAsync(user.Username);

            // Notify event
            if (_eventNotifier != null)
            {
                var @event = new UserConfigurationChangedEvent
                {
                    EntityId = userId.ToString(),
                    ChangeType = "Deleted",
                    ChangedBy = changedBy,
                    ChangeDescription = $"User '{user.Username}' was deleted",
                    OldValues = oldUser
                };
                await _eventNotifier.NotifyConfigurationChangedAsync(@event);
            }
        }
    }

    private async Task InvalidateUserCacheAsync(string username)
    {
        _userCache.TryRemove(username, out _);
        if (_cache != null)
        {
            await _cache.RemoveAsync($"user:{username}");
        }
    }

    private async Task<string?> GetCurrentTenantIdAsync()
    {
        return _tenantResolver != null ? await _tenantResolver.GetCurrentTenantIdAsync() : null;
    }
}