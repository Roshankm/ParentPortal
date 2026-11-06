using Microsoft.AspNetCore.SignalR.Client;

using ParentPortal.Contracts;

namespace ParentPortal.Web.Services;

/// <summary>
/// One SignalR connection per Blazor circuit. Raises <see cref="AnnouncementPublished"/> when
/// a teacher in the same school publishes something.
/// </summary>
public sealed partial class NotificationClient(IConfiguration configuration, DemoSession session, ILogger<NotificationClient> logger)
    : IAsyncDisposable
{
    private HubConnection? _connection;
    private string? _connectedAs;

    public event Func<AnnouncementDto, Task>? AnnouncementPublished;

    public async Task ConnectAsync()
    {
        var user = session.CurrentUser;
        if (user is null || user.Id == _connectedAs)
        {
            return;
        }

        await DisposeAsync();

        var baseUrl = configuration["Api:BaseUrl"]!.TrimEnd('/');
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}{NotificationsHub.Path}", options =>
            {
                options.Headers[DemoAuth.Header] = user.Id;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<AnnouncementDto>(NotificationsHub.AnnouncementPublished, async dto =>
        {
            if (AnnouncementPublished is { } handler)
            {
                await handler(dto);
            }
        });

        try
        {
            await _connection.StartAsync();
            _connectedAs = user.Id;
        }
        catch (HttpRequestException ex)
        {
            Log.ConnectFailed(logger, ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connectedAs = null;
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Real-time notifications unavailable")]
        public static partial void ConnectFailed(ILogger logger, Exception exception);
    }
}
