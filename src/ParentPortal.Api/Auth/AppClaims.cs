namespace ParentPortal.Api.Auth;

public static class AppClaims
{
    /// <summary>
    /// The school (tenant) a user belongs to. With Entra ID this comes from a directory
    /// extension attribute or a claims-mapping policy on the app registration.
    /// </summary>
    public const string SchoolId = "school_id";

    public const string DemoScheme = "Demo";
}
