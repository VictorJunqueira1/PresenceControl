using ControlePresenca.Application.Core.Mediator.Commands;
using ControlePresenca.Application.Core.Mediator.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ControlePresenca.Application.Core.Mediator;

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)command, cancellationToken);
    }

    public async Task<TResponse> Send<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)query, cancellationToken);
    }
}
