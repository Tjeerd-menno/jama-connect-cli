using FluentAssertions;
using JamaConnect.Application.Items;
using Xunit;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;

namespace JamaConnect.Application.Tests.Items;

public sealed class GetItemsQueryHandlerTests
{
    private readonly Mock<IItemReader> _itemReaderMock = new();
    private readonly GetItemsQueryHandler _sut;

    public GetItemsQueryHandlerTests()
    {
        _sut = new GetItemsQueryHandler(_itemReaderMock.Object);
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
        _itemReaderMock
            .Setup(x => x.SearchItemsAsync(It.Is<ItemSearchCriteria>(c => c.ProjectId == projectId), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JamaPage<JamaItem>(0, 50, expectedItems.Count, expectedItems.Count, expectedItems.Select(x => new JamaItem(x.Id, x.DocumentKey, null, x.ProjectId, x.TypeId, null, x.ParentId, x.Subject, new Dictionary<string, System.Text.Json.JsonElement>(), null)).ToArray()));

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
        _itemReaderMock
            .Setup(x => x.SearchItemsAsync(It.IsAny<ItemSearchCriteria>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JamaPage<JamaItem>(0, 50, 0, 0, []));

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
        _itemReaderMock
            .Setup(x => x.SearchItemsAsync(It.Is<ItemSearchCriteria>(c => c.ProjectId == projectId), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JamaPage<JamaItem>(0, 50, 0, 0, []));

        // Act
        await _sut.HandleAsync(new GetItemsQuery(projectId));

        // Assert
        _itemReaderMock.Verify(x => x.SearchItemsAsync(It.Is<ItemSearchCriteria>(c => c.ProjectId == projectId), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
