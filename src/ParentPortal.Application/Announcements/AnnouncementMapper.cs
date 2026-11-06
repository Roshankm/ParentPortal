using ParentPortal.Contracts;
using ParentPortal.Domain.Announcements;

namespace ParentPortal.Application.Announcements;

internal static class AnnouncementMapper
{
    public static AnnouncementDto ToDto(this Announcement a, string? classroomName, string currentUserId) => new(
        a.Id,
        a.Title,
        a.Body,
        a.Priority.ToString(),
        a.Status.ToString(),
        a.ClassroomId,
        classroomName,
        a.CreatedBy,
        a.CreatedAtUtc,
        a.PublishedAtUtc,
        a.Acknowledgements.Count,
        a.IsAcknowledgedBy(currentUserId));
}
