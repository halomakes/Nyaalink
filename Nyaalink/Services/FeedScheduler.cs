namespace Nyaalink.Services;

internal class FeedScheduler(IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = services.CreateAsyncScope();
        var consumer = scope.ServiceProvider.GetRequiredService<FeedConsumer>();

        await consumer.IngestAsync(stoppingToken);

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }
}