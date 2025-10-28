namespace TimeReportingApi.Models;

/// <summary>
/// Defines the workflow states for time entry approval process.
/// Implements a state machine pattern for time entry lifecycle management.
/// </summary>
/// <remarks>
/// <para><strong>State Machine Flow:</strong></para>
/// <code>
/// NOT_REPORTED → SUBMITTED → APPROVED (terminal)
///                     ↓
///                 DECLINED → SUBMITTED (resubmit)
/// </code>
///
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
/// <item><description>Only NOT_REPORTED and DECLINED entries can be edited or deleted</description></item>
/// <item><description>SUBMITTED entries are read-only, awaiting approval decision</description></item>
/// <item><description>APPROVED entries are immutable and cannot be changed</description></item>
/// <item><description>DECLINED entries can be corrected and resubmitted</description></item>
/// </list>
///
/// <para><strong>Database Mapping:</strong></para>
/// <para>Values are stored in the database as uppercase with underscores (e.g., "NOT_REPORTED").</para>
/// <para>See TimeReportingDbContext for conversion logic.</para>
/// </remarks>
public enum TimeEntryStatus
{
    /// <summary>
    /// Initial state when a time entry is created.
    /// Entry is fully editable and can be deleted.
    /// </summary>
    /// <remarks>
    /// <para><strong>Allowed Actions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Update any field (hours, task, description, tags, dates)</description></item>
    /// <item><description>Delete the entry</description></item>
    /// <item><description>Submit for approval (transition to SUBMITTED)</description></item>
    /// </list>
    /// <para><strong>Database Value:</strong> "NOT_REPORTED"</para>
    /// </remarks>
    NotReported,

    /// <summary>
    /// Entry has been submitted for manager approval.
    /// Entry is read-only and awaiting approval decision.
    /// </summary>
    /// <remarks>
    /// <para><strong>Allowed Actions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Approve (admin/manager only, transition to APPROVED)</description></item>
    /// <item><description>Decline with comment (admin/manager only, transition to DECLINED)</description></item>
    /// </list>
    /// <para><strong>Restrictions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Cannot be edited by the user</description></item>
    /// <item><description>Cannot be deleted</description></item>
    /// </list>
    /// <para><strong>Database Value:</strong> "SUBMITTED"</para>
    /// </remarks>
    Submitted,

    /// <summary>
    /// Entry has been approved by a manager.
    /// This is a terminal state - entry is immutable and locked for reporting.
    /// </summary>
    /// <remarks>
    /// <para><strong>Allowed Actions:</strong> None (read-only)</para>
    /// <para><strong>Restrictions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Cannot be edited</description></item>
    /// <item><description>Cannot be deleted</description></item>
    /// <item><description>Cannot change status (terminal state)</description></item>
    /// </list>
    /// <para><strong>Business Purpose:</strong></para>
    /// <para>Approved entries are used for payroll, billing, and reporting purposes.
    /// Immutability ensures data integrity for financial and compliance requirements.</para>
    /// <para><strong>Database Value:</strong> "APPROVED"</para>
    /// </remarks>
    Approved,

    /// <summary>
    /// Entry was declined by a manager with a comment explaining why.
    /// Entry becomes editable again so user can make corrections and resubmit.
    /// </summary>
    /// <remarks>
    /// <para><strong>Allowed Actions:</strong></para>
    /// <list type="bullet">
    /// <item><description>Update fields to address decline comment</description></item>
    /// <item><description>Delete the entry if no longer needed</description></item>
    /// <item><description>Resubmit for approval (transition back to SUBMITTED)</description></item>
    /// </list>
    /// <para><strong>Related Fields:</strong></para>
    /// <para>When an entry is declined, the DeclineComment field should be populated
    /// with the manager's explanation for why the entry was rejected.</para>
    /// <para><strong>Database Value:</strong> "DECLINED"</para>
    /// </remarks>
    Declined
}
