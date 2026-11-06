using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ParentPortal.Application.Abstractions;
using ParentPortal.Domain.Announcements;
using ParentPortal.Infrastructure.Email;
using ParentPortal.Infrastructure.Persistence;

namespace ParentPortal.Infrastructure.Jobs;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan RemindAfter { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Sends one reminder for urgent announcements that are still open after <see cref="ReminderOptions.RemindAfter"/>.
/// Runs across all schools, so it deliberately bypasses the tenant query filter.
/// </summary>
public sealed partial class AnnouncementReminderJob(
    IServiceScopeFactory scopeFactory,
    IOptions<ReminderOptions> options,
    ILogger<AnnouncementReminderJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailure(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = clock.UtcNow;
        var cutoff = now - options.Value.RemindAfter;

        var due = await db.Announcements
            .IgnoreQueryFilters()
            .Where(a => a.Status == AnnouncementStatus.Published
                        && a.Priority == AnnouncementPriority.Urgent
                        && a.ReminderSentAtUtc == null
                        && a.PublishedAtUtc <= cutoff)
            .ToListAsync(cancellationToken);

        foreach (var announcement in due.Where(a => a.NeedsReminder(now, options.Value.RemindAfter)))
        {
            await email.SendAsync(
                $"parents+{announcement.SchoolId:N}@example.com",
                $"Reminder: {announcement.Title}",
                announcement.Body,
                cancellationToken);
            announcement.MarkReminderSent(now);
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            LogSent(logger, due.Count);
        }

        return due.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent {Count} announcement reminder(s)")]
    private static partial void LogSent(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Announcement reminder run failed")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
