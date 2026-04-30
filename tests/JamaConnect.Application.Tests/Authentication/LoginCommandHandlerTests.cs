using FluentAssertions;
using JamaConnect.Application.Authentication;
using Xunit;
using JamaConnect.Domain.Interfaces;
using Moq;

namespace JamaConnect.Application.Tests.Authentication;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IAuthenticationService> _authServiceMock = new();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_authServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallLoginOnAuthenticationService()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.HandleAsync(new LoginCommand());

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _authServiceMock
            .Setup(x => x.LoginAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.HandleAsync(new LoginCommand(), cts.Token);

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(cts.Token), Times.Once);
    }
}
