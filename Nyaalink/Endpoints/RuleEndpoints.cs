using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nyaalink.Endpoints;

public static class RuleEndpoints
{
    public static void MapRuleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("rules",
                async ([FromServices] DownloadContext db, CancellationToken ct) => await db.Rules.ToListAsync(ct))
            .WithName("GetRules")
            .Produces<IList<DownloadRule>>((int)HttpStatusCode.OK);

        builder.MapPost("rules",
                async ([FromServices] DownloadContext db, [FromServices] Channel<RuleCreatedEvent> chan,
                    [FromBody] DownloadRule rule, CancellationToken ct) =>
                {
                    var queryIds = rule.Queries?.Select(static q => q.Id).ToList();
                    if (queryIds is not null or [])
                    {
                        rule.Queries = await db.Queries.Where(q => queryIds.Contains(q.Id)).ToListAsync(ct);
                    }

                    db.Rules.Add(rule);
                    await db.SaveChangesAsync(ct);
                    db.Entry(rule).State = EntityState.Detached;
                    await chan.Writer.WriteAsync(new(rule), ct);
                    return Results.CreatedAtRoute($"GetRule", new { ruleId = rule.Id }, rule);
                })
            .WithName("CreateRule")
            .Produces<DownloadRule>((int)HttpStatusCode.Created)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);

        builder.MapPut("rules/{ruleId:int}", async ([FromServices] DownloadContext db, [FromRoute] uint ruleId,
                [FromBody] DownloadRule rule, CancellationToken ct) =>
            {
                if (rule.Id != ruleId)
                    return Results.BadRequest("Rule ID in route and body must match");
                var existing = await db.Rules
                    .Include(static r => r.Queries)
                    .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken: ct);
                if (existing is null)
                    return Results.NotFound($"Rule {ruleId} not found");
                var queryIds = rule.Queries?.Select(static q => q.Id).ToList();
                if (queryIds is not null)
                {
                    rule.Queries = await db.Queries.Where(q => queryIds.Contains(q.Id)).ToListAsync(ct);
                }

                existing.Label = rule.Label;
                existing.Pattern = rule.Pattern;
                existing.AniDbId = rule.AniDbId;
                await db.SaveChangesAsync(ct);
                return Results.Ok(existing);
            })
            .WithName("UpdateRule")
            .Produces<DownloadRule>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);

        builder.MapGet("rules/{ruleId:int}",
                async ([FromServices] DownloadContext db, [FromRoute] uint ruleId, CancellationToken ct) =>
                {
                    var existing = await db.Rules
                        .Include(static r => r.Queries)
                        .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken: ct);
                    return existing is null ? Results.NotFound($"Rule {ruleId} not found") : Results.Ok(existing);
                })
            .WithName("GetRule")
            .Produces<DownloadRule>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound);

        builder.MapDelete("rules/{ruleId:int}",
                async ([FromServices] DownloadContext db, [FromRoute] uint ruleId, CancellationToken ct) =>
                {
                    var existing = await db.Rules.FindAsync([ruleId], cancellationToken: ct);
                    if (existing is null)
                        return Results.NotFound($"Rule {ruleId} not found");
                    db.Rules.Remove(existing);
                    await db.SaveChangesAsync(ct);
                    return Results.Ok();
                })
            .WithName("DeleteRule")
            .Produces<DownloadRule>((int)HttpStatusCode.OK)
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound);
    }
}