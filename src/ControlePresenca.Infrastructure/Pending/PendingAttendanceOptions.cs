namespace ControlePresenca.Infrastructure.Pending;

public sealed class PendingAttendanceOptions
{
    public const string SectionName = "PendingAttendance";

    public string FilePath { get; set; } = Path.Combine("App_Data", "pending-attendances.json");
    public int RetryIntervalSeconds { get; set; } = 30;

    public TimeSpan GetRetryInterval() => TimeSpan.FromSeconds(Math.Max(5, RetryIntervalSeconds));
}
