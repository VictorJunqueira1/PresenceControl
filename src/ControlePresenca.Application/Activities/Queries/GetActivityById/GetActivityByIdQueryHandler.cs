using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Core.Mediator.Queries;

namespace ControlePresenca.Application.Activities.Queries.GetActivityById;

public sealed class GetActivityByIdQueryHandler(IActivityQueryRepository repository) : IQueryHandler<GetActivityByIdQuery, ActivityModel?>
{
    public Task<ActivityModel?> Handle(
        GetActivityByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ActivityId))
            return Task.FromResult<ActivityModel?>(null);

        return repository.GetByIdAsync(query.ActivityId.Trim(), cancellationToken);
    }
}
