namespace ControlePresenca.Application.Attendances.Commands.RegisterAttendance;

public sealed record RegisterAttendanceResponse(RegisterAttendanceStatus Status, DateTimeOffset? RegisteredAt = null)
{
    public bool Success => Status is RegisterAttendanceStatus.Success or RegisterAttendanceStatus.StoredForRetry;
}
