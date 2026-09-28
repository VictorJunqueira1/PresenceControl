using ControlePresenca.Application.Enums;
using ControlePresenca.Domain.Entities;

namespace ControlePresenca.Application.Contracts;

public interface IAttendanceRepository
{
    Task<AttendancePersistenceStatus> TryRegisterAsync(Attendance attendance, CancellationToken cancellationToken = default);
}