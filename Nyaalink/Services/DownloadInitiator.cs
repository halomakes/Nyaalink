using System.Threading.Channels;

namespace Nyaalink.Services;

public class DownloadInitiator(
    IServiceProvider serviceProvider,
    Channel<DownloadRecord> channel,
    ILogger<DownloadInitiator> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            var download = await channel.Reader.ReadAsync(stoppingToken);
            await using var scope = serviceProvider.CreateAsyncScope();
            var downloader = scope.ServiceProvider.GetRequiredService<QbitService>();
            try
            {
                await downloader.Download(download, stoppingToken);
            }
            catch (Exception e)
            {
                log.LogError(e, "Unable to initiate download");
            }
        }
    }
}