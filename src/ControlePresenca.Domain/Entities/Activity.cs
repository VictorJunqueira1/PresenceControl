using ControlePresenca.Domain.Enums;
using ControlePresenca.Domain.ValueObjects;

namespace ControlePresenca.Domain.Entities;

public sealed class Activity
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public DateOnly Date { get; private set; }
    public AttendanceWindow EntryWindow { get; private set; }
    public AttendanceWindow ExitWindow { get; private set; }

    public Activity(
        string id,
        string name,
        DateOnly date,
        AttendanceWindow entryWindow,
        AttendanceWindow exitWindow)
    {
        Id = id;
        Name = name;
        Date = date;
        EntryWindow = entryWindow;
        ExitWindow = exitWindow;
    }

    public Attendance? CreateAttendance(
        string studentName,
        string ra,
        string deviceId,
        DateTimeOffset registeredAt)
    {
        var attendanceType = GetAttendanceType(registeredAt);

        if (attendanceType is null)
            return null;

        return new Attendance(
            Id,
            Name,
            studentName.Trim(),
            ra.Trim(),
            deviceId.Trim(),
            attendanceType.Value,
            registeredAt);
    }

    private AttendanceType? GetAttendanceType(DateTimeOffset registeredAt)
    {
        var date = DateOnly.FromDateTime(registeredAt.DateTime);
        var time = TimeOnly.FromDateTime(registeredAt.DateTime);

        if (Date != date)
            return null;

        if (EntryWindow.Contains(time))
            return AttendanceType.Entry;

        if (ExitWindow.Contains(time))
            return AttendanceType.Exit;

        return null;
    }
}