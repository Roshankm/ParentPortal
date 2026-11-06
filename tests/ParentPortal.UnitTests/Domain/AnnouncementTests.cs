using ParentPortal.Domain.Announcements;

namespace ParentPortal.UnitTests.Domain;

public class AnnouncementTests
{
    private static readonly Guid School = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static Announcement NewDraft(AnnouncementPriority priority = AnnouncementPriority.Normal) =>
        Announcement.Create(School, null, "Sports day", "Wear house colours.", priority, "teacher-1", Now).Value;

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Create_rejects_short_titles(string title)
    {
        var result = Announcement.Create(School, null, title, "Body", AnnouncementPriority.Normal, "t", Now);

        Assert.True(result.IsFailure);
        Assert.Equal(AnnouncementErrors.InvalidTitle, result.Error);
    }

    [Fact]
    public void Create_rejects_body_over_max_length()
    {
        var body = new string('x', Announcement.BodyMaxLength + 1);

        var result = Announcement.Create(School, null, "Valid title", body, AnnouncementPriority.Normal, "t", Now);

        Assert.Equal(AnnouncementErrors.InvalidBody, result.Error);
    }

    [Fact]
    public void Create_trims_input_and_starts_as_draft()
    {
        var result = Announcement.Create(School, null, "  Sports day  ", " Body ", AnnouncementPriority.High, "t", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sports day", result.Value.Title);
        Assert.Equal("Body", result.Value.Body);
        Assert.Equal(AnnouncementStatus.Draft, result.Value.Status);
        Assert.Null(result.Value.PublishedAtUtc);
    }

    [Fact]
    public void Publish_sets_status_and_timestamp_once()
    {
        var announcement = NewDraft();

        Assert.True(announcement.Publish(Now).IsSuccess);
        Assert.Equal(AnnouncementStatus.Published, announcement.Status);
        Assert.Equal(Now, announcement.PublishedAtUtc);

        Assert.Equal(AnnouncementErrors.NotDraft, announcement.Publish(Now).Error);
    }

    [Fact]
    public void Acknowledge_requires_published_announcement()
    {
        var announcement = NewDraft();

        Assert.Equal(AnnouncementErrors.NotPublished, announcement.Acknowledge("parent-1", Now).Error);
    }

    [Fact]
    public void Acknowledge_is_idempotent_per_parent()
    {
        var announcement = NewDraft();
        announcement.Publish(Now);

        Assert.True(announcement.Acknowledge("parent-1", Now).IsSuccess);
        Assert.Equal(AnnouncementErrors.AlreadyAcknowledged, announcement.Acknowledge("parent-1", Now).Error);
        Assert.True(announcement.Acknowledge("parent-2", Now).IsSuccess);

        Assert.Equal(2, announcement.Acknowledgements.Count);
        Assert.True(announcement.IsAcknowledgedBy("parent-1"));
    }

    [Fact]
    public void NeedsReminder_only_for_urgent_published_after_delay_and_only_once()
    {
        var delay = TimeSpan.FromHours(24);
        var urgent = NewDraft(AnnouncementPriority.Urgent);
        urgent.Publish(Now);

        Assert.False(urgent.NeedsReminder(Now.AddHours(23), delay));
        Assert.True(urgent.NeedsReminder(Now.AddHours(24), delay));

        urgent.MarkReminderSent(Now.AddHours(24));
        Assert.False(urgent.NeedsReminder(Now.AddHours(48), delay));

        var normal = NewDraft();
        normal.Publish(Now);
        Assert.False(normal.NeedsReminder(Now.AddDays(3), delay));
    }

    [Fact]
    public void Archive_cannot_happen_twice()
    {
        var announcement = NewDraft();

        Assert.True(announcement.Archive().IsSuccess);
        Assert.Equal(AnnouncementErrors.AlreadyArchived, announcement.Archive().Error);
    }
}
