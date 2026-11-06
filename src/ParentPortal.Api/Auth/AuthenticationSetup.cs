using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ParentPortal.Api.Auth;

internal static class AuthenticationSetup
{
    /// <summary>
    /// Uses Microsoft Entra ID (JWT bearer) when <c>AzureAd:ClientId</c> is configured,
    /// otherwise falls back to the demo header scheme so the project runs with zero setup.
    /// </summary>
    public static IServiceCollection AddParentPortalAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var entra = configuration.GetSection("AzureAd");
        var useEntra = !string.IsNullOrWhiteSpace(entra["ClientId"]);

        if (useEntra)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{entra["Instance"] ?? "https://login.microsoftonline.com/"}{entra["TenantId"]}/v2.0";
                    options.Audience = entra["Audience"] ?? $"api://{entra["ClientId"]}";
                    options.MapInboundClaims = true;

                    // Allow SignalR to send the token on the query string for WebSocket connections.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) &&
                                context.HttpContext.Request.Path.StartsWithSegments(Contracts.NotificationsHub.Path))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },
                    };
                });
        }
        else
        {
            services.AddAuthentication(AppClaims.DemoScheme)
                .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(AppClaims.DemoScheme, null);
        }

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddSingleton(new AuthMode(useEntra));
        return services;
    }
}

internal sealed record AuthMode(bool UsesEntraId);
