using System.Threading.Channels;
using System.Web;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace Nyaalink.Services;

internal class FeedConsumer
{
    private readonly DownloadContext _db;
    private readonly Channel<DownloadRecord> _eventChannel;
    private readonly ILogger<FeedConsumer> _log;
    private readonly HttpClient _httpClient;

    public FeedConsumer(DownloadContext db, IHttpClientFactory clientFactory, Channel<DownloadRecord> eventChannel, ILogger<FeedConsumer> log)
    {
        _db = db;
        _eventChannel = eventChannel;
        _log = log;
        _httpClient = clientFactory.CreateClient();
    }

    public async Task IngestAsync(CancellationToken cancellationToken = default)
    {
        var queries = await _db.Queries
            .Include(static q => q.Rules)
            .ToListAsync(cancellationToken);
        foreach (var query in queries)
            await IngestAsync(query, cancellationToken);
    }

    public async Task IngestAsync(DownloadQuery query, CancellationToken cancellationToken)
    {
        if (query.Rules?.Count is not > 0)
            return;
        var items = await FetchItemsAsync(query, cancellationToken);
        var downloads = items
            .Select(i => ApplyRules(i, query.Rules))
            .Where(i => i.IsT0)
            .Select(i => i.AsT0)
            .ToList();
        if (downloads is [])
            return;

        var ids = downloads.Select(static d => d.Id).ToList();
        var existingIds = _db.Records
            .Where(r => ids.Contains(r.Id))
            .Select(static r => r.Id)
            .ToList();
        var recordsToCreate = downloads
            .ExceptBy(existingIds, static d => d.Id)
            .ToList();
        _db.Records.AddRange(recordsToCreate);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var download in recordsToCreate)
        {
            _db.Entry(download).State = EntityState.Detached;
            await _eventChannel.Writer.WriteAsync(download, cancellationToken);
        }
    }

    private async Task<IList<Item>> FetchItemsAsync(DownloadQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = new Uri($"https://nyaa.si/?page=rss&q={HttpUtility.UrlEncode(query.Query)}&c=0_0&f=0&m");
            var responseStream = await _httpClient.GetStreamAsync(requestUrl, cancellationToken);
            var serializer = new XmlSerializer(typeof(Rss));
            var feed = (Rss?)serializer.Deserialize(responseStream);
            var results = feed?.FeedChannel?.Item;

            query.LastFetched = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return results ?? [];
        }
        catch (Exception e)
        {
            _log.LogError(e, "Unable to fetch feed for {Query}", query);
            return [];
        }
    }

    private static OneOf<DownloadRecord, Ignore> ApplyRules(Item feedItem, IEnumerable<DownloadRule> rules)
    {
        var matchingRule = rules.FirstOrDefault(r => r.Matches(feedItem));
        if (matchingRule is null)
            return Ignore.Instance;
        var id = feedItem.ParseId();
        if (id is null)
            return Ignore.Instance;
        if (!Uri.TryCreate(feedItem.Link, UriKind.RelativeOrAbsolute, out var magnetLink))
            return Ignore.Instance;
        return new DownloadRecord()
        {
            Id = id.Value,
            Title = feedItem.Title,
            IngestedAt = DateTime.UtcNow,
            RuleId = matchingRule.Id,
            MagnetLink = magnetLink
        };
    }

    private struct Ignore
    {
        public static readonly Ignore Instance = new();
    }
}