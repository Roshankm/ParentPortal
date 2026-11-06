using ParentPortal.Domain.Common;

namespace ParentPortal.Domain.Classrooms;

public sealed class Classroom : Entity, ITenantOwned
{
    public const int NameMaxLength = 60;

    private Classroom()
    {
    }

    public Guid SchoolId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Grade { get; private set; }

    public static Result<Classroom> Create(Guid schoolId, string name, int grade)
    {
        name = name?.Trim() ?? string.Empty;

        if (name.Length is 0 or > NameMaxLength)
        {
            return ClassroomErrors.InvalidName;
        }

        if (grade is < 1 or > 12)
        {
            return ClassroomErrors.InvalidGrade;
        }

        return new Classroom { SchoolId = schoolId, Name = name, Grade = grade };
    }
}

public static class ClassroomErrors
{
    public static readonly Error InvalidName =
        Error.Validation("Classroom.InvalidName", $"Name is required and must be at most {Classroom.NameMaxLength} characters.");

    public static readonly Error InvalidGrade =
        Error.Validation("Classroom.InvalidGrade", "Grade must be between 1 and 12.");

    public static readonly Error DuplicateName =
        Error.Conflict("Classroom.DuplicateName", "A classroom with this name already exists.");

    public static readonly Error NotFound =
        Error.NotFound("Classroom.NotFound", "Classroom not found.");
}
