using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Users;

namespace ParentPortal.UnitTests.Application;

internal sealed class FakeTenant(Guid schoolId, string userId, UserRole role) : ITenantContext
{
    public Guid SchoolId { get; set; } = schoolId;

    public string UserId { get; set; } = userId;

    public UserRole Role { get; set; } = role;
}

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
}

/// <summary>In-memory store that mimics the EF Core tenant query filter.</summary>
internal sealed class InMemoryStore(ITenantContext tenant) : IAnnouncementRepository, IClassroomRepository, IUnitOfWork
{
    public List<Announcement> Announcements { get; } = [];

    public List<Classroom> Classrooms { get; } = [];

    public int SaveCount { get; private set; }

    private IEnumerable<Announcement> TenantAnnouncements => Announcements.Where(a => a.SchoolId == tenant.SchoolId);

    private IEnumerable<Classroom> TenantClassrooms => Classrooms.Where(c => c.SchoolId == tenant.SchoolId);

    Task<Announcement?> IAnnouncementRepository.GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(TenantAnnouncements.FirstOrDefault(a => a.Id == id));

    Task<(IReadOnlyList<Announcement> Items, int TotalCount)> IAnnouncementRepository.ListAsync(AnnouncementFilter filter, CancellationToken ct)
    {
        var query = TenantAnnouncements.Where(a => a.Status != AnnouncementStatus.Archived);
        if (filter.PublishedOnly)
        {
            query = query.Where(a => a.Status == AnnouncementStatus.Published);
        }

        if (filter.ClassroomId is { } cid)
        {
            query = query.Where(a => a.ClassroomId == cid || a.ClassroomId == null);
        }

        var list = query.OrderByDescending(a => a.PublishedAtUtc ?? a.CreatedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<Announcement>, int)>((list.Skip(filter.Skip).Take(filter.Take).ToList(), list.Count));
    }

    void IAnnouncementRepository.Add(Announcement announcement) => Announcements.Add(announcement);

    Task<Classroom?> IClassroomRepository.GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(TenantClassrooms.FirstOrDefault(c => c.Id == id));

    Task<IReadOnlyList<Classroom>> IClassroomRepository.ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Classroom>>(TenantClassrooms.ToList());

    Task<bool> IClassroomRepository.NameExistsAsync(string name, CancellationToken ct) =>
        Task.FromResult(TenantClassrooms.Any(c => c.Name == name));

    void IClassroomRepository.Add(Classroom classroom) => Classrooms.Add(classroom);

    Task IUnitOfWork.SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPublisher : INotificationPublisher
{
    public List<(Guid SchoolId, AnnouncementDto Announcement)> Published { get; } = [];

    public Task AnnouncementPublishedAsync(Guid schoolId, AnnouncementDto announcement, CancellationToken ct)
    {
        Published.Add((schoolId, announcement));
        return Task.CompletedTask;
    }
}
