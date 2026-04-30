using FluentAssertions;
using JamaConnect.Domain.Models;
using Xunit;

namespace JamaConnect.Domain.Tests.Models;

public sealed class ProjectTests
{
    [Fact]
    public void Project_WhenCreated_ShouldHaveCorrectProperties()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Test Project",
            Description = "A test project",
            IsFolder = false,
            ProjectKey = "TP"
        };

        project.Id.Should().Be(1);
        project.Name.Should().Be("Test Project");
        project.Description.Should().Be("A test project");
        project.IsFolder.Should().BeFalse();
        project.ProjectKey.Should().Be("TP");
    }

    [Fact]
    public void Project_WhenCreatedAsFolder_ShouldHaveIsFolderTrue()
    {
        var folder = new Project
        {
            Id = 2,
            Name = "Folder",
            IsFolder = true
        };

        folder.IsFolder.Should().BeTrue();
        folder.Description.Should().BeNull();
    }
}
