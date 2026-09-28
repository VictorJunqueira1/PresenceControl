using ControlePresenca.Application.Contracts;
using ControlePresenca.Application.Enums;
using ControlePresenca.Domain.Entities;

namespace ControlePresenca.Application.UseCases.Attendances.Register;

public sealed class RegisterAttendanceUseCase
{
    private readonly IActivityRepository _activityRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IPendingAttendanceStore _pendingAttendanceStore;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterAttendanceUseCase(
        IActivityRepository activityRepository,
        IAttendanceRepository attendanceRepository,
        IPendingAttendanceStore pendingAttendanceStore,
        IDateTimeProvider dateTimeProvider)
    {
        _activityRepository = activityRepository;
        _attendanceRepository = attendanceRepository;
        _pendingAttendanceStore = pendingAttendanceStore;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RegisterAttendanceResponse> ExecuteAsync(
        RegisterAttendanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (IsInvalid(request))
            return CreateResponse(RegisterAttendanceStatus.InvalidRequest);

        var activity = await _activityRepository.GetByIdAsync(
            request.ActivityId.Trim(),
            cancellationToken);

        if (activity is null)
            return CreateResponse(RegisterAttendanceStatus.ActivityNotFound);

        var now = _dateTimeProvider.GetNow();

        var currentDate = DateOnly.FromDateTime(now.DateTime);
        var currentTime = TimeOnly.FromDateTime(now.DateTime);

        var attendanceType = activity.GetAttendanceType(
            currentDate,
            currentTime);

        if (attendanceType is null)
            return CreateResponse(
                RegisterAttendanceStatus.AttendanceWindowClosed);

        var attendance = new Attendance(
            activity.Id,
            activity.Name,
            request.StudentName.Trim(),
            request.RA.Trim(),
            request.DeviceId.Trim(),
            attendanceType.Value,
            now);

        var persistenceStatus = await _attendanceRepository.TryRegisterAsync(
            attendance,
            cancellationToken);

        if (persistenceStatus == AttendancePersistenceStatus.Registered)
            return CreateResponse(
                RegisterAttendanceStatus.Success,
                now);

        if (persistenceStatus == AttendancePersistenceStatus.Duplicate)
            return CreateResponse(
                RegisterAttendanceStatus.AlreadyRegistered);

        if (persistenceStatus == AttendancePersistenceStatus.DeviceAlreadyUsed)
            return CreateResponse(RegisterAttendanceStatus.DeviceAlreadyUsed);

        if (!request.IsRetry)
            return CreateResponse(
                RegisterAttendanceStatus.RetryRequired);

        try
        {
            await _pendingAttendanceStore.SaveAsync(
                attendance,
                cancellationToken);

            return CreateResponse(
                RegisterAttendanceStatus.StoredForRetry,
                now);
        }
        catch
        {
            return CreateResponse(
                RegisterAttendanceStatus.Failed);
        }
    }

    private static bool IsInvalid(RegisterAttendanceRequest request)
    {
        return string.IsNullOrWhiteSpace(request.ActivityId)
            || string.IsNullOrWhiteSpace(request.StudentName)
            || string.IsNullOrWhiteSpace(request.RA)
            || string.IsNullOrWhiteSpace(request.DeviceId);
    }

    private static RegisterAttendanceResponse CreateResponse(
        RegisterAttendanceStatus status,
        DateTimeOffset? registeredAt = null)
    {
        return new RegisterAttendanceResponse
        {
            Status = status,
            RegisteredAt = registeredAt
        };
    }
}