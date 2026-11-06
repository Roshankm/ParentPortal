using ParentPortal.Api.Auth;
using ParentPortal.Api.Endpoints;
using ParentPortal.Api.Realtime;
using ParentPortal.Application.Abstractions;
using ParentPortal.Application.Announcements;
using ParentPortal.Application.Classrooms;
using ParentPortal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

builder.Services.AddParentPortalAuth(builder.Configuration);
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddInfrastructure(builder.Configuration);

// Use cases (plain handler classes - no mediator library needed, see docs/adr/0003).
builder.Services.AddScoped<CreateAnnouncementHandler>();
builder.Services.AddScoped<PublishAnnouncementHandler>();
builder.Services.AddScoped<AcknowledgeAnnouncementHandler>();
builder.Services.AddScoped<ListAnnouncementsHandler>();
builder.Services.AddScoped<ListClassroomsHandler>();
builder.Services.AddScoped<CreateClassroomHandler>();

var app = builder.Build();

if (builder.Configuration.GetValue("Database:InitialiseOnStartup", true))
{
    await app.Services.InitialiseDatabaseAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAnnouncementEndpoints();
app.MapClassroomEndpoints();
app.MapDemoEndpoints();
app.MapHub<NotificationsHubEndpoint>(ParentPortal.Contracts.NotificationsHub.Path);

await app.RunAsync();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the integration tests.</summary>
public partial class Program;
