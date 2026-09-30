using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Application.Core.Mediator.Commands;

namespace ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;

public sealed class ReprocessPendingAttendancesCommandHandler(
    IAttendanceRepository attendanceRepository,
    IPendingAttendanceStore pendingAttendanceStore)
    : ICommandHandler<ReprocessPendingAttendancesCommand, ReprocessPendingAttendancesResult>
{
    public async Task<ReprocessPendingAttendancesResult> Handle(
        ReprocessPendingAttendancesCommand command,
        CancellationToken cancellationToken = default)
    {
        var pendingAttendances = await pendingAttendanceStore.GetAllAsync(cancellationToken);
        var registered = 0;
        var alreadyRegistered = 0;
        var conflicts = 0;
        var remaining = 0;

        foreach (var attendance in pendingAttendances)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await attendanceRepository.TryRegisterAsync(attendance, cancellationToken);

            switch (status)
            {
                case AttendancePersistenceStatus.Registered:
                    registered++;
                    await pendingAttendanceStore.RemoveAsync(attendance, cancellationToken);
                    break;

                case AttendancePersistenceStatus.Duplicate:
                    alreadyRegistered++;
                    await pendingAttendanceStore.RemoveAsync(attendance, cancellationToken);
                    break;

                case AttendancePersistenceStatus.DeviceAlreadyUsed:
                    conflicts++;
                    await pendingAttendanceStore.RemoveAsync(attendance, cancellationToken);
                    break;

                case AttendancePersistenceStatus.Unavailable:
                    remaining++;
                    break;

                default:
                    remaining++;
                    break;
            }
        }

        return new ReprocessPendingAttendancesResult(
            pendingAttendances.Count,
            registered,
            alreadyRegistered,
            conflicts,
            remaining);
    }
}
