namespace ControlePresenca.Application.UseCases.Attendances.Register;

public sealed class RegisterAttendanceRequest
{
    public string ActivityId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RA { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsRetry { get; set; }
}