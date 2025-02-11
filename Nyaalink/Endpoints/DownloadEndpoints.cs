using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/downloads/recent",
                async ([FromServices] DownloadContext db, CancellationToken ct, [FromQuery] int limit = 100) =>
                {
                    var results = await db.Records
                        .OrderByDescending(static r => r.IngestedAt)
                        .Take(limit)
                        .ToListAsync(ct);
                    return Results.Ok(results);
                })
            .WithName("GetRecentDownloads")
            .Produces<IList<DownloadRecord>>((int)HttpStatusCode.OK);

        builder.MapGet("api/rules/{ruleId:int}/downloads", async ([FromServices] DownloadContext db,
                [FromRouteAttribute] int ruleId, CancellationToken ct, [FromQuery] int limit = 100) =>
            {
                var results = await db.Records
                    .Where(r => r.RuleId == ruleId)
                    .OrderByDescending(static r => r.IngestedAt)
                    .Take(limit)
                    .ToListAsync(ct);
                return Results.Ok(results);
            })
            .WithName("GetDownloadsByRuleId")
            .Produces<IList<DownloadRecord>>((int)HttpStatusCode.OK);
    }
}