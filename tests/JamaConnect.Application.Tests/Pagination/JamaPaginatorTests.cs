using FluentAssertions;
using JamaConnect.Domain.Models;
using JamaConnect.Infrastructure.JamaConnect;
using Xunit;

namespace JamaConnect.Application.Tests.Pagination;

public sealed class JamaPaginatorTests
{
    [Fact]
    public async Task GetAllAsync_ShouldStopAtLimit()
    {
        var paginator = new JamaPaginator();

        var result = new List<int>();
        await foreach (var item in paginator.GetAllAsync(
            (startAt, maxResults, _) => Task.FromResult(new JamaPage<int>(startAt, maxResults, 2, 10, [startAt, startAt + 1])),
            2,
            3,
            CancellationToken.None))
        {
            result.Add(item);
        }

        result.Should().Equal(0, 1, 2);
    }
}
