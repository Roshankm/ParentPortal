using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Schools;

namespace ParentPortal.Infrastructure.Persistence.Configurations;

internal sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(120).IsRequired();
        builder.Property(s => s.Slug).HasMaxLength(60).IsRequired();
        builder.HasIndex(s => s.Slug).IsUnique();
    }
}

internal sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(Classroom.NameMaxLength).IsRequired();
        builder.HasIndex(c => new { c.SchoolId, c.Name }).IsUnique();
        builder.HasOne<School>().WithMany().HasForeignKey(c => c.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(Announcement.TitleMaxLength).IsRequired();
        builder.Property(a => a.Body).HasMaxLength(Announcement.BodyMaxLength).IsRequired();
        builder.Property(a => a.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(a => new { a.SchoolId, a.Status, a.CreatedAtUtc });

        builder.HasOne<School>().WithMany().HasForeignKey(a => a.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Classroom>().WithMany().HasForeignKey(a => a.ClassroomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Acknowledgements)
            .WithOne()
            .HasForeignKey(ack => ack.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Acknowledgements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AcknowledgementConfiguration : IEntityTypeConfiguration<Acknowledgement>
{
    public void Configure(EntityTypeBuilder<Acknowledgement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ParentId).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => new { a.AnnouncementId, a.ParentId }).IsUnique();
    }
}
