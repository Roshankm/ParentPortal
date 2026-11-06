using ParentPortal.Contracts;

namespace ParentPortal.Application.Abstractions;

/// <summary>Pushes real-time events to connected clients of one school.</summary>
public interface INotificationPublisher
{
    Task AnnouncementPublishedAsync(Guid schoolId, AnnouncementDto announcement, CancellationToken cancellationToken);
}
