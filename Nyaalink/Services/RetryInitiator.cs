using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Services;

public class RetryInitiator(IServiceProvider serviceProvider, ILogger<RetryInitiator> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var downloader = scope.ServiceProvider.GetRequiredService<QbitService>();
            var db = scope.ServiceProvider.GetRequiredService<DownloadContext>();
            try
            {
                var queue = await db.Records
                    .AsNoTracking()
                    .Where(static r => r.DownloadedAt == null)
                    .ToListAsync(cancellationToken: stoppingToken);
                foreach (var download in queue)
                    await downloader.Download(download, stoppingToken);
            }
            catch (Exception e)
            {
                log.LogError(e, "Unable to retry downloads");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}