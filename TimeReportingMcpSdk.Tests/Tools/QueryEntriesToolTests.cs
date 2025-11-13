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

        // Assert
        result.Should().Contain("Time Entries (1)");
        result.Should().Contain("INTERNAL");
        result.Should().Contain("Development");
        result.Should().Contain("8");
        result.Should().Contain("test");
        // Verify table structure
        result.Should().Contain("| Date");
        result.Should().Contain("| Project");
        result.Should().Contain("| Task");
        result.Should().Contain("| Hours");
        result.Should().Contain("| Status");
        result.Should().Contain("| Tags");
        result.Should().Contain("| User");
    }

    [Fact]
    public async Task QueryTimeEntries_ReturnsNoEntriesMessage_WhenNoEntriesFound()
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
        result.Should().Be("No time entries found matching the criteria.");
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
        mockEntry.Description.Returns((string?)null);

        return mockEntry;
    }
}
