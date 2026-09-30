using ControlePresenca.Domain.Entities;
using ControlePresenca.Domain.Enums;
using ControlePresenca.Infrastructure.Pending;

namespace ControlePresenca.Tests.Infrastructure;

public sealed class JsonPendingAttendanceStoreTests
{
    [Fact]
    public async Task ShouldRestorePendingAttendancesAfterStoreRecreation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"controle-presenca-tests-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "pending-attendances.json");

        try
        {
            var attendance = new Attendance(
                "atividade-1",
                "Atividade Teste",
                "Aluno Teste",
                "123456",
                "device-1",
                AttendanceType.Entry,
                new DateTimeOffset(2026, 9, 30, 8, 30, 0, TimeSpan.FromHours(-3)));

            var firstStore = new JsonPendingAttendanceStore(filePath);
            await firstStore.SaveAsync(attendance);

            var recreatedStore = new JsonPendingAttendanceStore(filePath);
            var restored = await recreatedStore.GetAllAsync();

            Assert.Single(restored);
            Assert.Equal(attendance.ActivityId, restored.Single().ActivityId);
            Assert.Equal(attendance.RA, restored.Single().RA);
            Assert.Equal(attendance.RegisteredAt, restored.Single().RegisteredAt);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
