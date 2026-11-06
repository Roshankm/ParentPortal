using Microsoft.Extensions.Logging;

namespace ParentPortal.Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

/// <summary>Development sender that writes emails to the log. Swap for SMTP / Azure Communication Services in production.</summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        LogEmail(logger, to, subject);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email to {To}: {Subject}")]
    private static partial void LogEmail(ILogger logger, string to, string subject);
}
