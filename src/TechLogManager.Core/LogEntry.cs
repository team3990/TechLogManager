namespace TechLogManager;

/// <summary>A log that exists on the robot (or on one of its Limelights).</summary>
/// <param name="Source">Which kind of log this is.</param>
/// <param name="Name">Unique display name (file name for robot logs, "{limelight}_{recording}" for Limelight).</param>
/// <param name="RemotePath">Full SFTP path for robot logs, recording name for Limelight.</param>
/// <param name="Date">When the log was started, if it could be determined.</param>
/// <param name="Size">Size in bytes, if known.</param>
/// <param name="Match">Match info parsed from the file name, if the log was recorded on the FMS.</param>
/// <param name="Device">Limelight host name (Limelight only).</param>
public sealed record LogEntry(
    LogSource Source,
    string Name,
    string RemotePath,
    DateTimeOffset? Date,
    long? Size,
    MatchInfo? Match = null,
    string? Device = null);

public enum LogSource
{
    Wpilog,
    Hoot,
    Limelight
}

public enum MatchType
{
    Practice,
    Qualification,
    Elimination
}

public sealed record MatchInfo(string Event, MatchType Type, int Number);
