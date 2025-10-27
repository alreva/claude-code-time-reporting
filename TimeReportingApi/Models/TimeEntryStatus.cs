namespace TimeReportingApi.Models;

/// <summary>
/// Status workflow for time entries.
/// </summary>
public enum TimeEntryStatus
{
    /// <summary>
    /// Initial state, editable. Can transition to SUBMITTED.
    /// </summary>
    NotReported,

    /// <summary>
    /// Sent for approval, read-only. Can transition to APPROVED or DECLINED.
    /// </summary>
    Submitted,

    /// <summary>
    /// Approved by manager, immutable. Terminal state.
    /// </summary>
    Approved,

    /// <summary>
    /// Rejected with comment, editable. Can transition to SUBMITTED.
    /// </summary>
    Declined
}
