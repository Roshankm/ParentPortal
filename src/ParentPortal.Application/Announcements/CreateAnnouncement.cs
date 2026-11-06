using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Announcements;

public sealed class CreateAnnouncementHandler(
    ITenantContext tenant,
    IAnnouncementRepository announcements,
    IClassroomRepository classrooms,
    IUnitOfWork unitOfWork,
    INotificationPublisher notifications,
    IClock clock)
{
    public async Task<Result<AnnouncementDto>> HandleAsync(CreateAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.Role.CanAuthorAnnouncements())
        {
            return CommonErrors.Forbidden;
        }

        if (!Enum.TryParse<AnnouncementPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            return CommonErrors.InvalidPriority(request.Priority);
        }

        Classroom? classroom = null;
        if (request.ClassroomId is { } classroomId)
        {
            classroom = await classrooms.GetAsync(classroomId, cancellationToken);
            if (classroom is null || classroom.SchoolId != tenant.SchoolId)
            {
                return ClassroomErrors.NotFound;
            }
        }

        var created = Announcement.Create(
            tenant.SchoolId, request.ClassroomId, request.Title, request.Body, priority, tenant.UserId, clock.UtcNow);

        if (created.IsFailure)
        {
            return created.Error!;
        }

        var announcement = created.Value;
        if (request.PublishNow)
        {
            announcement.Publish(clock.UtcNow);
        }

        announcements.Add(announcement);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = announcement.ToDto(classroom?.Name, tenant.UserId);
        if (announcement.Status == AnnouncementStatus.Published)
        {
            await notifications.AnnouncementPublishedAsync(tenant.SchoolId, dto, cancellationToken);
        }

        return dto;
    }
}
