using ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;
using ControlePresenca.Application.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControlePresenca.Infrastructure.Pending.Background;

public sealed class PendingAttendanceBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PendingAttendanceOptions> options,
    ILogger<PendingAttendanceBackgroundService> logger)
    : BackgroundService
{
    private readonly TimeSpan _retryInterval = options.Value.GetRetryInterval();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ReprocessAsync(stoppingToken);

        using var timer = new PeriodicTimer(_retryInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ReprocessAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ReprocessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new ReprocessPendingAttendancesCommand(), cancellationToken);

            if (result.Total == 0)
                return;

            logger.LogInformation(
                "Reprocessamento concluído. Total: {Total}, registradas: {Registered}, já existentes: {AlreadyRegistered}, conflitos: {Conflicts}, pendentes: {Remaining}.",
                result.Total,
                result.Registered,
                result.AlreadyRegistered,
                result.Conflicts,
                result.Remaining);

            if (result.Conflicts > 0)
            {
                logger.LogWarning(
                    "{Conflicts} presença(s) pendente(s) foram descartadas por conflito de dispositivo já utilizado.",
                    result.Conflicts);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha inesperada ao reprocessar presenças pendentes.");
        }
    }
}
