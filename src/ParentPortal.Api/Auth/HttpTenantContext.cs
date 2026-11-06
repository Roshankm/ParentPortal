using System.Security.Claims;

using ParentPortal.Application.Abstractions;
using ParentPortal.Domain.Users;

namespace ParentPortal.Api.Auth;

/// <summary>Resolves the current user and school from the authenticated principal.</summary>
internal sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    // Outside a request (background jobs, seeding) there is no tenant: Guid.Empty matches no rows.
    public Guid SchoolId => Guid.TryParse(User?.FindFirst(AppClaims.SchoolId)?.Value, out var id) ? id : Guid.Empty;

    public string UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("oid")?.Value
        ?? string.Empty;

    public UserRole Role => Enum.TryParse<UserRole>(User?.FindFirst(ClaimTypes.Role)?.Value, out var role)
        ? role
        : UserRole.Parent; // least privilege
}
