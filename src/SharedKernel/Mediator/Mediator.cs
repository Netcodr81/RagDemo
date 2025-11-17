using SharedKernel.Mediator.Abstractions;

namespace SharedKernel.Mediator;

public class Mediator(IServiceProvider provider) : IMediator
{
    public async ValueTask<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        object? handler = null;
        var requestType = request.GetType();

        if (request is ICommand<TResult>)
        {
            var commandHandlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, typeof(TResult));
            handler = provider.GetService(commandHandlerType);
        }
        else if (request is IQuery<TResult>)
        {
            var queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResult));
            handler = provider.GetService(queryHandlerType);
        }

        if (handler == null)
            throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var handleMethod = handler.GetType().GetMethod("Handle");
        if (handleMethod == null)
            throw new InvalidOperationException($"Handler for {requestType.Name} does not implement Handle method");

        var task = (ValueTask<TResult>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
        return await task;
    }

}
