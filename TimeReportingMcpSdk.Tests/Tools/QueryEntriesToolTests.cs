using NSubstitute;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Tools;
using StrawberryShake;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Integration tests for QueryEntriesTool to verify filter parameters are properly passed to GraphQL client
/// </summary>
public class QueryEntriesToolTests
{
    [Fact]
    public async Task QueryTimeEntries_WithDateRange_PassesCorrectFiltersToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            startDate: "2025-11-05",
            endDate: "2025-11-11");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 2 &&
                filter.And!.Any(f => f.StartDate != null && f.StartDate.Gte != null) &&
                filter.And!.Any(f => f.CompletionDate != null && f.CompletionDate.Lte != null)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithProjectCode_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(projectCode: "INTERNAL");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Project != null &&
                filter.And![0].Project!.Code != null &&
                filter.And![0].Project!.Code!.Eq == "INTERNAL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithStatus_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: "NotReported");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Status != null &&
                filter.And![0].Status!.Eq == TimeEntryStatus.NotReported),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithUserEmail_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(userEmail: "test@example.com");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].UserEmail != null &&
                filter.And![0].UserEmail!.Eq == "test@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithMultipleFilters_CombinesThemWithAndLogic()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            projectCode: "INTERNAL",
            startDate: "2025-11-05",
            endDate: "2025-11-11",
            status: "NotReported");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 4), // All 4 filters should be present
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithNoFilters_PassesNullToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries();

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput?>(filter => filter == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithStartDateOnly_PassesOnlyStartDateFilter()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(startDate: "2025-11-05");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].StartDate != null &&
                filter.And![0].StartDate!.Gte!.Value == new DateOnly(2025, 11, 5)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithEndDateOnly_PassesOnlyEndDateFilter()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(endDate: "2025-11-11");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].CompletionDate != null &&
                filter.And![0].CompletionDate!.Lte!.Value == new DateOnly(2025, 11, 11)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_ReturnsFormattedOutput_WhenEntriesFound()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockEntry = CreateMockEntry(
            id: Guid.NewGuid(),
            projectCode: "INTERNAL",
            projectName: "Internal Development",
            taskName: "Development",
            standardHours: 8.0m,
            startDate: new DateOnly(2025, 11, 5),
            completionDate: new DateOnly(2025, 11, 5),
            status: TimeEntryStatus.NotReported,
            userEmail: "test@example.com");
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { mockEntry });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        var result = await tool.QueryTimeEntries();

        // Assert - Should return valid JSON
        result.Should().StartWith("[");
        result.Should().EndWith("]");
        result.Should().Contain("INTERNAL");
        result.Should().Contain("Development");
        result.Should().Contain("\"standardHours\":");
        result.Should().Contain("\"task\":");
        result.Should().Contain("\"projectCode\":");
        result.Should().Contain("\"status\":");
    }

    [Fact]
    public async Task QueryTimeEntries_ReturnsEmptyJsonArray_WhenNoEntriesFound()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        var result = await tool.QueryTimeEntries();

        // Assert
        result.Should().Be("[]");
    }

    [Theory]
    [InlineData("NOT_REPORTED", TimeEntryStatus.NotReported)]
    [InlineData("SUBMITTED", TimeEntryStatus.Submitted)]
    [InlineData("APPROVED", TimeEntryStatus.Approved)]
    [InlineData("DECLINED", TimeEntryStatus.Declined)]
    public async Task QueryTimeEntries_WithStatusUnderscoreFormat_ParsesCorrectly(string statusInput, TimeEntryStatus expectedStatus)
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: statusInput);

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Status != null &&
                filter.And![0].Status!.Eq == expectedStatus),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("NotReported", TimeEntryStatus.NotReported)]
    [InlineData("Submitted", TimeEntryStatus.Submitted)]
    [InlineData("Approved", TimeEntryStatus.Approved)]
    [InlineData("Declined", TimeEntryStatus.Declined)]
    public async Task QueryTimeEntries_WithStatusPascalCaseFormat_ParsesCorrectly(string statusInput, TimeEntryStatus expectedStatus)
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: statusInput);

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Status != null &&
                filter.And![0].Status!.Eq == expectedStatus),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not_reported")]
    [InlineData("notreported")]
    [InlineData("NOTREPORTED")]
    [InlineData("NotReported")]
    public async Task QueryTimeEntries_WithStatusCaseInsensitive_ParsesCorrectly(string statusInput)
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: statusInput);

        // Assert - All should parse to NotReported
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Status != null &&
                filter.And![0].Status!.Eq == TimeEntryStatus.NotReported),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithTask_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(task: "Development");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].ProjectTask != null &&
                filter.And![0].ProjectTask!.TaskName != null &&
                filter.And![0].ProjectTask!.TaskName!.Eq == "Development"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithDescription_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(description: "authentication");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Description != null &&
                filter.And![0].Description!.Contains == "authentication"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithHasOvertimeTrue_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(hasOvertime: true);

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].OvertimeHours != null &&
                filter.And![0].OvertimeHours!.Gt == 0m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithHasOvertimeFalse_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(hasOvertime: false);

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].OvertimeHours != null &&
                filter.And![0].OvertimeHours!.Eq == 0m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithMinHours_FiltersResultsByTotalHours()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithHours(standardHours: 6m, overtimeHours: 2m); // Total: 8h
        var entry2 = CreateMockEntryWithHours(standardHours: 10m, overtimeHours: 0m); // Total: 10h
        var entry3 = CreateMockEntryWithHours(standardHours: 4m, overtimeHours: 0m); // Total: 4h

        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1, entry2, entry3 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        var result = await tool.QueryTimeEntries(minHours: 8m);

        // Assert - Should only include entries with 8h or more total
        result.Should().Contain("\"standardHours\": 6"); // entry1
        result.Should().Contain("\"standardHours\": 10"); // entry2
        result.Should().NotContain("\"standardHours\": 4"); // entry3 excluded
    }

    [Fact]
    public async Task QueryTimeEntries_WithMaxHours_FiltersResultsByTotalHours()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithHours(standardHours: 6m, overtimeHours: 2m); // Total: 8h
        var entry2 = CreateMockEntryWithHours(standardHours: 10m, overtimeHours: 0m); // Total: 10h
        var entry3 = CreateMockEntryWithHours(standardHours: 4m, overtimeHours: 0m); // Total: 4h

        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1, entry2, entry3 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        var result = await tool.QueryTimeEntries(maxHours: 8m);

        // Assert - Should only include entries with 8h or less total
        result.Should().Contain("\"standardHours\": 6"); // entry1
        result.Should().Contain("\"standardHours\": 4"); // entry3
        result.Should().NotContain("\"standardHours\": 10"); // entry2 excluded
    }

    [Fact]
    public async Task QueryTimeEntries_WithMinAndMaxHours_FiltersResultsByTotalHoursRange()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithHours(standardHours: 6m, overtimeHours: 2m); // Total: 8h
        var entry2 = CreateMockEntryWithHours(standardHours: 10m, overtimeHours: 0m); // Total: 10h
        var entry3 = CreateMockEntryWithHours(standardHours: 4m, overtimeHours: 0m); // Total: 4h

        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1, entry2, entry3 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        var result = await tool.QueryTimeEntries(minHours: 6m, maxHours: 9m);

        // Assert - Should only include entry1 (8h total)
        result.Should().Contain("\"standardHours\": 6"); // entry1 only
        result.Should().NotContain("\"standardHours\": 10"); // entry2 too high
        result.Should().NotContain("\"standardHours\": 4"); // entry3 too low
    }

    [Fact]
    public async Task QueryTimeEntries_WithTagsDictionary_FiltersResultsByTags()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithTags(new[] { ("Type", "Feature"), ("Environment", "Production") });
        var entry2 = CreateMockEntryWithTags(new[] { ("Type", "Bug"), ("Environment", "Production") });
        var entry3 = CreateMockEntryWithTags(new[] { ("Type", "Feature"), ("Environment", "Development") });

        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1, entry2, entry3 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act - Filter by Type=Feature AND Environment=Production
        var result = await tool.QueryTimeEntries(tags: "{\"Type\": \"Feature\", \"Environment\": \"Production\"}");

        // Assert - Should only include entry1
        var jsonResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(result);
        jsonResult.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task QueryTimeEntries_WithTagsArray_FiltersResultsByTags()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithTags(new[] { ("Type", "Feature") });
        var entry2 = CreateMockEntryWithTags(new[] { ("Type", "Bug") });

        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1, entry2 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act - Filter by Type=Feature using array format
        var result = await tool.QueryTimeEntries(tags: "[{\"name\": \"Type\", \"value\": \"Feature\"}]");

        // Assert - Should only include entry1
        var jsonResult = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(result);
        jsonResult.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task QueryTimeEntries_WithInvalidTags_IgnoresTagFilter()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();

        var entry1 = CreateMockEntryWithTags(new[] { ("Type", "Feature") });
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes> { entry1 });

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act - Invalid JSON should not throw, just ignore the filter
        var result = await tool.QueryTimeEntries(tags: "invalid json");

        // Assert - Should return all entries (tag filter ignored)
        result.Should().Contain("\"standardHours\":");
    }

    [Fact]
    public async Task QueryTimeEntries_WithMultipleNewFilters_CombinesThemCorrectly()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(Arg.Any<TimeEntryFilterInput?>(), Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            task: "Development",
            description: "API",
            hasOvertime: true);

        // Assert - Should have 3 GraphQL filters
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 3),
            Arg.Any<CancellationToken>());
    }

    // Helper methods to create mock objects
    private static IOperationResult<IQueryTimeEntriesResult> CreateMockResult(
        List<IQueryTimeEntries_TimeEntries_Nodes> entries)
    {
        var mockNodes = entries;
        var mockTimeEntries = Substitute.For<IQueryTimeEntries_TimeEntries>();
        mockTimeEntries.Nodes.Returns(mockNodes);

        var mockResultData = Substitute.For<IQueryTimeEntriesResult>();
        mockResultData.TimeEntries.Returns(mockTimeEntries);

        var mockResult = Substitute.For<IOperationResult<IQueryTimeEntriesResult>>();
        mockResult.Data.Returns(mockResultData);
        mockResult.Errors.Returns((IReadOnlyList<IClientError>?)null);

        return mockResult;
    }

    private static IQueryTimeEntries_TimeEntries_Nodes CreateMockEntry(
        Guid id,
        string projectCode,
        string projectName,
        string taskName,
        decimal standardHours,
        DateOnly startDate,
        DateOnly completionDate,
        TimeEntryStatus status,
        string userEmail)
    {
        var mockProject = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Project>();
        mockProject.Code.Returns(projectCode);
        mockProject.Name.Returns(projectName);

        var mockTask = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_ProjectTask>();
        mockTask.TaskName.Returns(taskName);

        var mockEntry = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes>();
        mockEntry.Id.Returns(id);
        mockEntry.Project.Returns(mockProject);
        mockEntry.ProjectTask.Returns(mockTask);
        mockEntry.StandardHours.Returns(standardHours);
        mockEntry.OvertimeHours.Returns(0m);
        mockEntry.StartDate.Returns(startDate);
        mockEntry.CompletionDate.Returns(completionDate);
        mockEntry.Status.Returns(status);
        mockEntry.UserEmail.Returns(userEmail);
        mockEntry.UserName.Returns("Test User");
        mockEntry.Description.Returns((string?)null);
        mockEntry.IssueId.Returns((string?)null);
        mockEntry.CreatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.UpdatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.Tags.Returns((IReadOnlyList<IQueryTimeEntries_TimeEntries_Nodes_Tags>?)null);

        return mockEntry;
    }

    private static IQueryTimeEntries_TimeEntries_Nodes CreateMockEntryWithHours(
        decimal standardHours,
        decimal overtimeHours)
    {
        var mockProject = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Project>();
        mockProject.Code.Returns("INTERNAL");
        mockProject.Name.Returns("Internal");

        var mockTask = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_ProjectTask>();
        mockTask.TaskName.Returns("Development");

        var mockEntry = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes>();
        mockEntry.Id.Returns(Guid.NewGuid());
        mockEntry.Project.Returns(mockProject);
        mockEntry.ProjectTask.Returns(mockTask);
        mockEntry.StandardHours.Returns(standardHours);
        mockEntry.OvertimeHours.Returns(overtimeHours);
        mockEntry.StartDate.Returns(new DateOnly(2025, 1, 1));
        mockEntry.CompletionDate.Returns(new DateOnly(2025, 1, 1));
        mockEntry.Status.Returns(TimeEntryStatus.NotReported);
        mockEntry.UserEmail.Returns("test@example.com");
        mockEntry.UserName.Returns("Test User");
        mockEntry.Description.Returns((string?)null);
        mockEntry.IssueId.Returns((string?)null);
        mockEntry.CreatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.UpdatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.Tags.Returns((IReadOnlyList<IQueryTimeEntries_TimeEntries_Nodes_Tags>?)null);

        return mockEntry;
    }

    private static IQueryTimeEntries_TimeEntries_Nodes CreateMockEntryWithTags(
        (string Name, string Value)[] tags)
    {
        var mockProject = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Project>();
        mockProject.Code.Returns("INTERNAL");
        mockProject.Name.Returns("Internal");

        var mockTask = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_ProjectTask>();
        mockTask.TaskName.Returns("Development");

        var mockTags = tags.Select(tag =>
        {
            var mockProjectTag = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Tags_TagValue_ProjectTag>();
            mockProjectTag.TagName.Returns(tag.Name);

            var mockTagValue = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Tags_TagValue>();
            mockTagValue.ProjectTag.Returns(mockProjectTag);
            mockTagValue.Value.Returns(tag.Value);

            var mockEntryTag = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes_Tags>();
            mockEntryTag.TagValue.Returns(mockTagValue);

            return mockEntryTag;
        }).ToList();

        var mockEntry = Substitute.For<IQueryTimeEntries_TimeEntries_Nodes>();
        mockEntry.Id.Returns(Guid.NewGuid());
        mockEntry.Project.Returns(mockProject);
        mockEntry.ProjectTask.Returns(mockTask);
        mockEntry.StandardHours.Returns(8m);
        mockEntry.OvertimeHours.Returns(0m);
        mockEntry.StartDate.Returns(new DateOnly(2025, 1, 1));
        mockEntry.CompletionDate.Returns(new DateOnly(2025, 1, 1));
        mockEntry.Status.Returns(TimeEntryStatus.NotReported);
        mockEntry.UserEmail.Returns("test@example.com");
        mockEntry.UserName.Returns("Test User");
        mockEntry.Description.Returns((string?)null);
        mockEntry.IssueId.Returns((string?)null);
        mockEntry.CreatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.UpdatedAt.Returns(DateTimeOffset.UtcNow);
        mockEntry.Tags.Returns(mockTags);

        return mockEntry;
    }
}
