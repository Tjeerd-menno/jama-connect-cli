using JamaConnect.Application.Abstractions;
using JamaConnect.Domain.Interfaces;

namespace JamaConnect.Application.Authentication;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand>
{
    private readonly IAuthenticationService _authenticationService;

    public LoginCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        return _authenticationService.LoginAsync(cancellationToken);
    }
}
