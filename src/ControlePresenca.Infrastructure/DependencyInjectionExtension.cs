using ControlePresenca.Application.Activities.Queries;
using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Infrastructure.DateTime;
using ControlePresenca.Infrastructure.GoogleSheets;
using ControlePresenca.Infrastructure.GoogleSheets.Repositories;
using ControlePresenca.Infrastructure.Pending;
using ControlePresenca.Infrastructure.Pending.Background;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlePresenca.Infrastructure;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleSheetsOptions>(configuration.GetSection(GoogleSheetsOptions.SectionName));
        services.Configure<PendingAttendanceOptions>(configuration.GetSection(PendingAttendanceOptions.SectionName));

        services.AddSingleton<IGoogleSheetsClient, GoogleSheetsClient>();
        services.AddSingleton<GoogleSheetsActivityRepository>();
        services.AddSingleton<IActivityRepository>(provider => provider.GetRequiredService<GoogleSheetsActivityRepository>());
        services.AddSingleton<IActivityQueryRepository>(provider => provider.GetRequiredService<GoogleSheetsActivityRepository>());
        services.AddSingleton<IAttendanceRepository, GoogleSheetsAttendanceRepository>();
        services.AddSingleton<IDateTimeProvider, SaoPauloDateTimeProvider>();
        services.AddSingleton<IPendingAttendanceStore, JsonPendingAttendanceStore>();
        services.AddHostedService<PendingAttendanceBackgroundService>();

        return services;
    }
}
