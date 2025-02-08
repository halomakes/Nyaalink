using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nyaalink.Configuration;
using QBittorrent.Client;

namespace Nyaalink.Services;

internal class QbitService(QBittorrentClient client, DownloadContext db, IOptions<QbitConfiguration> options)
{
    public async Task Download(DownloadRecord item, CancellationToken cancellationToken = default)
    {
        var record = await db.Records
            .Include(static r => r.Rule)
            .FirstOrDefaultAsync(r => r.Id == item.Id, cancellationToken);
        if (record?.DownloadedAt != null)
            return;

        await client.LoginAsync(options.Value.Username, options.Value.Password, cancellationToken);
        await client.AddTorrentsAsync(new AddTorrentUrlsRequest(record!.MagnetLink)
        {
            Category = "Anime",
            DownloadFolder = $"/data/Anime/{record!.Rule!.Label} - {record.Rule.AniDbId}"
        }, cancellationToken);
        
        record.DownloadedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}