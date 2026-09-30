using ControlePresenca.Domain.Entities;

namespace ControlePresenca.Application.Attendances.Persistence;

public interface IAttendanceRepository
{
    Task<AttendancePersistenceStatus> TryRegisterAsync(Attendance attendance, CancellationToken cancellationToken = default);
}