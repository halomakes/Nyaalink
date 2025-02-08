using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Endpoints;

public static class QueryEndpoints
{
    public static void MapQueryEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/queries",
                async ([FromServices] DownloadContext db, CancellationToken ct) => await db.Queries.ToListAsync(ct))
            .WithName("GetQueries")
            .Produces<IList<DownloadQuery>>((int)HttpStatusCode.OK);

        builder.MapPost("api/queries",
                async ([FromServices] DownloadContext db, [FromBody] DownloadQuery query, CancellationToken ct) =>
                {
                    db.Queries.Add(query);
                    await db.SaveChangesAsync(ct);
                    return Results.CreatedAtRoute("GetQuery", new { queryId = query.Id }, query);
                })
            .WithName("CreateQuery")
            .Produces<DownloadQuery>((int)HttpStatusCode.Created)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);

        builder.MapPut("api/queries/{queryId:int}", async ([FromServices] DownloadContext db, [FromRoute] uint queryId,
                [FromBody] DownloadQuery query, CancellationToken ct) =>
            {
                if (query.Id != queryId)
                    return Results.BadRequest("Query ID in route and body must match");
                var existing = await db.Queries.FindAsync([queryId], cancellationToken: ct);
                if (existing is null)
                    return Results.NotFound($"Query {queryId} not found");

                existing.Query = query.Query;
                await db.SaveChangesAsync(ct);
                return Results.Ok(existing);
            })
            .WithName("UpdateQuery")
            .Produces<DownloadQuery>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);

        builder.MapGet("api/queries/{queryId:int}",
                async ([FromServices] DownloadContext db, [FromRoute] uint queryId, CancellationToken ct) =>
                {
                    var existing = await db.Queries.FindAsync([queryId], cancellationToken: ct);
                    return existing is null ? Results.NotFound($"Query {queryId} not found") : Results.Ok(existing);
                })
            .WithName("GetQuery")
            .Produces<DownloadQuery>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound);

        builder.MapDelete("api/queries/{queryId:int}",
                async ([FromServices] DownloadContext db, [FromRoute] uint queryId, CancellationToken ct) =>
                {
                    var existing = await db.Queries
                        .FindAsync([queryId], cancellationToken: ct);
                    if (existing is null)
                        return Results.NotFound($"Query {queryId} not found");

                    var links = await db.Rules.Where(r => r.Queries!.Contains(existing)).ToListAsync(ct);
                    if (links.Count != 0)
                        return Results.Conflict(
                            $"Query is used by rules {string.Join(", ", links)} and cannot be deleted");
                    db.Queries.Remove(existing);
                    await db.SaveChangesAsync(ct);
                    return Results.Ok();
                })
            .WithName("DeleteQuery")
            .Produces<DownloadQuery>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.Conflict);
    }
}