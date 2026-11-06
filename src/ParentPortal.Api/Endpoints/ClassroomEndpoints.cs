using ParentPortal.Application.Classrooms;
using ParentPortal.Contracts;

namespace ParentPortal.Api.Endpoints;

internal static class ClassroomEndpoints
{
    public static IEndpointRouteBuilder MapClassroomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/classrooms").WithTags("Classrooms");

        group.MapGet("/", async (ListClassroomsHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)))
            .WithName("ListClassrooms");

        group.MapPost("/", async (CreateClassroomRequest request, CreateClassroomHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/classrooms/{result.Value.Id}", result.Value)
                : result.Error!.ToProblem();
        })
        .WithName("CreateClassroom")
        .WithSummary("Create a classroom (school admins only).");

        return app;
    }
}
