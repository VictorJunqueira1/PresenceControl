using ControlePresenca.Domain.Entities;

namespace ControlePresenca.Application.Contracts;

public interface IPendingAttendanceStore
{
    Task SaveAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Attendance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task RemoveAsync(Attendance attendance, CancellationToken cancellationToken = default);
}