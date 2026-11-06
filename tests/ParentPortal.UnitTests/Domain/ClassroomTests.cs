using ParentPortal.Domain.Classrooms;

namespace ParentPortal.UnitTests.Domain;

public class ClassroomTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_rejects_grades_outside_1_to_12(int grade)
    {
        var result = Classroom.Create(Guid.NewGuid(), "Grade X", grade);

        Assert.Equal(ClassroomErrors.InvalidGrade, result.Error);
    }

    [Fact]
    public void Create_rejects_blank_name()
    {
        Assert.Equal(ClassroomErrors.InvalidName, Classroom.Create(Guid.NewGuid(), "   ", 3).Error);
    }

    [Fact]
    public void Create_trims_name()
    {
        var result = Classroom.Create(Guid.NewGuid(), "  Grade 3 - A ", 3);

        Assert.Equal("Grade 3 - A", result.Value.Name);
    }
}
