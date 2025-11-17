namespace SharedKernel.Mediator.Abstractions;

public interface IMediator
{
    public ValueTask<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default);
}
