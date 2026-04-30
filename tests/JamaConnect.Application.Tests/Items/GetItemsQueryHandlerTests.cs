using FluentAssertions;
using JamaConnect.Application.Items;
using Xunit;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;

namespace JamaConnect.Application.Tests.Items;

public sealed class GetItemsQueryHandlerTests
{
    private readonly Mock<IItemService> _itemServiceMock = new();
    private readonly GetItemsQueryHandler _sut;

    public GetItemsQueryHandlerTests()
    {
        _sut = new GetItemsQueryHandler(_itemServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenItemsExist_ShouldReturnProjectItems()
    {
        // Arrange
        const int projectId = 42;
        var expectedItems = new List<Item>
        {
            new() { Id = 1, DocumentKey = "REQ-001", Subject = "Requirement 1", TypeId = 1, ProjectId = projectId },
            new() { Id = 2, DocumentKey = "REQ-002", Subject = "Requirement 2", TypeId = 1, ProjectId = projectId },
        };
        _itemServiceMock
            .Setup(x => x.GetItemsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems.AsReadOnly());

        // Act
        var result = await _sut.HandleAsync(new GetItemsQuery(projectId));

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task HandleAsync_WhenNoItemsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _itemServiceMock
            .Setup(x => x.GetItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Item>().AsReadOnly());

        // Act
        var result = await _sut.HandleAsync(new GetItemsQuery(1));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldPassProjectIdToService()
    {
        // Arrange
        const int projectId = 99;
        _itemServiceMock
            .Setup(x => x.GetItemsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Item>().AsReadOnly());

        // Act
        await _sut.HandleAsync(new GetItemsQuery(projectId));

        // Assert
        _itemServiceMock.Verify(x => x.GetItemsAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
