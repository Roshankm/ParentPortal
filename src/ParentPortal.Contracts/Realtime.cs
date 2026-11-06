namespace ParentPortal.Contracts;

/// <summary>SignalR hub route and event names shared by server and client.</summary>
public static class NotificationsHub
{
    public const string Path = "/hubs/notifications";
    public const string AnnouncementPublished = "AnnouncementPublished";
}

public sealed record DemoUserDto(string Id, string Name, string Role, Guid SchoolId, string SchoolName);

public static class DemoAuth
{
    /// <summary>Header used by the development-only demo authentication scheme.</summary>
    public const string Header = "X-Demo-User";
}
