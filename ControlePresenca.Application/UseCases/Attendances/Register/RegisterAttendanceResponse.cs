using ControlePresenca.Application.Enums;

namespace ControlePresenca.Application.UseCases.Attendances.Register;

public sealed class RegisterAttendanceResponse
{
    public RegisterAttendanceStatus Status { get; init; }
    public DateTimeOffset? RegisteredAt { get; init; }

    public bool Success => Status
        is RegisterAttendanceStatus.Success
        or RegisterAttendanceStatus.StoredForRetry;
}