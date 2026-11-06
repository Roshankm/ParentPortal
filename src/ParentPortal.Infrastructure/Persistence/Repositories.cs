using Microsoft.EntityFrameworkCore;

using ParentPortal.Application.Abstractions;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;

namespace ParentPortal.Infrastructure.Persistence;

internal sealed class AnnouncementRepository(AppDbContext db) : IAnnouncementRepository
{
    public Task<Announcement?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Announcements
            .Include(a => a.Acknowledgements)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Announcement> Items, int TotalCount)> ListAsync(
        AnnouncementFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Announcements.AsNoTracking().Where(a => a.Status != AnnouncementStatus.Archived);

        if (filter.PublishedOnly)
        {
            query = query.Where(a => a.Status == AnnouncementStatus.Published);
        }

        if (filter.ClassroomId is { } classroomId)
        {
            // A class feed includes school-wide announcements too.
            query = query.Where(a => a.ClassroomId == classroomId || a.ClassroomId == null);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(a => a.Acknowledgements)
            .OrderByDescending(a => a.PublishedAtUtc ?? a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Announcement announcement) => db.Announcements.Add(announcement);
}

internal sealed class ClassroomRepository(AppDbContext db) : IClassroomRepository
{
    public Task<Classroom?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Classroom>> ListAsync(CancellationToken cancellationToken) =>
        await db.Classrooms.AsNoTracking().ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        db.Classrooms.AnyAsync(c => c.Name == name, cancellationToken);

    public void Add(Classroom classroom) => db.Classrooms.Add(classroom);
}
