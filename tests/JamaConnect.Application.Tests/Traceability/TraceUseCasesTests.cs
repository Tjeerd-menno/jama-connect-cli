using FluentAssertions;
using JamaConnect.Application.Configuration;
using JamaConnect.Application.Traceability;
using JamaConnect.Domain.Interfaces;
using JamaConnect.Domain.Models;
using Moq;
using Xunit;

namespace JamaConnect.Application.Tests.Traceability;

public sealed class TraceUseCasesTests
{
    [Fact]
    public async Task FindGapsAsync_WhenNoRulesConfigured_ShouldReturnWarning()
    {
        var sut = new TraceUseCases(
            Mock.Of<IItemReader>(),
            Mock.Of<IRelationshipReader>(),
            new JamaCliConfiguration(),
            new AliasResolver(new JamaCliConfiguration()),
            Mock.Of<IJamaPaginator>());

        var result = await sut.FindGapsAsync(1001);

        result.Summary.RulesEvaluated.Should().Be(0);
        result.Warnings.Should().Contain("No traceability rules are configured.");
    }
}
