using FluentAssertions;
using JamaConnect.Application.Projects;
using Xunit;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;

namespace JamaConnect.Application.Tests.Projects;

public sealed class GetProjectsQueryHandlerTests
{
    private readonly Mock<IProjectReader> _projectReaderMock = new();
    private readonly GetProjectsQueryHandler _sut;

    public GetProjectsQueryHandlerTests()
    {
        _sut = new GetProjectsQueryHandler(_projectReaderMock.Object);
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
        _projectReaderMock
            .Setup(x => x.GetProjectsAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JamaPage<Project>(0, 50, expectedProjects.Count, expectedProjects.Count, expectedProjects.AsReadOnly()));

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
        _projectReaderMock
            .Setup(x => x.GetProjectsAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JamaPage<Project>(0, 50, 0, 0, []));

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
        _projectReaderMock
            .Setup(x => x.GetProjectsAsync(It.IsAny<PageRequest>(), cts.Token))
            .ReturnsAsync(new JamaPage<Project>(0, 50, 0, 0, []));

        // Act
        await _sut.HandleAsync(new GetProjectsQuery(), cts.Token);

        // Assert
        _projectReaderMock.Verify(x => x.GetProjectsAsync(It.IsAny<PageRequest>(), cts.Token), Times.Once);
    }
}
