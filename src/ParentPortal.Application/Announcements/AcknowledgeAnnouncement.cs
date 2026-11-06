using ParentPortal.Application.Abstractions;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Announcements;

public sealed class AcknowledgeAnnouncementHandler(
    ITenantContext tenant,
    IAnnouncementRepository announcements,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (tenant.Role != UserRole.Parent)
        {
            return CommonErrors.Forbidden;
        }

        var announcement = await announcements.GetAsync(id, cancellationToken);
        if (announcement is null || announcement.SchoolId != tenant.SchoolId)
        {
            return AnnouncementErrors.NotFound;
        }

        var result = announcement.Acknowledge(tenant.UserId, clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
