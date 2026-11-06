using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ParentPortal.Application.Abstractions;
using ParentPortal.Infrastructure.Email;
using ParentPortal.Infrastructure.Jobs;
using ParentPortal.Infrastructure.Persistence;

namespace ParentPortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("ParentPortal") ?? "Data Source=parentportal.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IClassroomRepository, ClassroomRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.AddOptions<ReminderOptions>().Bind(configuration.GetSection(ReminderOptions.SectionName));
        services.AddSingleton<AnnouncementReminderJob>();
        if (configuration.GetValue(ReminderOptions.SectionName + ":Enabled", true))
        {
            services.AddHostedService(sp => sp.GetRequiredService<AnnouncementReminderJob>());
        }

        return services;
    }

    /// <summary>Creates the schema and demo data. Production deployments should use EF Core migrations instead.</summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DbSeeder.SeedAsync(db, clock.UtcNow, cancellationToken);
    }
}

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
