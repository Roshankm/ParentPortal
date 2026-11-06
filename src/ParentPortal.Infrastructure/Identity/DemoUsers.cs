using ParentPortal.Contracts;
using ParentPortal.Domain.Users;

namespace ParentPortal.Infrastructure.Identity;

/// <summary>
/// Fictional users for local development and the public demo. In production users come from Entra ID.
/// </summary>
public static class DemoUsers
{
    public static readonly Guid GreenfieldSchoolId = Guid.Parse("8d5b6f0a-3c1e-4b7a-9f52-1a2b3c4d5e01");
    public static readonly Guid OakridgeSchoolId = Guid.Parse("8d5b6f0a-3c1e-4b7a-9f52-1a2b3c4d5e02");

    public const string GreenfieldName = "Greenfield International School";
    public const string OakridgeName = "Oakridge Academy";

    public static readonly IReadOnlyList<DemoUser> All =
    [
        new("admin.greenfield", "Sara Ahmed", UserRole.SchoolAdmin, GreenfieldSchoolId),
        new("teacher.greenfield", "David Thomas", UserRole.Teacher, GreenfieldSchoolId),
        new("parent.greenfield.1", "Fatima Khan", UserRole.Parent, GreenfieldSchoolId),
        new("parent.greenfield.2", "John Mathew", UserRole.Parent, GreenfieldSchoolId),
        new("teacher.oakridge", "Aisha Rahman", UserRole.Teacher, OakridgeSchoolId),
        new("parent.oakridge", "Priya Nair", UserRole.Parent, OakridgeSchoolId),
    ];

    public static DemoUser? Find(string? id) =>
        All.FirstOrDefault(u => string.Equals(u.Id, id, StringComparison.OrdinalIgnoreCase));

    public static DemoUserDto ToDto(this DemoUser user) => new(
        user.Id,
        user.Name,
        user.Role.ToString(),
        user.SchoolId,
        user.SchoolId == GreenfieldSchoolId ? GreenfieldName : OakridgeName);
}

public sealed record DemoUser(string Id, string Name, UserRole Role, Guid SchoolId);
