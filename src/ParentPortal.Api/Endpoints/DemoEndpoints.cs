using ParentPortal.Api.Auth;
using ParentPortal.Infrastructure.Identity;

namespace ParentPortal.Api.Endpoints;

internal static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/demo/users", (AuthMode mode) =>
                mode.UsesEntraId ? Results.NotFound() : Results.Ok(DemoUsers.All.Select(u => u.ToDto())))
            .WithTags("Demo")
            .WithName("ListDemoUsers")
            .AllowAnonymous();

        return app;
    }
}
