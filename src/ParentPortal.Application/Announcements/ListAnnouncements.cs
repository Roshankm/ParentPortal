using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Announcements;

public sealed record ListAnnouncementsQuery(Guid? ClassroomId, int Page = 1, int PageSize = 20);

public sealed class ListAnnouncementsHandler(
    ITenantContext tenant,
    IAnnouncementRepository announcements,
    IClassroomRepository classrooms)
{
    public const int MaxPageSize = 100;

    public async Task<Result<PagedResult<AnnouncementDto>>> HandleAsync(ListAnnouncementsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // Parents only ever see published announcements; staff also see drafts.
        var filter = new AnnouncementFilter(
            query.ClassroomId,
            PublishedOnly: tenant.Role == UserRole.Parent,
            Skip: (page - 1) * pageSize,
            Take: pageSize);

        var (items, total) = await announcements.ListAsync(filter, cancellationToken);
        var classroomNames = (await classrooms.ListAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Name);

        var dtos = items
            .Select(a => a.ToDto(
                a.ClassroomId is { } cid && classroomNames.TryGetValue(cid, out var name) ? name : null,
                tenant.UserId))
            .ToList();

        return new PagedResult<AnnouncementDto>(dtos, page, pageSize, total);
    }
}
