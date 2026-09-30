using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Application.Contracts;
using ControlePresenca.Application.Core.Mediator.Commands;

namespace ControlePresenca.Application.Attendances.Commands.RegisterAttendance;

public sealed class RegisterAttendanceCommandHandler(
    IActivityRepository activityRepository,
    IAttendanceRepository attendanceRepository,
    IPendingAttendanceStore pendingAttendanceStore,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterAttendanceCommand, RegisterAttendanceResponse>
{
    public async Task<RegisterAttendanceResponse> Handle(
        RegisterAttendanceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (IsInvalid(command))
            return CreateResponse(RegisterAttendanceStatus.InvalidRequest);

        var activity = await activityRepository.GetByIdAsync(command.ActivityId.Trim(), cancellationToken);

        if (activity is null)
            return CreateResponse(RegisterAttendanceStatus.ActivityNotFound);

        var now = dateTimeProvider.GetNow();
        var attendance = activity.CreateAttendance(command.StudentName, command.RA, command.DeviceId, now);

        if (attendance is null)
            return CreateResponse(RegisterAttendanceStatus.AttendanceWindowClosed);

        var persistenceStatus = await attendanceRepository.TryRegisterAsync(attendance, cancellationToken);

        if (persistenceStatus == AttendancePersistenceStatus.Registered)
            return CreateResponse(RegisterAttendanceStatus.Success, now);

        if (persistenceStatus == AttendancePersistenceStatus.Duplicate)
            return CreateResponse(RegisterAttendanceStatus.AlreadyRegistered);

        if (persistenceStatus == AttendancePersistenceStatus.DeviceAlreadyUsed)
            return CreateResponse(RegisterAttendanceStatus.DeviceAlreadyUsed);

        try
        {
            await pendingAttendanceStore.SaveAsync(attendance, cancellationToken);
            return CreateResponse(RegisterAttendanceStatus.StoredForRetry, now);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return CreateResponse(RegisterAttendanceStatus.RetryRequired);
        }
    }

    private static bool IsInvalid(RegisterAttendanceCommand command)
        => string.IsNullOrWhiteSpace(command.ActivityId)
           || string.IsNullOrWhiteSpace(command.StudentName)
           || string.IsNullOrWhiteSpace(command.RA)
           || string.IsNullOrWhiteSpace(command.DeviceId);

    private static RegisterAttendanceResponse CreateResponse(
        RegisterAttendanceStatus status,
        DateTimeOffset? registeredAt = null)
        => new(status, registeredAt);
}