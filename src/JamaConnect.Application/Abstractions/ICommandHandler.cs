namespace JamaConnect.Application.Abstractions;

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
