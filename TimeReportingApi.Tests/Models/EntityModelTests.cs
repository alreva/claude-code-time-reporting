using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.Tests.Models;

/// <summary>
/// Tests for entity models to verify structure, properties, and relationships.
/// Following TDD approach: RED phase - these tests should fail until models are implemented.
/// </summary>
public class EntityModelTests
{
    [Fact]
    public void TimeEntry_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "INTERNAL",
            Task = "Development",
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        entry.Id.Should().NotBe(Guid.Empty);
        entry.ProjectCode.Should().Be("INTERNAL");
        entry.Task.Should().Be("Development");
        entry.StandardHours.Should().Be(8.0m);
        entry.OvertimeHours.Should().Be(0.0m);
        entry.StartDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        entry.CompletionDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        entry.Status.Should().Be(TimeEntryStatus.NotReported);
    }

    [Fact]
    public void TimeEntry_ShouldHaveOptionalProperties()
    {
        // Arrange & Act
        var entry = new TimeEntry
        {
            IssueId = "JIRA-123",
            Description = "Fixed bug in authentication",
            DeclineComment = "Please add more details",
            UserId = "user@example.com"
        };

        // Assert
        entry.IssueId.Should().Be("JIRA-123");
        entry.Description.Should().Be("Fixed bug in authentication");
        entry.DeclineComment.Should().Be("Please add more details");
        entry.UserId.Should().Be("user@example.com");
    }

    [Fact]
    public void TimeEntry_ShouldInitializeTagsAsEmptyList()
    {
        // Arrange & Act
        var entry = new TimeEntry();

        // Assert
        entry.Tags.Should().NotBeNull();
        entry.Tags.Should().BeEmpty();
    }

    [Fact]
    public void TimeEntry_ShouldSupportTagsCollection()
    {
        // Arrange
        var entry = new TimeEntry { Id = Guid.NewGuid() };
        var tag1 = new TimeEntryTag { TimeEntryId = entry.Id, Name = "Environment", Value = "Production" };
        var tag2 = new TimeEntryTag { TimeEntryId = entry.Id, Name = "Billable", Value = "Yes" };

        // Act
        entry.Tags.Add(tag1);
        entry.Tags.Add(tag2);

        // Assert
        entry.Tags.Should().HaveCount(2);
        entry.Tags[0].Name.Should().Be("Environment");
        entry.Tags[0].Value.Should().Be("Production");
        entry.Tags[1].Name.Should().Be("Billable");
        entry.Tags[1].Value.Should().Be("Yes");
    }

    [Fact]
    public void TimeEntryStatus_ShouldHaveAllValues()
    {
        // Act & Assert - Verify all enum values exist
        Enum.IsDefined(typeof(TimeEntryStatus), TimeEntryStatus.NotReported).Should().BeTrue();
        Enum.IsDefined(typeof(TimeEntryStatus), TimeEntryStatus.Submitted).Should().BeTrue();
        Enum.IsDefined(typeof(TimeEntryStatus), TimeEntryStatus.Approved).Should().BeTrue();
        Enum.IsDefined(typeof(TimeEntryStatus), TimeEntryStatus.Declined).Should().BeTrue();
    }

    [Fact]
    public void TimeEntryTag_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var tag = new TimeEntryTag
        {
            Id = 1,
            TimeEntryId = Guid.NewGuid(),
            Name = "Environment",
            Value = "Production"
        };

        // Assert
        tag.Id.Should().Be(1);
        tag.TimeEntryId.Should().NotBe(Guid.Empty);
        tag.Name.Should().Be("Environment");
        tag.Value.Should().Be("Production");
    }

    [Fact]
    public void TagAllowedValue_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var allowedValue = new TagAllowedValue
        {
            Id = 1,
            TagConfigurationId = 1,
            Value = "Production"
        };

        // Assert
        allowedValue.Id.Should().Be(1);
        allowedValue.TagConfigurationId.Should().Be(1);
        allowedValue.Value.Should().Be("Production");
    }

    [Fact]
    public void Project_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var project = new Project
        {
            Code = "INTERNAL",
            Name = "Internal Development",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        project.Code.Should().Be("INTERNAL");
        project.Name.Should().Be("Internal Development");
        project.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Project_ShouldInitializeIsActiveAsTrue()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        project.IsActive.Should().BeTrue("IsActive should default to true");
    }

    [Fact]
    public void Project_ShouldInitializeCollections()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        project.AvailableTasks.Should().NotBeNull();
        project.AvailableTasks.Should().BeEmpty();
        project.TagConfigurations.Should().NotBeNull();
        project.TagConfigurations.Should().BeEmpty();
        project.TimeEntries.Should().NotBeNull();
        project.TimeEntries.Should().BeEmpty();
    }

    [Fact]
    public void ProjectTask_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var task = new ProjectTask
        {
            Id = 1,
            ProjectCode = "INTERNAL",
            TaskName = "Development",
            IsActive = true
        };

        // Assert
        task.Id.Should().Be(1);
        task.ProjectCode.Should().Be("INTERNAL");
        task.TaskName.Should().Be("Development");
        task.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ProjectTask_ShouldInitializeIsActiveAsTrue()
    {
        // Arrange & Act
        var task = new ProjectTask();

        // Assert
        task.IsActive.Should().BeTrue("IsActive should default to true");
    }

    [Fact]
    public void TagConfiguration_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var tagConfig = new TagConfiguration
        {
            Id = 1,
            ProjectCode = "INTERNAL",
            TagName = "Environment",
            IsActive = true
        };

        // Assert
        tagConfig.Id.Should().Be(1);
        tagConfig.ProjectCode.Should().Be("INTERNAL");
        tagConfig.TagName.Should().Be("Environment");
        tagConfig.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TagConfiguration_ShouldInitializeIsActiveAsTrue()
    {
        // Arrange & Act
        var tagConfig = new TagConfiguration();

        // Assert
        tagConfig.IsActive.Should().BeTrue("IsActive should default to true");
    }

    [Fact]
    public void TagConfiguration_ShouldInitializeAllowedValuesAsEmptyList()
    {
        // Arrange & Act
        var tagConfig = new TagConfiguration();

        // Assert
        tagConfig.AllowedValues.Should().NotBeNull();
        tagConfig.AllowedValues.Should().BeEmpty();
    }

    [Fact]
    public void TagConfiguration_ShouldSupportAllowedValuesCollection()
    {
        // Arrange
        var tagConfig = new TagConfiguration { Id = 1 };
        tagConfig.AllowedValues.Add(new TagAllowedValue { TagConfigurationId = 1, Value = "Production" });
        tagConfig.AllowedValues.Add(new TagAllowedValue { TagConfigurationId = 1, Value = "Staging" });
        tagConfig.AllowedValues.Add(new TagAllowedValue { TagConfigurationId = 1, Value = "Development" });

        // Act & Assert
        tagConfig.AllowedValues.Should().HaveCount(3);
        tagConfig.AllowedValues.Should().Contain(v => v.Value == "Production");
        tagConfig.AllowedValues.Should().Contain(v => v.Value == "Staging");
        tagConfig.AllowedValues.Should().Contain(v => v.Value == "Development");
    }

    [Fact]
    public void TimeEntry_ShouldHaveNavigationPropertyToProject()
    {
        // Arrange & Act
        var project = new Project { Code = "INTERNAL", Name = "Internal" };
        var entry = new TimeEntry { ProjectCode = "INTERNAL", Project = project };

        // Assert
        entry.Project.Should().NotBeNull();
        entry.Project.Code.Should().Be("INTERNAL");
    }

    [Fact]
    public void Project_ShouldHaveNavigationPropertyToTasks()
    {
        // Arrange
        var project = new Project { Code = "INTERNAL" };
        var task1 = new ProjectTask { ProjectCode = "INTERNAL", TaskName = "Development" };
        var task2 = new ProjectTask { ProjectCode = "INTERNAL", TaskName = "Testing" };

        // Act
        project.AvailableTasks.Add(task1);
        project.AvailableTasks.Add(task2);

        // Assert
        project.AvailableTasks.Should().HaveCount(2);
        project.AvailableTasks.Should().Contain(t => t.TaskName == "Development");
        project.AvailableTasks.Should().Contain(t => t.TaskName == "Testing");
    }

    [Fact]
    public void Project_ShouldHaveNavigationPropertyToTagConfigurations()
    {
        // Arrange
        var project = new Project { Code = "INTERNAL" };
        var tagConfig = new TagConfiguration { ProjectCode = "INTERNAL", TagName = "Environment" };

        // Act
        project.TagConfigurations.Add(tagConfig);

        // Assert
        project.TagConfigurations.Should().HaveCount(1);
        project.TagConfigurations[0].TagName.Should().Be("Environment");
    }

    [Fact]
    public void ProjectTask_ShouldHaveNavigationPropertyToProject()
    {
        // Arrange & Act
        var project = new Project { Code = "INTERNAL", Name = "Internal" };
        var task = new ProjectTask { ProjectCode = "INTERNAL", TaskName = "Development", Project = project };

        // Assert
        task.Project.Should().NotBeNull();
        task.Project.Code.Should().Be("INTERNAL");
    }

    [Fact]
    public void TagConfiguration_ShouldHaveNavigationPropertyToProject()
    {
        // Arrange & Act
        var project = new Project { Code = "INTERNAL", Name = "Internal" };
        var tagConfig = new TagConfiguration { ProjectCode = "INTERNAL", TagName = "Environment", Project = project };

        // Assert
        tagConfig.Project.Should().NotBeNull();
        tagConfig.Project.Code.Should().Be("INTERNAL");
    }
}
