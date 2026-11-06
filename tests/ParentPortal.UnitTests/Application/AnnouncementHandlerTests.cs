using ParentPortal.Application.Abstractions;
using ParentPortal.Application.Announcements;
using ParentPortal.Contracts;
using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Users;

namespace ParentPortal.UnitTests.Application;

public class AnnouncementHandlerTests
{
    private static readonly Guid SchoolA = Guid.NewGuid();
    private static readonly Guid SchoolB = Guid.NewGuid();

    private readonly FakeTenant _tenant = new(SchoolA, "teacher-1", UserRole.Teacher);
    private readonly FakeClock _clock = new();
    private readonly RecordingPublisher _publisher = new();
    private readonly InMemoryStore _store;

    public AnnouncementHandlerTests() => _store = new InMemoryStore(_tenant);

    private CreateAnnouncementHandler CreateHandler() => new(_tenant, _store, _store, _store, _publisher, _clock);

    private static CreateAnnouncementRequest ValidRequest(bool publishNow = true) => new()
    {
        Title = "Library books due",
        Body = "Please return all books by Friday.",
        Priority = Priorities.High,
        PublishNow = publishNow,
    };

    [Fact]
    public async Task Create_and_publish_saves_and_notifies_the_school()
    {
        var result = await CreateHandler().HandleAsync(ValidRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Published", result.Value.Status);
        Assert.Equal(1, _store.SaveCount);
        var notification = Assert.Single(_publisher.Published);
        Assert.Equal(SchoolA, notification.SchoolId);
    }

    [Fact]
    public async Task Create_as_draft_does_not_notify()
    {
        var result = await CreateHandler().HandleAsync(ValidRequest(publishNow: false), CancellationToken.None);

        Assert.Equal("Draft", result.Value.Status);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Parents_cannot_create_announcements()
    {
        _tenant.Role = UserRole.Parent;

        var result = await CreateHandler().HandleAsync(ValidRequest(), CancellationToken.None);

        Assert.Equal(CommonErrors.Forbidden, result.Error);
        Assert.Empty(_store.Announcements);
    }

    [Fact]
    public async Task Create_rejects_unknown_priority()
    {
        var request = ValidRequest();
        request.Priority = "Critical";

        var result = await CreateHandler().HandleAsync(request, CancellationToken.None);

        Assert.Equal("Announcement.InvalidPriority", result.Error!.Code);
    }

    [Fact]
    public async Task Create_rejects_a_classroom_from_another_school()
    {
        var otherSchoolClass = Classroom.Create(SchoolB, "Grade 1", 1).Value;
        _store.Classrooms.Add(otherSchoolClass);
        var request = ValidRequest();
        request.ClassroomId = otherSchoolClass.Id;

        var result = await CreateHandler().HandleAsync(request, CancellationToken.None);

        Assert.Equal(ClassroomErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Parent_can_acknowledge_once()
    {
        var created = await CreateHandler().HandleAsync(ValidRequest(), CancellationToken.None);
        _tenant.Role = UserRole.Parent;
        _tenant.UserId = "parent-1";
        var handler = new AcknowledgeAnnouncementHandler(_tenant, _store, _store, _clock);

        var first = await handler.HandleAsync(created.Value.Id, CancellationToken.None);
        var second = await handler.HandleAsync(created.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(AnnouncementErrors.AlreadyAcknowledged, second.Error);
    }

    [Fact]
    public async Task Acknowledging_another_schools_announcement_returns_not_found()
    {
        var created = await CreateHandler().HandleAsync(ValidRequest(), CancellationToken.None);
        _tenant.SchoolId = SchoolB;
        _tenant.Role = UserRole.Parent;

        var result = await new AcknowledgeAnnouncementHandler(_tenant, _store, _store, _clock)
            .HandleAsync(created.Value.Id, CancellationToken.None);

        Assert.Equal(AnnouncementErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Parents_only_see_published_announcements()
    {
        await CreateHandler().HandleAsync(ValidRequest(publishNow: true), CancellationToken.None);
        await CreateHandler().HandleAsync(ValidRequest(publishNow: false), CancellationToken.None);
        var list = new ListAnnouncementsHandler(_tenant, _store, _store);

        var staffView = await list.HandleAsync(new ListAnnouncementsQuery(null), CancellationToken.None);
        _tenant.Role = UserRole.Parent;
        var parentView = await list.HandleAsync(new ListAnnouncementsQuery(null), CancellationToken.None);

        Assert.Equal(2, staffView.Value.TotalCount);
        Assert.Equal(1, parentView.Value.TotalCount);
        Assert.All(parentView.Value.Items, a => Assert.Equal("Published", a.Status));
    }

    [Fact]
    public async Task List_clamps_page_size()
    {
        var result = await new ListAnnouncementsHandler(_tenant, _store, _store)
            .HandleAsync(new ListAnnouncementsQuery(null, Page: 0, PageSize: 10_000), CancellationToken.None);

        Assert.Equal(1, result.Value.Page);
        Assert.Equal(ListAnnouncementsHandler.MaxPageSize, result.Value.PageSize);
    }

    [Fact]
    public async Task Publishing_a_draft_notifies_and_second_publish_conflicts()
    {
        var draft = await CreateHandler().HandleAsync(ValidRequest(publishNow: false), CancellationToken.None);
        var publish = new PublishAnnouncementHandler(_tenant, _store, _store, _store, _publisher, _clock);

        var first = await publish.HandleAsync(draft.Value.Id, CancellationToken.None);
        var second = await publish.HandleAsync(draft.Value.Id, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Single(_publisher.Published);
        Assert.Equal(AnnouncementErrors.NotDraft, second.Error);
    }
}
