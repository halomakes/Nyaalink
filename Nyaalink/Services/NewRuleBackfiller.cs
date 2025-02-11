using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Services;

/// <summary>
/// Performs a more specific search to backfill episodes of a newly-added show that would be too far back to appear in
/// the normal RSS feed
/// </summary>
public class NewRuleBackfiller(
    Channel<RuleCreatedEvent> channel,
    IServiceProvider serviceProvider,
    ILogger<NewRuleBackfiller> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            var @event = await channel.Reader.ReadAsync(stoppingToken);
            await using var scope = serviceProvider.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetRequiredService<FeedConsumer>();
            var db = scope.ServiceProvider.GetRequiredService<DownloadContext>();
            try
            {
                foreach (var query in @event.Rule.Queries ?? [])
                {
                    var temporaryQuery = await db.Queries
                        .AsNoTracking()
                        .FirstOrDefaultAsync(q => q.Id == query.Id, stoppingToken);
                    if (temporaryQuery is null)
                        continue;
                    var partialName = string.IsNullOrWhiteSpace(@event.Rule.BackfillFilter)
                        ? @event.Rule.Label.Split(" ").FirstOrDefault()
                        : @event.Rule.BackfillFilter;
                    temporaryQuery.Query += $" {partialName}";
                    temporaryQuery.Rules = [@event.Rule];
                    await consumer.IngestAsync(temporaryQuery, stoppingToken);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Unable to backfill new rule");
            }
        }
    }
}