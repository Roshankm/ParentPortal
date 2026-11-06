using ParentPortal.Application.Announcements;
using ParentPortal.Contracts;

namespace ParentPortal.Api.Endpoints;

internal static class AnnouncementEndpoints
{
    public static IEndpointRouteBuilder MapAnnouncementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/announcements").WithTags("Announcements");

        group.MapGet("/", async (
            Guid? classroomId, int? page, int? pageSize,
            ListAnnouncementsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ListAnnouncementsQuery(classroomId, page ?? 1, pageSize ?? 20), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
        })
        .WithName("ListAnnouncements")
        .WithSummary("List announcements for the caller's school (parents see published only).");

        group.MapPost("/", async (CreateAnnouncementRequest request, CreateAnnouncementHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/announcements/{result.Value.Id}", result.Value)
                : result.Error!.ToProblem();
        })
        .WithName("CreateAnnouncement")
        .WithSummary("Create an announcement (teachers and school admins).");

        group.MapPost("/{id:guid}/publish", async (Guid id, PublishAnnouncementHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToProblem();
        })
        .WithName("PublishAnnouncement");

        group.MapPost("/{id:guid}/acknowledge", async (Guid id, AcknowledgeAnnouncementHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.Error!.ToProblem();
        })
        .WithName("AcknowledgeAnnouncement")
        .WithSummary("Parent confirms they have read an announcement.");

        return app;
    }
}
