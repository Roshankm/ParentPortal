using ParentPortal.Application.Abstractions;
using ParentPortal.Contracts;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Common;
using ParentPortal.Domain.Users;

namespace ParentPortal.Application.Classrooms;

public sealed class ListClassroomsHandler(IClassroomRepository classrooms)
{
    public async Task<IReadOnlyList<ClassroomDto>> HandleAsync(CancellationToken cancellationToken) =>
        (await classrooms.ListAsync(cancellationToken))
            .OrderBy(c => c.Grade)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new ClassroomDto(c.Id, c.Name, c.Grade))
            .ToList();
}

public sealed class CreateClassroomHandler(
    ITenantContext tenant,
    IClassroomRepository classrooms,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ClassroomDto>> HandleAsync(CreateClassroomRequest request, CancellationToken cancellationToken)
    {
        if (!tenant.Role.CanManageClassrooms())
        {
            return CommonErrors.Forbidden;
        }

        var created = Classroom.Create(tenant.SchoolId, request.Name, request.Grade);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        if (await classrooms.NameExistsAsync(created.Value.Name, cancellationToken))
        {
            return ClassroomErrors.DuplicateName;
        }

        classrooms.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ClassroomDto(created.Value.Id, created.Value.Name, created.Value.Grade);
    }
}
