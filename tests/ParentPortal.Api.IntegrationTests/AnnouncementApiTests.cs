using System.Net;
using System.Net.Http.Json;

using ParentPortal.Contracts;

namespace ParentPortal.Api.IntegrationTests;

public class AnnouncementApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string GreenfieldTeacher = "teacher.greenfield";
    private const string GreenfieldParent = "parent.greenfield.2";
    private const string OakridgeTeacher = "teacher.oakridge";
    private const string OakridgeParent = "parent.oakridge";

    [Fact]
    public async Task Health_endpoint_is_public()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/announcements");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Parents_only_see_published_announcements()
    {
        var page = await factory.ClientFor(GreenfieldParent)
            .GetFromJsonAsync<PagedResult<AnnouncementDto>>("/api/announcements");

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, a => Assert.Equal("Published", a.Status));
    }

    [Fact]
    public async Task Schools_never_see_each_others_announcements()
    {
        var greenfield = await factory.ClientFor(GreenfieldTeacher)
            .GetFromJsonAsync<PagedResult<AnnouncementDto>>("/api/announcements?pageSize=100");
        var oakridge = await factory.ClientFor(OakridgeTeacher)
            .GetFromJsonAsync<PagedResult<AnnouncementDto>>("/api/announcements?pageSize=100");

        var greenfieldIds = greenfield!.Items.Select(a => a.Id).ToHashSet();
        Assert.DoesNotContain(oakridge!.Items, a => greenfieldIds.Contains(a.Id));
        Assert.Contains(oakridge.Items, a => a.Title == "Reading week");
        Assert.DoesNotContain(greenfield.Items, a => a.Title == "Reading week");
    }

    [Fact]
    public async Task Cross_tenant_acknowledgement_returns_404()
    {
        var greenfieldItem = (await factory.ClientFor(GreenfieldParent)
            .GetFromJsonAsync<PagedResult<AnnouncementDto>>("/api/announcements"))!.Items[0];

        var response = await factory.ClientFor(OakridgeParent)
            .PostAsync($"/api/announcements/{greenfieldItem.Id}/acknowledge", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_publishes_and_parent_acknowledges_once()
    {
        var teacher = factory.ClientFor(GreenfieldTeacher);
        var create = await teacher.PostAsJsonAsync("/api/announcements", new CreateAnnouncementRequest
        {
            Title = "Integration test notice",
            Body = "Created by the integration tests.",
            Priority = Priorities.Normal,
            PublishNow = true,
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<AnnouncementDto>();

        var parent = factory.ClientFor(GreenfieldParent);
        var first = await parent.PostAsync($"/api/announcements/{created!.Id}/acknowledge", null);
        var second = await parent.PostAsync($"/api/announcements/{created.Id}/acknowledge", null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Parents_cannot_create_announcements()
    {
        var response = await factory.ClientFor(GreenfieldParent).PostAsJsonAsync("/api/announcements",
            new CreateAnnouncementRequest { Title = "Not allowed", Body = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_announcement_returns_400_problem_details()
    {
        var response = await factory.ClientFor(GreenfieldTeacher).PostAsJsonAsync("/api/announcements",
            new CreateAnnouncementRequest { Title = "x", Body = "Too short a title" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
