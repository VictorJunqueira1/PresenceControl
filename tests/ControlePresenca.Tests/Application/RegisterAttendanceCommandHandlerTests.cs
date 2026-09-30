using ControlePresenca.Application.Attendances.Commands.RegisterAttendance;
using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.ValueObjects;
using ControlePresenca.Tests.Support;

namespace ControlePresenca.Tests.Application;

public sealed class RegisterAttendanceCommandHandlerTests
{
    [Fact]
    public async Task ShouldStorePendingAttendanceOnFirstInfrastructureFailure()
    {
        var now = new DateTimeOffset(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3));
        var activity = CreateActivity(DateOnly.FromDateTime(now.DateTime));
        var pendingStore = new InMemoryPendingAttendanceStore();
        var handler = new RegisterAttendanceCommandHandler(
            new StubActivityRepository(activity),
            new StubAttendanceRepository(_ => AttendancePersistenceStatus.Unavailable),
            pendingStore,
            new FixedDateTimeProvider(now));

        var response = await handler.Handle(new RegisterAttendanceCommand(
            activity.Id,
            "Aluno Teste",
            "123456",
            "device-1"));

        var pending = await pendingStore.GetAllAsync();

        Assert.Equal(RegisterAttendanceStatus.StoredForRetry, response.Status);
        Assert.Single(pending);
        Assert.Equal(now, pending.Single().RegisteredAt);
    }

    [Fact]
    public async Task ShouldRequestManualRetryOnlyWhenGoogleAndLocalStoreFail()
    {
        var now = new DateTimeOffset(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3));
        var activity = CreateActivity(DateOnly.FromDateTime(now.DateTime));
        var pendingStore = new InMemoryPendingAttendanceStore { ThrowOnSave = true };
        var handler = new RegisterAttendanceCommandHandler(
            new StubActivityRepository(activity),
            new StubAttendanceRepository(_ => AttendancePersistenceStatus.Unavailable),
            pendingStore,
            new FixedDateTimeProvider(now));

        var response = await handler.Handle(new RegisterAttendanceCommand(
            activity.Id,
            "Aluno Teste",
            "123456",
            "device-1"));

        Assert.Equal(RegisterAttendanceStatus.RetryRequired, response.Status);
    }

    [Fact]
    public async Task ShouldRejectRegistrationOutsideAttendanceWindows()
    {
        var now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.FromHours(-3));
        var activity = CreateActivity(DateOnly.FromDateTime(now.DateTime));
        var handler = new RegisterAttendanceCommandHandler(
            new StubActivityRepository(activity),
            new StubAttendanceRepository(_ => AttendancePersistenceStatus.Registered),
            new InMemoryPendingAttendanceStore(),
            new FixedDateTimeProvider(now));

        var response = await handler.Handle(new RegisterAttendanceCommand(
            activity.Id,
            "Aluno Teste",
            "123456",
            "device-1"));

        Assert.Equal(RegisterAttendanceStatus.AttendanceWindowClosed, response.Status);
    }

    private static Activity CreateActivity(DateOnly date)
        => new(
            "atividade-1",
            "Atividade Teste",
            date,
            new AttendanceWindow(new TimeOnly(8, 0), new TimeOnly(9, 0)),
            new AttendanceWindow(new TimeOnly(17, 0), new TimeOnly(18, 0)));
}
