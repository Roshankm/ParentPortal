using Microsoft.AspNetCore.SignalR;

using ParentPortal.Api.Auth;
using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;

namespace ParentPortal.Api.Realtime;

/// <summary>Each connection joins its school's group, so events never cross tenants.</summary>
public sealed class NotificationsHubEndpoint : Hub
{
    internal static string GroupFor(Guid schoolId) => $"school:{schoolId:N}";

    public override async Task OnConnectedAsync()
    {
        var schoolId = Context.User?.FindFirst(AppClaims.SchoolId)?.Value;
        if (Guid.TryParse(schoolId, out var id))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(id));
        }

        await base.OnConnectedAsync();
    }
}

internal sealed class SignalRNotificationPublisher(IHubContext<NotificationsHubEndpoint> hub) : INotificationPublisher
{
    public Task AnnouncementPublishedAsync(Guid schoolId, AnnouncementDto announcement, CancellationToken cancellationToken) =>
        hub.Clients.Group(NotificationsHubEndpoint.GroupFor(schoolId))
            .SendAsync(NotificationsHub.AnnouncementPublished, announcement, cancellationToken);
}
