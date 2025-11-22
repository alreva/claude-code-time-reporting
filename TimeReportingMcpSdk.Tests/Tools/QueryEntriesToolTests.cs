using NSubstitute;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Tools;
using StrawberryShake;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for QueryEntriesTool - verifies that filter, pagination, and sorting parameters
/// are correctly passed to the GraphQL client.
///
/// Note: These tests focus on verifying BEHAVIOR (correct parameters passed to GraphQL client),
/// not implementation details. We use NSubstitute's ReceivedCalls() to inspect actual arguments.
/// </summary>
public class QueryEntriesToolTests
{
    #region Filter Tests

    [Fact]
    public async Task QueryTimeEntries_WithDateRange_PassesCorrectFiltersToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            startDate: "2025-11-05",
            endDate: "2025-11-11");

        // Assert
        var filter = mockQuery
            .ReceivedCalls()
            .FirstOrDefault()?
            .GetArguments()
            .FirstOrDefault() as TimeEntryFilterInput;

        Assert.NotNull(filter?.And);
        Assert.Equal(2, filter!.And!.Count);
        Assert.Contains(filter.And, f => f.StartDate?.Gte != null);
        Assert.Contains(filter.And, f => f.CompletionDate?.Lte != null);
    }

    [Fact]
    public async Task QueryTimeEntries_WithProjectCode_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);
        const string projectCode = "INTERNAL";

        // Act
        await tool.QueryTimeEntries(projectCode: projectCode);

        // Assert
        var actualProjectCode = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .Project?
            .Code?
            .Eq;

        Assert.Equal(projectCode, actualProjectCode);
    }

    [Fact]
    public async Task QueryTimeEntries_WithStatus_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: "NotReported");

        // Assert
        var actualStatus = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .Status?
            .Eq;

        Assert.Equal(TimeEntryStatus.NotReported, actualStatus);
    }

    [Fact]
    public async Task QueryTimeEntries_WithNoFilters_PassesNullToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries();

        // Assert
        var filter = mockQuery
            .ReceivedCalls()
            .FirstOrDefault()?
            .GetArguments()
            .FirstOrDefault() as TimeEntryFilterInput;

        Assert.Null(filter);
    }

    [Fact]
    public async Task QueryTimeEntries_WithUserEmail_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);
        const string testEmail = "test@example.com";

        // Act
        await tool.QueryTimeEntries(userEmail: testEmail);

        // Assert - Use NSubstitute's ReceivedCalls to get the actual arguments
        var actualEmail = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .UserEmail?
            .Eq;

        Assert.Equal(testEmail, actualEmail);
    }

    [Fact]
    public async Task QueryTimeEntries_WithTask_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);
        const string taskName = "Development";

        // Act
        await tool.QueryTimeEntries(task: taskName);

        // Assert
        var actualTask = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .ProjectTask?
            .TaskName?
            .Eq;

        Assert.Equal(taskName, actualTask);
    }

    [Fact]
    public async Task QueryTimeEntries_WithDescription_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);
        const string description = "authentication";

        // Act
        await tool.QueryTimeEntries(description: description);

        // Assert
        var actualDescription = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .Description?
            .Contains;

        Assert.Equal(description, actualDescription);
    }

    [Fact]
    public async Task QueryTimeEntries_WithStartDateOnly_PassesOnlyStartDateFilter()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(startDate: "2025-11-05");

        // Assert
        var actualStartDate = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .StartDate?
            .Gte;

        Assert.Equal(new DateOnly(2025, 11, 5), actualStartDate);
    }

    [Fact]
    public async Task QueryTimeEntries_WithEndDateOnly_PassesOnlyEndDateFilter()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(endDate: "2025-11-11");

        // Assert
        var actualEndDate = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .FirstOrDefault()?
            .CompletionDate?
            .Lte;

        Assert.Equal(new DateOnly(2025, 11, 11), actualEndDate);
    }

    [Fact]
    public async Task QueryTimeEntries_WithMultipleFilters_CombinesThemWithAndLogic()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            projectCode: "INTERNAL",
            startDate: "2025-11-05",
            endDate: "2025-11-11",
            status: "NotReported");

        // Assert - Verify all 4 filters are combined with AND logic
        var filterCount = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()
                .FirstOrDefault() as TimeEntryFilterInput)?
            .And?
            .Count;

        Assert.Equal(4, filterCount);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task QueryTimeEntries_WithTakeParameter_PassesPaginationToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(take: 10);

        // Assert
        var args = mockQuery
            .ReceivedCalls()
            .FirstOrDefault()?
            .GetArguments();

        Assert.Null(args?[1]);       // skip parameter
        Assert.Equal(10, args?[2]);  // take parameter
    }

    [Fact]
    public async Task QueryTimeEntries_WithOffsetPagination_PassesSkipAndTakeParametersToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(skip: 10, take: 10);

        // Assert
        var args = mockQuery
            .ReceivedCalls()
            .FirstOrDefault()?
            .GetArguments();

        Assert.Equal(10, args?[1]);  // skip parameter
        Assert.Equal(10, args?[2]);  // take parameter
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task QueryTimeEntries_WithSortByStartDateAsc_PassesSortParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(sortBy: "startDate", sortOrder: "asc");

        // Assert
        var order = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()?[3] as IReadOnlyList<TimeEntrySortInput>)?
            .FirstOrDefault()?
            .StartDate;

        Assert.Equal(SortEnumType.Asc, order);
    }

    [Fact]
    public async Task QueryTimeEntries_WithSortByCompletionDateDesc_PassesSortParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(sortBy: "completionDate", sortOrder: "desc");

        // Assert
        var order = (mockQuery
                .ReceivedCalls()
                .FirstOrDefault()?
                .GetArguments()?[3] as IReadOnlyList<TimeEntrySortInput>)?
            .FirstOrDefault()?
            .CompletionDate;

        Assert.Equal(SortEnumType.Desc, order);
    }

    #endregion

    #region Combined Tests

    [Fact]
    public async Task QueryTimeEntries_WithFiltersAndPaginationAndSorting_PassesAllParametersCorrectly()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        mockClient.QueryTimeEntries.Returns(mockQuery);
        var tool = new QueryEntriesTool(mockClient);

        // Act - Simulate "get earliest 10 entries from November"
        await tool.QueryTimeEntries(
            startDate: "2025-11-01",
            endDate: "2025-11-30",
            take: 10,
            sortBy: "startDate",
            sortOrder: "asc");

        // Assert
        var args = mockQuery
            .ReceivedCalls()
            .FirstOrDefault()?
            .GetArguments();

        var filter = args?[0] as TimeEntryFilterInput;
        var skip = args?[1];
        var take = args?[2];
        var order = (args?[3] as IReadOnlyList<TimeEntrySortInput>)?.FirstOrDefault()?.StartDate;

        Assert.Equal(2, filter?.And?.Count);        // 2 date filters
        Assert.Null(skip);                           // skip not specified
        Assert.Equal(10, take);                      // take = 10
        Assert.Equal(SortEnumType.Asc, order);      // sort by startDate asc
    }

    #endregion
}
