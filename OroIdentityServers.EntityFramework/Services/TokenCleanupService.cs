using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OroIdentityServers.EntityFramework.Services;

public class TokenCleanupService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private Timer? _timer;

    public TokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token cleanup service starting.");

        // Run cleanup every hour
        _timer = new Timer(CleanupExpiredTokens, null, TimeSpan.Zero, TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private async void CleanupExpiredTokens(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IOroIdentityServerDbContext>();

            var expiredTokens = await dbContext.PersistedGrants
                .Where(g => g.Expiration < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                dbContext.PersistedGrants.RemoveRange(expiredTokens);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired tokens.", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up expired tokens.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token cleanup service stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}