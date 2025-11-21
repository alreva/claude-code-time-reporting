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
/// not implementation details. We mock only the ITimeReportingClient (the dependency we control),
/// and verify it receives the correct arguments.
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
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(
            startDate: "2025-11-05",
            endDate: "2025-11-11");

        // Assert - Verify date filters are passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 2 &&
                filter.And!.Any(f => f.StartDate != null && f.StartDate.Gte != null) &&
                filter.And!.Any(f => f.CompletionDate != null && f.CompletionDate.Lte != null)),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithProjectCode_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(projectCode: "INTERNAL");

        // Assert - Verify project code filter is passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Project != null &&
                filter.And![0].Project!.Code != null &&
                filter.And![0].Project!.Code!.Eq == "INTERNAL"),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithStatus_PassesCorrectFilterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(status: "NotReported");

        // Assert - Verify status filter is passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 1 &&
                filter.And![0].Status != null &&
                filter.And![0].Status!.Eq == TimeEntryStatus.NotReported),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithNoFilters_PassesNullToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries();

        // Assert - Verify null filter is passed when no filters specified
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput?>(filter => filter == null),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task QueryTimeEntries_WithFirstParameter_PassesPaginationToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(first: 10);

        // Assert - Verify pagination parameters are passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Any<TimeEntryFilterInput?>(),
            Arg.Is<int?>(f => f == 10),  // first = 10
            Arg.Is<string?>(a => a == null),  // after = null
            Arg.Is<int?>(l => l == null),  // last = null
            Arg.Is<string?>(b => b == null),  // before = null
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithCursorPagination_PassesAfterParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(first: 10, after: "cursor123");

        // Assert - Verify cursor pagination works
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Any<TimeEntryFilterInput?>(),
            Arg.Is<int?>(f => f == 10),
            Arg.Is<string?>(a => a == "cursor123"),  // after cursor
            Arg.Is<int?>(l => l == null),
            Arg.Is<string?>(b => b == null),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task QueryTimeEntries_WithSortByStartDateAsc_PassesSortParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(sortBy: "startDate", sortOrder: "asc");

        // Assert - Verify sorting parameter is passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Any<TimeEntryFilterInput?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Is<IReadOnlyList<TimeEntrySortInput>?>(order =>
                order != null &&
                order.Count == 1 &&
                order[0].StartDate == SortEnumType.Asc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryTimeEntries_WithSortByCompletionDateDesc_PassesSortParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        await tool.QueryTimeEntries(sortBy: "completionDate", sortOrder: "desc");

        // Assert - Verify descending sort works
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Any<TimeEntryFilterInput?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Any<int?>(),
            Arg.Any<string?>(),
            Arg.Is<IReadOnlyList<TimeEntrySortInput>?>(order =>
                order != null &&
                order.Count == 1 &&
                order[0].CompletionDate == SortEnumType.Desc),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Combined Tests

    [Fact]
    public async Task QueryTimeEntries_WithFiltersAndPaginationAndSorting_PassesAllParametersCorrectly()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateEmptyMockResult();

        mockClient.QueryTimeEntries.Returns(mockQuery);
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act - Simulate "get earliest 10 entries from November"
        await tool.QueryTimeEntries(
            startDate: "2025-11-01",
            endDate: "2025-11-30",
            first: 10,
            sortBy: "startDate",
            sortOrder: "asc");

        // Assert - Verify all parameters are passed correctly
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Is<TimeEntryFilterInput>(filter =>
                filter != null &&
                filter.And != null &&
                filter.And!.Count == 2), // date filters
            Arg.Is<int?>(f => f == 10), // first = 10
            Arg.Is<string?>(a => a == null), // no cursor
            Arg.Is<int?>(l => l == null),
            Arg.Is<string?>(b => b == null),
            Arg.Is<IReadOnlyList<TimeEntrySortInput>?>(order =>
                order != null &&
                order.Count == 1 &&
                order[0].StartDate == SortEnumType.Asc), // sort by startDate asc
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an empty mock result for tests that only verify parameters passed to GraphQL client.
    /// We're testing behavior (correct parameters), not data transformation.
    /// </summary>
    private static IOperationResult<IQueryTimeEntriesResult> CreateEmptyMockResult()
    {
        var mockTimeEntries = Substitute.For<IQueryTimeEntries_TimeEntries>();
        mockTimeEntries.Nodes.Returns(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        var mockResultData = Substitute.For<IQueryTimeEntriesResult>();
        mockResultData.TimeEntries.Returns(mockTimeEntries);

        var mockResult = Substitute.For<IOperationResult<IQueryTimeEntriesResult>>();
        mockResult.Data.Returns(mockResultData);
        mockResult.Errors.Returns((IReadOnlyList<IClientError>?)null);

        return mockResult;
    }

    #endregion
}
