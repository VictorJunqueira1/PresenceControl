using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Domain.ValueObjects;

namespace ControlePresenca.Tests.Domain;

public sealed class ActivityTests
{
    [Fact]
    public void ShouldCreateEntryAttendanceInsideEntryWindow()
    {
        var activity = CreateActivity();
        var registeredAt = new DateTimeOffset(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3));

        var attendance = activity.CreateAttendance(
            "  Aluno Teste  ",
            "  123456  ",
            "  device-1  ",
            registeredAt);

        Assert.NotNull(attendance);
        Assert.Equal(AttendanceType.Entry, attendance.Type);
        Assert.Equal("Aluno Teste", attendance.StudentName);
        Assert.Equal("123456", attendance.RA);
        Assert.Equal("device-1", attendance.DeviceId);
        Assert.Equal(activity.Id, attendance.ActivityId);
        Assert.Equal(activity.Name, attendance.ActivityName);
    }

    [Fact]
    public void ShouldNotCreateAttendanceOutsideAttendanceWindows()
    {
        var activity = CreateActivity();
        var registeredAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.FromHours(-3));

        var attendance = activity.CreateAttendance(
            "Aluno Teste",
            "123456",
            "device-1",
            registeredAt);

        Assert.Null(attendance);
    }

    private static Activity CreateActivity()
        => new(
            "atividade-1",
            "Atividade Teste",
            new DateOnly(2026, 9, 30),
            new AttendanceWindow(new TimeOnly(8, 0), new TimeOnly(9, 0)),
            new AttendanceWindow(new TimeOnly(17, 0), new TimeOnly(18, 0)));
}