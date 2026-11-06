using ParentPortal.Domain.Common;

namespace ParentPortal.Domain.Schools;

/// <summary>A school is the tenant boundary: every other aggregate belongs to one school.</summary>
public sealed class School : Entity
{
    private School()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public static School Create(Guid id, string name, string slug) => new()
    {
        Id = id,
        Name = name.Trim(),
        Slug = slug.Trim().ToLowerInvariant(),
    };
}
