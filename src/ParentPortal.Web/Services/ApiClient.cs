using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using ParentPortal.Contracts;

namespace ParentPortal.Web.Services;

/// <summary>Typed client for ParentPortal.Api. Adds the demo identity header to every call.</summary>
public sealed class ApiClient(HttpClient http, DemoSession session)
{
    public Task<IReadOnlyList<DemoUserDto>?> GetDemoUsersAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<DemoUserDto>>("api/demo/users", ct);

    public async Task<PagedResult<AnnouncementDto>> GetAnnouncementsAsync(Guid? classroomId, int page, CancellationToken ct = default)
    {
        var url = $"api/announcements?page={page}&pageSize=10" + (classroomId is { } id ? $"&classroomId={id}" : string.Empty);
        using var response = await http.SendAsync(Request(HttpMethod.Get, url), ct);
        return await ReadAsync<PagedResult<AnnouncementDto>>(response, ct);
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken ct = default)
    {
        using var message = Request(HttpMethod.Post, "api/announcements");
        message.Content = JsonContent.Create(request);
        using var response = await http.SendAsync(message, ct);
        return await ReadAsync<AnnouncementDto>(response, ct);
    }

    public async Task PublishAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, $"api/announcements/{id}/publish"), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, $"api/announcements/{id}/acknowledge"), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<IReadOnlyList<ClassroomDto>> GetClassroomsAsync(CancellationToken ct = default)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Get, "api/classrooms"), ct);
        return await ReadAsync<IReadOnlyList<ClassroomDto>>(response, ct);
    }

    public async Task<ClassroomDto> CreateClassroomAsync(CreateClassroomRequest request, CancellationToken ct = default)
    {
        using var message = Request(HttpMethod.Post, "api/classrooms");
        message.Content = JsonContent.Create(request);
        using var response = await http.SendAsync(message, ct);
        return await ReadAsync<ClassroomDto>(response, ct);
    }

    private HttpRequestMessage Request(HttpMethod method, string url)
    {
        var message = new HttpRequestMessage(method, url);
        if (session.CurrentUser is { } user)
        {
            message.Headers.Add(DemoAuth.Header, user.Id);
        }

        return message;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        }
        catch (System.Text.Json.JsonException)
        {
            // Non-JSON error body.
        }

        throw new ApiException(problem?.Detail ?? $"Request failed ({(int)response.StatusCode}).");
    }
}

public sealed class ApiException(string message) : Exception(message);
