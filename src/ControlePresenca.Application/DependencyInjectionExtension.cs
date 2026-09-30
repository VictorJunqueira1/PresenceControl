using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Activities.Queries.GetActivityById;
using ControlePresenca.Application.Attendances.Commands.RegisterAttendance;
using ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;
using ControlePresenca.Application.Core.Mediator;
using ControlePresenca.Application.Core.Mediator.Commands;
using ControlePresenca.Application.Core.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ControlePresenca.Application;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IQueryHandler<GetActivityByIdQuery, ActivityModel?>, GetActivityByIdQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterAttendanceCommand, RegisterAttendanceResponse>, RegisterAttendanceCommandHandler>();
        services.AddScoped<ICommandHandler<ReprocessPendingAttendancesCommand, ReprocessPendingAttendancesResult>, ReprocessPendingAttendancesCommandHandler>();

        return services;
    }
}
