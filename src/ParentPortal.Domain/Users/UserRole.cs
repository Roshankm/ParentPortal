namespace ParentPortal.Domain.Users;

public enum UserRole
{
    Parent,
    Teacher,
    SchoolAdmin,
}

public static class UserRoleExtensions
{
    public static bool CanAuthorAnnouncements(this UserRole role) => role is UserRole.Teacher or UserRole.SchoolAdmin;

    public static bool CanManageClassrooms(this UserRole role) => role is UserRole.SchoolAdmin;
}
