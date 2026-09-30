using ControlePresenca.Application.Activities.Models;

namespace ControlePresenca.Application.Activities.Queries;

public interface IActivityQueryRepository
{
    Task<ActivityModel?> GetByIdAsync(string activityId, CancellationToken cancellationToken = default);
}
