using NSubstitute;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Tools;
using StrawberryShake;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for pagination and sorting support in QueryEntriesTool.
/// Tests verify that pagination parameters (first, after, last, before) and sorting parameters
/// are correctly passed to the GraphQL client.
/// </summary>
public class QueryEntriesPaginationTests
{
    [Fact]
    public async Task QueryTimeEntries_WithFirstParameter_PassesPaginationToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

        mockClient.QueryTimeEntries.Returns(mockQuery);

        // CRITICAL: This is the key assertion - we expect ExecuteAsync to receive pagination parameters
        // Currently, the tool doesn't support these parameters, so this test should FAIL
        mockQuery.ExecuteAsync(
                Arg.Any<TimeEntryFilterInput?>(),
                Arg.Any<int?>(),  // first parameter
                Arg.Any<string?>(),  // after parameter
                Arg.Any<int?>(),  // last parameter
                Arg.Any<string?>(),  // before parameter
                Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),  // order parameter
                Arg.Any<CancellationToken>())
            .Returns(mockResult);

        var tool = new QueryEntriesTool(mockClient);

        // Act
        // NOTE: QueryEntriesTool doesn't have a 'first' parameter yet - this will cause a compilation error
        // That's expected for the RED phase - we're writing the test BEFORE the feature exists
        await tool.QueryTimeEntries(first: 10);

        // Assert
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
    public async Task QueryTimeEntries_WithSorting_PassesSortParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

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
        // NOTE: sortBy parameter doesn't exist yet - expected RED phase failure
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
    public async Task QueryTimeEntries_WithCursorPagination_PassesAfterParameterToGraphQLClient()
    {
        // Arrange
        var mockClient = Substitute.For<ITimeReportingClient>();
        var mockQuery = Substitute.For<IQueryTimeEntriesQuery>();
        var mockResult = CreateMockResult(new List<IQueryTimeEntries_TimeEntries_Nodes>());

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
        // NOTE: 'after' parameter doesn't exist yet - expected RED phase failure
        await tool.QueryTimeEntries(first: 10, after: "cursor123");

        // Assert
        await mockQuery.Received(1).ExecuteAsync(
            Arg.Any<TimeEntryFilterInput?>(),
            Arg.Is<int?>(f => f == 10),
            Arg.Is<string?>(a => a == "cursor123"),  // after cursor
            Arg.Is<int?>(l => l == null),
            Arg.Is<string?>(b => b == null),
            Arg.Any<IReadOnlyList<TimeEntrySortInput>?>(),
            Arg.Any<CancellationToken>());
    }

    // Helper methods
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
}
