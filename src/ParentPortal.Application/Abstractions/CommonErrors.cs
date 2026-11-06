using ParentPortal.Domain.Common;

namespace ParentPortal.Application.Abstractions;

public static class CommonErrors
{
    public static readonly Error Forbidden =
        Error.Forbidden("Auth.Forbidden", "You are not allowed to perform this action.");

    public static Error InvalidPriority(string value) =>
        Error.Validation("Announcement.InvalidPriority", $"'{value}' is not a valid priority.");
}
