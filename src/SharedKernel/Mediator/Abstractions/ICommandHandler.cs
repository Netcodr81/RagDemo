namespace SharedKernel.Mediator.Abstractions;

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    ValueTask<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}