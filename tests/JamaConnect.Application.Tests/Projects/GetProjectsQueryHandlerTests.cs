using FluentAssertions;
using JamaConnect.Application.Projects;
using Xunit;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;

namespace JamaConnect.Application.Tests.Projects;

public sealed class GetProjectsQueryHandlerTests
{
    private readonly Mock<IProjectService> _projectServiceMock = new();
    private readonly GetProjectsQueryHandler _sut;

    public GetProjectsQueryHandlerTests()
    {
        _sut = new GetProjectsQueryHandler(_projectServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenProjectsExist_ShouldReturnAllProjects()
    {
        // Arrange
        var expectedProjects = new List<Project>
        {
            new() { Id = 1, Name = "Project A", ProjectKey = "PA" },
            new() { Id = 2, Name = "Project B", ProjectKey = "PB" },
        };
        _projectServiceMock
            .Setup(x => x.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProjects.AsReadOnly());

        // Act
        var result = await _sut.HandleAsync(new GetProjectsQuery());

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedProjects);
    }

    [Fact]
    public async Task HandleAsync_WhenNoProjectsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _projectServiceMock
            .Setup(x => x.GetProjectsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>().AsReadOnly());

        // Act
        var result = await _sut.HandleAsync(new GetProjectsQuery());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _projectServiceMock
            .Setup(x => x.GetProjectsAsync(cts.Token))
            .ReturnsAsync(new List<Project>().AsReadOnly());

        // Act
        await _sut.HandleAsync(new GetProjectsQuery(), cts.Token);

        // Assert
        _projectServiceMock.Verify(x => x.GetProjectsAsync(cts.Token), Times.Once);
    }
}
