namespace GymBook.Api.Billing;

/// <summary>
/// Every few hours, checks running subscriptions with their stores again (see <see cref="Subscriptions.RecheckDueAsync"/>):
/// refunds and revocations show up without waiting for the expiry, Microsoft Store ones included, and anything a
/// store notification missed is caught.
/// </summary>
public class SubscriptionRefresher(IServiceScopeFactory scopes, ILogger<SubscriptionRefresher> log) : BackgroundService
{
    static readonly TimeSpan Interval = TimeSpan.FromHours(3);
    const int Batch = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Not right at startup, while migrations and the first requests run.
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var subscriptions = scope.ServiceProvider.GetRequiredService<Subscriptions>();
                // Batches until the due ones are done, so a backlog clears in one run.
                while (await subscriptions.RecheckDueAsync(Batch, stoppingToken) == Batch)
                {
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Checking subscriptions with the stores failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
