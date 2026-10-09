namespace EmployeeManagement.Api.Helpers;

/// <summary>
/// Supplies the current time for the application.
///
/// Instants (check-in/check-out times, audit stamps) are always stored in UTC.
/// Calendar dates (an attendance work date) must instead be resolved in the
/// organisation's local timezone, because "today" is a local concept: at
/// 05:00 in Asia/Colombo (UTC+5:30) the UTC date is still the previous day.
/// </summary>
public class WorkClock
{
    private readonly TimeZoneInfo _workZone;

    public WorkClock(WorkTimeSettings settings, ILogger<WorkClock> logger)
    {
        _workZone = ResolveTimeZone(settings.TimeZoneId, logger);
    }

    /// <summary>
    /// The organisation's local timezone, used for all calendar-date decisions.
    /// </summary>
    public TimeZoneInfo WorkZone => _workZone;

    /// <summary>
    /// The current instant in UTC. Use this for anything stored as a timestamp.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// Today's date in the organisation's local timezone.
    /// </summary>
    public DateOnly Today() => ToWorkDate(DateTimeOffset.UtcNow);

    /// <summary>
    /// Convert an absolute instant to the calendar date it falls on locally.
    /// </summary>
    public DateOnly ToWorkDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _workZone).DateTime);

    /// <summary>
    /// Resolve a configured timezone ID, falling back to UTC rather than failing
    /// startup if the host does not recognise it.
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone(string timeZoneId, ILogger<WorkClock> logger)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            logger.LogWarning("WorkTime:TimeZoneId is not configured. Falling back to UTC");
            return TimeZoneInfo.Utc;
        }

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            logger.LogInformation("Work timezone resolved: {TimeZoneId} (UTC{Offset:+hh\\:mm;-hh\\:mm;+00\\:00})",
                timeZoneId, zone.BaseUtcOffset);
            return zone;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogError(ex, "Unknown timezone '{TimeZoneId}'. Falling back to UTC", timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }
}
