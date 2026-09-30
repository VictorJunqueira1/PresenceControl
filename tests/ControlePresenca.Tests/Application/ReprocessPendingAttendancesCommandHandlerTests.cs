using ControlePresenca.Application.Attendances.Commands.ReprocessPendingAttendances;
using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Tests.Support;

namespace ControlePresenca.Tests.Application;

public sealed class ReprocessPendingAttendancesCommandHandlerTests
{
    [Fact]
    public async Task ShouldPreserveOriginalAttendanceAndRemoveAfterSuccess()
    {
        var registeredAt = new DateTimeOffset(2026, 9, 30, 8, 31, 12, TimeSpan.FromHours(-3));
        var attendance = CreateAttendance(registeredAt);
        var pendingStore = new InMemoryPendingAttendanceStore();
        await pendingStore.SaveAsync(attendance);

        var repository = new StubAttendanceRepository(_ => AttendancePersistenceStatus.Registered);
        var handler = new ReprocessPendingAttendancesCommandHandler(repository, pendingStore);

        var result = await handler.Handle(new ReprocessPendingAttendancesCommand());
        var remaining = await pendingStore.GetAllAsync();

        Assert.Equal(1, result.Registered);
        Assert.Equal(0, result.Remaining);
        Assert.Empty(remaining);
        Assert.NotNull(repository.LastAttendance);
        Assert.Equal(registeredAt, repository.LastAttendance!.RegisteredAt);
        Assert.Equal(AttendanceType.Entry, repository.LastAttendance.Type);
    }

    [Fact]
    public async Task ShouldRemovePendingAttendanceWhenItAlreadyExistsRemotely()
    {
        var attendance = CreateAttendance(
            new DateTimeOffset(2026, 9, 30, 8, 31, 12, TimeSpan.FromHours(-3)));
        var pendingStore = new InMemoryPendingAttendanceStore();
        await pendingStore.SaveAsync(attendance);

        var handler = new ReprocessPendingAttendancesCommandHandler(
            new StubAttendanceRepository(_ => AttendancePersistenceStatus.Duplicate),
            pendingStore);

        var result = await handler.Handle(new ReprocessPendingAttendancesCommand());
        var remaining = await pendingStore.GetAllAsync();

        Assert.Equal(1, result.AlreadyRegistered);
        Assert.Equal(0, result.Remaining);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ShouldKeepPendingAttendanceWhenInfrastructureIsUnavailable()
    {
        var attendance = CreateAttendance(
            new DateTimeOffset(2026, 9, 30, 8, 31, 12, TimeSpan.FromHours(-3)));
        var pendingStore = new InMemoryPendingAttendanceStore();
        await pendingStore.SaveAsync(attendance);

        var handler = new ReprocessPendingAttendancesCommandHandler(
            new StubAttendanceRepository(_ => AttendancePersistenceStatus.Unavailable),
            pendingStore);

        var result = await handler.Handle(new ReprocessPendingAttendancesCommand());
        var remaining = await pendingStore.GetAllAsync();

        Assert.Equal(1, result.Remaining);
        Assert.Single(remaining);
    }

    private static Attendance CreateAttendance(DateTimeOffset registeredAt)
        => new(
            "atividade-1",
            "Atividade Teste",
            "Aluno Teste",
            "123456",
            "device-1",
            AttendanceType.Entry,
            registeredAt);
}
