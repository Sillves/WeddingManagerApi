
using System.Reflection;

namespace WeddingManager.Web.Endpoints;

public static class EndpointMappingExtensions
{
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app, bool useApiPrefix = false)
    {
        // Conditionally create a route group with /api prefix
        var routeBuilder = useApiPrefix ? app.MapGroup("/api") : app;
        
        // 1. Find all types in the current assembly
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpoint).IsAssignableFrom(t));

        // 2. Instantiate and call MapEndpoint on each one
        foreach (var type in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(routeBuilder);
        }

        return app;
    }
}
