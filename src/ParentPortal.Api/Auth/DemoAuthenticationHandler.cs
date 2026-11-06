using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using ParentPortal.Contracts;
using ParentPortal.Infrastructure.Identity;

namespace ParentPortal.Api.Auth;

/// <summary>
/// Demo-only scheme: the caller names a seeded user in the <c>X-Demo-User</c> header
/// (or the <c>demo_user</c> query string for SignalR). Only registered when Entra ID is not configured.
/// </summary>
internal sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = Request.Headers[DemoAuth.Header];
        if (string.IsNullOrEmpty(userId))
        {
            userId = Request.Query["demo_user"];
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var user = DemoUsers.Find(userId);
        if (user is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown demo user."));
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AppClaims.SchoolId, user.SchoolId.ToString()),
        ];

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
