using ControlePresenca.Domain.Enums;

namespace ControlePresenca.Domain.Entities;

public sealed class Attendance
{
    public string ActivityId { get; private set; }
    public string ActivityName { get; private set; }
    public string StudentName { get; private set; }
    public string RA { get; private set; }
    public string DeviceId { get; private set; }
    public AttendanceType Type { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    public Attendance(
        string activityId,
        string activityName,
        string studentName,
        string ra,
        string deviceId,
        AttendanceType type,
        DateTimeOffset registeredAt)
    {
        ActivityId = activityId;
        ActivityName = activityName;
        StudentName = studentName;
        RA = ra;
        DeviceId = deviceId;
        Type = type;
        RegisteredAt = registeredAt;
    }
}