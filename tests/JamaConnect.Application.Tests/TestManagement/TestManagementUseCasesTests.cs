using System.Text.Json;
using FluentAssertions;
using JamaConnect.Application.TestManagement;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;
using Xunit;

namespace JamaConnect.Application.Tests.TestManagement;

public sealed class TestManagementUseCasesTests
{
    [Fact]
    public async Task UpdateRunStepsAsync_ShouldMergeUpdatesAndPreserveStepOrder()
    {
        var reader = new Mock<ITestManagementReader>();
        var writer = new Mock<ITestManagementWriter>();
        var raw = JsonElement.Parse("""
            {
              "id": 10,
              "testRunSteps": [
                { "status": "NOT_RUN", "actualResult": "" },
                { "status": "NOT_RUN", "actualResult": "" }
              ]
            }
            """);
        reader
            .Setup(x => x.GetTestRunAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestRun(10, null, null, null, "NOT_RUN", [], raw));
        writer
            .Setup(x => x.UpdateTestRunAsync(10, It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, JsonElement request, CancellationToken _) => request);
        var sut = new TestManagementUseCases(reader.Object, writer.Object);

        var result = await sut.UpdateRunStepsAsync(10, [new TestRunStepUpdate(1, "PASSED", "Done")]);

        var steps = result.GetProperty("testRunSteps").EnumerateArray().ToArray();
        steps[0].GetProperty("status").GetString().Should().Be("NOT_RUN");
        steps[1].GetProperty("status").GetString().Should().Be("PASSED");
        steps[1].GetProperty("actualResult").GetString().Should().Be("Done");
    }
}
