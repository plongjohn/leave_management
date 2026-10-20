namespace LeaveManagement.Api.Auth;

/// <summary>Deletes expired refresh tokens periodically, so the table does not grow forever.</summary>
internal sealed partial class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var refreshTokens = scope.ServiceProvider.GetRequiredService<RefreshTokenService>();
                var deleted = await refreshTokens.DeleteExpiredAsync(stoppingToken);
                LogDeleted(deleted);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed run (e.g. database briefly unavailable) is retried with the next tick.
                LogFailed(exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} expired refresh tokens")]
    private partial void LogDeleted(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Deleting expired refresh tokens failed")]
    private partial void LogFailed(Exception exception);
}
