using ControlePresenca.Application.Activities.Models;
using ControlePresenca.Application.Core.Mediator.Queries;

namespace ControlePresenca.Application.Activities.Queries.GetActivityById;

public sealed record GetActivityByIdQuery(string ActivityId) : IQuery<ActivityModel?>;
