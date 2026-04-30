namespace JamaConnect.Domain.Interfaces;

public interface IAuthenticationService
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task LoginAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    bool IsAuthenticated { get; }
}
