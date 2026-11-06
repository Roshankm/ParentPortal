using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Abstractions;

/// <summary>Who is calling and which school (tenant) they belong to.</summary>
public interface ITenantContext
{
    Guid SchoolId { get; }

    string UserId { get; }

    UserRole Role { get; }
}
