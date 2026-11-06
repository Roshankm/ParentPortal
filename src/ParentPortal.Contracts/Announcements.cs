using System.ComponentModel.DataAnnotations;

namespace ParentPortal.Contracts;

public static class Priorities
{
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";

    public static readonly IReadOnlyList<string> All = [Normal, High, Urgent];
}

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    string Priority,
    string Status,
    Guid? ClassroomId,
    string? ClassroomName,
    string CreatedBy,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    int AcknowledgementCount,
    bool AcknowledgedByMe);

public sealed class CreateAnnouncementRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = Priorities.Normal;

    /// <summary>Target class; <c>null</c> means the whole school.</summary>
    public Guid? ClassroomId { get; set; }

    public bool PublishNow { get; set; } = true;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
