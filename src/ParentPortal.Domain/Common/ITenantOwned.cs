namespace ParentPortal.Domain.Common;

/// <summary>
/// Marks an entity that belongs to exactly one school (tenant).
/// The persistence layer applies a global query filter on <see cref="SchoolId"/>.
/// </summary>
public interface ITenantOwned
{
    Guid SchoolId { get; }
}
