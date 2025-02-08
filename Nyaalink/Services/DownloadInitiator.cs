using System.Threading.Channels;

namespace Nyaalink.Services;

public class DownloadInitiator(IServiceProvider serviceProvider, Channel<DownloadRecord> channel) : BackgroundService
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
                //todo: something idk
            }
        }
    }
}