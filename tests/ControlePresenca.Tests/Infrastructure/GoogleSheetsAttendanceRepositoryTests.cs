using ControlePresenca.Application.Attendances.Persistence;
using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Infrastructure.GoogleSheets.Repositories;
using ControlePresenca.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControlePresenca.Tests.Infrastructure;

public sealed class GoogleSheetsAttendanceRepositoryTests
{
    [Fact]
    public async Task ShouldRegisterOnlyOnceWhenSameStudentArrivesConcurrently()
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);
        var registeredAt = CreateRegisteredAt();

        var tasks = Enumerable.Range(0, 30)
            .Select(index => repository.TryRegisterAsync(
                CreateAttendance("123456", $"device-{index}", registeredAt)))
            .ToArray();

        var statuses = await Task.WhenAll(tasks);

        Assert.Equal(1, statuses.Count(status => status == AttendancePersistenceStatus.Registered));
        Assert.Equal(29, statuses.Count(status => status == AttendancePersistenceStatus.Duplicate));
        Assert.Equal(1, client.DataRowCount);
    }

    [Fact]
    public async Task ShouldRegisterOnlyOnceWhenSameDeviceIsUsedByDifferentStudentsConcurrently()
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);
        var registeredAt = CreateRegisteredAt();

        var tasks = Enumerable.Range(0, 30)
            .Select(index => repository.TryRegisterAsync(
                CreateAttendance($"{100000 + index}", "same-device", registeredAt)))
            .ToArray();

        var statuses = await Task.WhenAll(tasks);

        Assert.Equal(1, statuses.Count(status => status == AttendancePersistenceStatus.Registered));
        Assert.Equal(29, statuses.Count(status => status == AttendancePersistenceStatus.DeviceAlreadyUsed));
        Assert.Equal(1, client.DataRowCount);
    }

    [Fact]
    public async Task ShouldNotDuplicateWhenRemoteAppendSucceededButResponseFailed()
    {
        var client = new InMemoryGoogleSheetsClient { FailAfterSuccessfulDataAppendOnce = true };
        var repository = CreateRepository(client);
        var attendance = CreateAttendance("123456", "device-1", CreateRegisteredAt());

        var firstResult = await repository.TryRegisterAsync(attendance);
        var retryResult = await repository.TryRegisterAsync(attendance);

        Assert.Equal(AttendancePersistenceStatus.Unavailable, firstResult);
        Assert.Equal(AttendancePersistenceStatus.Duplicate, retryResult);
        Assert.Equal(1, client.DataRowCount);
    }

    [Fact]
    public async Task ShouldCreateSheetUsingActivityNameAndRegistrationDate()
    {
        var client = new InMemoryGoogleSheetsClient();
        var repository = CreateRepository(client);
        var attendance = CreateAttendance(
            "123456",
            "device-1",
            CreateRegisteredAt());

        var result = await repository.TryRegisterAsync(attendance);

        Assert.Equal(AttendancePersistenceStatus.Registered, result);
        Assert.True(await client.SheetExistsAsync("Atividade Teste - 30-09-2026"));
    }

    private static GoogleSheetsAttendanceRepository CreateRepository(InMemoryGoogleSheetsClient client)
        => new(client, NullLogger<GoogleSheetsAttendanceRepository>.Instance);

    private static DateTimeOffset CreateRegisteredAt()
        => new(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3));

    private static Attendance CreateAttendance(
        string ra,
        string deviceId,
        DateTimeOffset registeredAt)
        => new(
            "atividade-1",
            "Atividade Teste",
            $"Aluno {ra}",
            ra,
            deviceId,
            AttendanceType.Entry,
            registeredAt);
}
