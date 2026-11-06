using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;

namespace ParentPortal.Application.Abstractions;

public sealed record AnnouncementFilter(Guid? ClassroomId, bool PublishedOnly, int Skip, int Take);

/// <summary>
/// Implementations must scope every query to the current tenant
/// (EF Core does this with a global query filter).
/// </summary>
public interface IAnnouncementRepository
{
    Task<Announcement?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Announcement> Items, int TotalCount)> ListAsync(AnnouncementFilter filter, CancellationToken cancellationToken);

    void Add(Announcement announcement);
}

public interface IClassroomRepository
{
    Task<Classroom?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Classroom>> ListAsync(CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);

    void Add(Classroom classroom);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
