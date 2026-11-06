using System.ComponentModel.DataAnnotations;

namespace ParentPortal.Contracts;

public sealed record ClassroomDto(Guid Id, string Name, int Grade);

public sealed class CreateClassroomRequest
{
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Grade { get; set; } = 1;
}
