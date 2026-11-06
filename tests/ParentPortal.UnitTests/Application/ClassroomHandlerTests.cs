using ParentPortal.Application.Abstractions;
using ParentPortal.Application.Classrooms;
using ParentPortal.Contracts;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Users;

namespace ParentPortal.UnitTests.Application;

public class ClassroomHandlerTests
{
    private readonly FakeTenant _tenant = new(Guid.NewGuid(), "admin-1", UserRole.SchoolAdmin);
    private readonly InMemoryStore _store;

    public ClassroomHandlerTests() => _store = new InMemoryStore(_tenant);

    [Fact]
    public async Task Admin_can_create_classroom_but_not_a_duplicate()
    {
        var handler = new CreateClassroomHandler(_tenant, _store, _store);
        var request = new CreateClassroomRequest { Name = "Grade 5 - C", Grade = 5 };

        var first = await handler.HandleAsync(request, CancellationToken.None);
        var second = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(ClassroomErrors.DuplicateName, second.Error);
    }

    [Theory]
    [InlineData(UserRole.Teacher)]
    [InlineData(UserRole.Parent)]
    public async Task Only_admins_can_create_classrooms(UserRole role)
    {
        _tenant.Role = role;

        var result = await new CreateClassroomHandler(_tenant, _store, _store)
            .HandleAsync(new CreateClassroomRequest { Name = "Grade 1", Grade = 1 }, CancellationToken.None);

        Assert.Equal(CommonErrors.Forbidden, result.Error);
    }

    [Fact]
    public async Task List_is_sorted_by_grade_then_name()
    {
        _store.Classrooms.Add(Classroom.Create(_tenant.SchoolId, "Grade 7 - B", 7).Value);
        _store.Classrooms.Add(Classroom.Create(_tenant.SchoolId, "Grade 4 - B", 4).Value);
        _store.Classrooms.Add(Classroom.Create(_tenant.SchoolId, "Grade 4 - A", 4).Value);

        var result = await new ListClassroomsHandler(_store).HandleAsync(CancellationToken.None);

        Assert.Equal(new[] { "Grade 4 - A", "Grade 4 - B", "Grade 7 - B" }, result.Select(c => c.Name));
    }
}
