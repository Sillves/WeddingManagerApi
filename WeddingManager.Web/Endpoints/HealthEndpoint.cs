namespace WeddingManager.Web.Endpoints;

public class HealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => "Healthy");
    }
}