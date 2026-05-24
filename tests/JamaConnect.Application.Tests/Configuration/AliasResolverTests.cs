using FluentAssertions;
using JamaConnect.Application.Configuration;
using Xunit;

namespace JamaConnect.Application.Tests.Configuration;

public sealed class AliasResolverTests
{
    [Fact]
    public void ResolveItemTypeId_WhenAliasExists_ShouldReturnConfiguredId()
    {
        var resolver = new AliasResolver(new JamaCliConfiguration
        {
            Aliases = new AliasConfiguration
            {
                ItemTypes = new Dictionary<string, ItemTypeAlias>(StringComparer.OrdinalIgnoreCase)
                {
                    ["requirement"] = new() { ItemTypeId = 102 }
                }
            }
        });

        resolver.ResolveItemTypeId("requirement").Should().Be(102);
    }

    [Fact]
    public void ResolveRelationshipTypeId_WhenNumericValue_ShouldReturnValue()
    {
        var resolver = new AliasResolver(new JamaCliConfiguration());

        resolver.ResolveRelationshipTypeId("42").Should().Be(42);
    }
}
