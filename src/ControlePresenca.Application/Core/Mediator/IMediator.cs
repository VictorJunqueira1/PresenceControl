using ControlePresenca.Application.Core.Mediator.Commands;
using ControlePresenca.Application.Core.Mediator.Queries;

namespace ControlePresenca.Application.Core.Mediator;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
