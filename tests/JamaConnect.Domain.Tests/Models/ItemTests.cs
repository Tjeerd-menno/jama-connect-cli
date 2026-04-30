using FluentAssertions;
using JamaConnect.Domain.Models;
using Xunit;

namespace JamaConnect.Domain.Tests.Models;

public sealed class ItemTests
{
    [Fact]
    public void Item_WhenCreated_ShouldHaveCorrectProperties()
    {
        var item = new Item
        {
            Id = 100,
            DocumentKey = "REQ-001",
            Subject = "The system shall...",
            TypeId = 1,
            ProjectId = 1,
            ParentId = null,
            Status = ItemStatus.Active
        };

        item.Id.Should().Be(100);
        item.DocumentKey.Should().Be("REQ-001");
        item.Subject.Should().Be("The system shall...");
        item.TypeId.Should().Be(1);
        item.ProjectId.Should().Be(1);
        item.ParentId.Should().BeNull();
        item.Status.Should().Be(ItemStatus.Active);
    }

    [Fact]
    public void ItemStatus_ShouldHaveExpectedValues()
    {
        Enum.GetValues<ItemStatus>().Should().BeEquivalentTo(new[]
        {
            ItemStatus.Active,
            ItemStatus.Inactive,
            ItemStatus.Draft,
            ItemStatus.Approved,
            ItemStatus.Rejected
        });
    }
}
