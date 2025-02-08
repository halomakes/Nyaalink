using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/downloads/recent", async ([FromServices] DownloadContext db, CancellationToken ct, [FromQuery] int limit = 100) =>
        {
            var results = await db.Records
                .OrderByDescending(static r => r.IngestedAt)
                .Take(limit)
                .ToListAsync(ct);
            return Results.Ok(results);
        });
    }
}