using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using ParentPortal.Application.Abstractions;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Schools;

namespace ParentPortal.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
    : DbContext(options), IUnitOfWork
{
    public DbSet<School> Schools => Set<School>();

    public DbSet<Classroom> Classrooms => Set<Classroom>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    /// <summary>
    /// Read by the global query filters on every query, so each request only ever sees its own school's rows.
    /// </summary>
    private Guid CurrentSchoolId => tenant.SchoolId;

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Multi-tenancy: shared schema, row-level isolation by SchoolId.
        modelBuilder.Entity<Classroom>().HasQueryFilter(c => c.SchoolId == CurrentSchoolId);
        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.SchoolId == CurrentSchoolId);

        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Ids are created in the domain (Guid v7), never by the database.
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                entityType.FindProperty(nameof(Entity.Id))!.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }
}
