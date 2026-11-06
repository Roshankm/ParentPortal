using ParentPortal.Domain.Common;

namespace ParentPortal.Domain.Announcements;

/// <summary>Records that a parent has read and acknowledged an announcement.</summary>
public sealed class Acknowledgement : Entity
{
    private Acknowledgement()
    {
    }

    public Guid AnnouncementId { get; private set; }

    public string ParentId { get; private set; } = string.Empty;

    public DateTime AcknowledgedAtUtc { get; private set; }

    internal static Acknowledgement Create(Guid announcementId, string parentId, DateTime nowUtc) => new()
    {
        AnnouncementId = announcementId,
        ParentId = parentId,
        AcknowledgedAtUtc = nowUtc,
    };
}
