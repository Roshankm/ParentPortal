using ParentPortal.Domain.Common;

namespace ParentPortal.Domain.Announcements;

/// <summary>
/// Aggregate root for a school or class announcement. All state changes go through
/// methods that enforce the business rules, so the entity can never be invalid.
/// </summary>
public sealed class Announcement : Entity, ITenantOwned
{
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 120;
    public const int BodyMaxLength = 4000;

    private readonly List<Acknowledgement> _acknowledgements = [];

    private Announcement()
    {
    }

    public Guid SchoolId { get; private set; }

    /// <summary>Target class, or <c>null</c> for a school-wide announcement.</summary>
    public Guid? ClassroomId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public AnnouncementPriority Priority { get; private set; }

    public AnnouncementStatus Status { get; private set; }

    public string CreatedBy { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public DateTime? ReminderSentAtUtc { get; private set; }

    public IReadOnlyCollection<Acknowledgement> Acknowledgements => _acknowledgements.AsReadOnly();

    public static Result<Announcement> Create(
        Guid schoolId,
        Guid? classroomId,
        string title,
        string body,
        AnnouncementPriority priority,
        string createdBy,
        DateTime nowUtc)
    {
        title = title?.Trim() ?? string.Empty;
        body = body?.Trim() ?? string.Empty;

        if (title.Length is < TitleMinLength or > TitleMaxLength)
        {
            return AnnouncementErrors.InvalidTitle;
        }

        if (body.Length is 0 or > BodyMaxLength)
        {
            return AnnouncementErrors.InvalidBody;
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return AnnouncementErrors.MissingAuthor;
        }

        return new Announcement
        {
            SchoolId = schoolId,
            ClassroomId = classroomId,
            Title = title,
            Body = body,
            Priority = priority,
            Status = AnnouncementStatus.Draft,
            CreatedBy = createdBy,
            CreatedAtUtc = nowUtc,
        };
    }

    public Result Publish(DateTime nowUtc)
    {
        if (Status != AnnouncementStatus.Draft)
        {
            return AnnouncementErrors.NotDraft;
        }

        Status = AnnouncementStatus.Published;
        PublishedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Archive()
    {
        if (Status == AnnouncementStatus.Archived)
        {
            return AnnouncementErrors.AlreadyArchived;
        }

        Status = AnnouncementStatus.Archived;
        return Result.Success();
    }

    public Result Acknowledge(string parentId, DateTime nowUtc)
    {
        if (Status != AnnouncementStatus.Published)
        {
            return AnnouncementErrors.NotPublished;
        }

        if (IsAcknowledgedBy(parentId))
        {
            return AnnouncementErrors.AlreadyAcknowledged;
        }

        _acknowledgements.Add(Acknowledgement.Create(Id, parentId, nowUtc));
        return Result.Success();
    }

    public bool IsAcknowledgedBy(string parentId) =>
        _acknowledgements.Exists(a => string.Equals(a.ParentId, parentId, StringComparison.Ordinal));

    /// <summary>
    /// Urgent announcements that are still unacknowledged after <paramref name="after"/> get one reminder.
    /// </summary>
    public bool NeedsReminder(DateTime nowUtc, TimeSpan after) =>
        Status == AnnouncementStatus.Published
        && Priority == AnnouncementPriority.Urgent
        && ReminderSentAtUtc is null
        && PublishedAtUtc is { } published
        && nowUtc - published >= after;

    public void MarkReminderSent(DateTime nowUtc) => ReminderSentAtUtc = nowUtc;
}

public static class AnnouncementErrors
{
    public static readonly Error InvalidTitle = Error.Validation(
        "Announcement.InvalidTitle",
        $"Title must be between {Announcement.TitleMinLength} and {Announcement.TitleMaxLength} characters.");

    public static readonly Error InvalidBody = Error.Validation(
        "Announcement.InvalidBody",
        $"Body is required and must be at most {Announcement.BodyMaxLength} characters.");

    public static readonly Error MissingAuthor =
        Error.Validation("Announcement.MissingAuthor", "An author is required.");

    public static readonly Error NotDraft =
        Error.Conflict("Announcement.NotDraft", "Only draft announcements can be published.");

    public static readonly Error AlreadyArchived =
        Error.Conflict("Announcement.AlreadyArchived", "The announcement is already archived.");

    public static readonly Error NotPublished =
        Error.Conflict("Announcement.NotPublished", "Only published announcements can be acknowledged.");

    public static readonly Error AlreadyAcknowledged =
        Error.Conflict("Announcement.AlreadyAcknowledged", "You have already acknowledged this announcement.");

    public static readonly Error NotFound =
        Error.NotFound("Announcement.NotFound", "Announcement not found.");
}
