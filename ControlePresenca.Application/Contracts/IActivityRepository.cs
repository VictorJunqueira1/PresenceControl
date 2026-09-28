using ControlePresenca.Domain.Entities;

namespace ControlePresenca.Application.Contracts;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(string activityId, CancellationToken cancellationToken = default);
}