namespace Nyaalink.Endpoints;

public static class EndpointExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapRuleEndpoints();
        builder.MapQueryEndpoints();
        builder.MapDownloadEndpoints();
    }
}