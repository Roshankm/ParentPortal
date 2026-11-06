using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Announcements;

public sealed class PublishAnnouncementHandler(
    ITenantContext tenant,
    IAnnouncementRepository announcements,
    IClassroomRepository classrooms,
    IUnitOfWork unitOfWork,
    INotificationPublisher notifications,
    IClock clock)
{
    public async Task<Result<AnnouncementDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!tenant.Role.CanAuthorAnnouncements())
        {
            return CommonErrors.Forbidden;
        }

        var announcement = await announcements.GetAsync(id, cancellationToken);
        if (announcement is null || announcement.SchoolId != tenant.SchoolId)
        {
            return AnnouncementErrors.NotFound;
        }

        var published = announcement.Publish(clock.UtcNow);
        if (published.IsFailure)
        {
            return published.Error!;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var classroomName = announcement.ClassroomId is { } cid
            ? (await classrooms.GetAsync(cid, cancellationToken))?.Name
            : null;

        var dto = announcement.ToDto(classroomName, tenant.UserId);
        await notifications.AnnouncementPublishedAsync(tenant.SchoolId, dto, cancellationToken);
        return dto;
    }
}
