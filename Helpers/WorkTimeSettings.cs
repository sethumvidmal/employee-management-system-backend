namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Configuration model bound from appsettings.json "WorkTime" section.
/// Defines the organisation's local timezone, which determines what "today"
/// means for calendar-based records such as attendance work dates.
/// </summary>
public class WorkTimeSettings
{
    public const string SectionName = "WorkTime";

    /// <summary>
    /// IANA timezone ID (e.g. "Asia/Colombo", "Europe/London", "UTC").
    /// Windows IDs such as "Sri Lanka Standard Time" are also accepted.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";
}
