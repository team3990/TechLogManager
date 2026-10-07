using System.Globalization;
using System.Text.RegularExpressions;

namespace TechLogManager;

/// <summary>Parses dates and match info out of log file names.</summary>
public static partial class LogFileNames
{
    /// <summary>Prefix of WPILib logs that have not been renamed yet (DS never connected, or still recording).</summary>
    public const string TemporaryWpilogPrefix = "WPILIB_TBD_";

    // WPILib DataLogManager: WPILIB_yyyyMMdd_HHmmss.wpilog or WPILIB_yyyyMMdd_HHmmss_{event}_{P|Q|E}{match}.wpilog (UTC)
    [GeneratedRegex(@"^WPILIB_(?<date>\d{8}_\d{6})(?:_(?<event>.+)_(?<type>[PQE])(?<num>\d+))?\.wpilog$")]
    private static partial Regex WpilogRegex();

    // CTRE: yyyy-MM-dd_HH-mm-ss.hoot (robot local time)
    [GeneratedRegex(@"^(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})\.hoot$")]
    private static partial Regex HootRegex();

    public static bool IsTemporaryWpilog(string fileName)
    {
        return fileName.StartsWith(TemporaryWpilogPrefix, StringComparison.Ordinal);
    }

    /// <summary>Parses a WPILib log name. The date in the name is UTC.</summary>
    public static bool TryParseWpilog(string fileName, out DateTimeOffset date, out MatchInfo? match)
    {
        date = default;
        match = null;

        var m = WpilogRegex().Match(fileName);
        if (!m.Success) return false;

        if (!DateTime.TryParseExact(m.Groups["date"].Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc))
            return false;
        date = new DateTimeOffset(utc, TimeSpan.Zero);

        if (m.Groups["event"].Success)
        {
            var type = m.Groups["type"].Value switch
            {
                "P" => MatchType.Practice,
                "Q" => MatchType.Qualification,
                _ => MatchType.Elimination
            };
            match = new MatchInfo(m.Groups["event"].Value, type, int.Parse(m.Groups["num"].Value));
        }

        return true;
    }

    /// <summary>
    /// Parses a hoot log name. The date in the name is the robot's local time, so it is interpreted
    /// in <paramref name="robotTimeZone"/> (defaults to this computer's time zone).
    /// </summary>
    public static bool TryParseHoot(string fileName, out DateTimeOffset date, TimeZoneInfo? robotTimeZone = null)
    {
        date = default;

        var m = HootRegex().Match(fileName);
        if (!m.Success) return false;

        if (!DateTime.TryParseExact(m.Groups["date"].Value, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return false;

        var tz = robotTimeZone ?? TimeZoneInfo.Local;
        date = new DateTimeOffset(local, tz.GetUtcOffset(local));
        return true;
    }
}
