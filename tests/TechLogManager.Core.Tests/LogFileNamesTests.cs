namespace TechLogManager.Core.Tests;

public class LogFileNamesTests
{
    // The sample hoot logs were recorded in Quebec
    private static readonly TimeZoneInfo RobotTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

    // Expected values were checked against the "systemTime" entry inside each sample log
    [Theory]
    [InlineData("WPILIB_20261001_220632.wpilog", "2026-10-01T22:06:32Z")]
    [InlineData("WPILIB_20261002_001852.wpilog", "2026-10-02T00:18:52Z")]
    [InlineData("WPILIB_20261005_183841.wpilog", "2026-10-05T18:38:41Z")]
    [InlineData("WPILIB_20261005_184314.wpilog", "2026-10-05T18:43:14Z")]
    public void Wpilog_DateIsUtc(string name, string expected)
    {
        Assert.True(LogFileNames.TryParseWpilog(name, out var date, out var match));
        Assert.Equal(DateTimeOffset.Parse(expected), date);
        Assert.Equal(TimeSpan.Zero, date.Offset);
        Assert.Null(match);
    }

    [Theory]
    [InlineData("WPILIB_20270314_153000_QCMO_Q12.wpilog", "QCMO", MatchType.Qualification, 12)]
    [InlineData("WPILIB_20270314_153000_QCMO_P3.wpilog", "QCMO", MatchType.Practice, 3)]
    [InlineData("WPILIB_20270314_153000_my_event_E7.wpilog", "my_event", MatchType.Elimination, 7)]
    public void Wpilog_MatchInfo(string name, string eventName, MatchType type, int number)
    {
        Assert.True(LogFileNames.TryParseWpilog(name, out var date, out var match));
        Assert.Equal(new DateTimeOffset(2027, 3, 14, 15, 30, 0, TimeSpan.Zero), date);
        Assert.Equal(new MatchInfo(eventName, type, number), match);
    }

    [Theory]
    [InlineData("WPILIB_TBD_a1b2c3d4.wpilog")]
    [InlineData("FRC_20240314_153000.wpilog")]
    [InlineData("WPILIB_20261001_220632.hoot")]
    [InlineData("akit_24-03-14_15-30-00.wpilog")]
    public void Wpilog_InvalidNames(string name)
    {
        Assert.False(LogFileNames.TryParseWpilog(name, out _, out _));
    }

    [Fact]
    public void Wpilog_Temporary()
    {
        Assert.True(LogFileNames.IsTemporaryWpilog("WPILIB_TBD_a1b2c3d4.wpilog"));
        Assert.False(LogFileNames.IsTemporaryWpilog("WPILIB_20261001_220632.wpilog"));
    }

    // Expected values come from the UTC timestamp in each sample file's header
    [Theory]
    [InlineData("2026-10-06_19-08-23.hoot", "2026-10-06T23:08:23Z")]
    [InlineData("2026-10-06_19-14-59.hoot", "2026-10-06T23:14:59Z")]
    [InlineData("2026-10-07_13-18-45.hoot", "2026-10-07T17:18:45Z")]
    [InlineData("2026-10-07_13-19-42.hoot", "2026-10-07T17:19:42Z")]
    [InlineData("2027-01-15_10-00-00.hoot", "2027-01-15T15:00:00Z")] // winter: UTC-5
    public void Hoot_DateIsRobotLocalTime(string name, string expected)
    {
        Assert.True(LogFileNames.TryParseHoot(name, out var date, RobotTimeZone));
        Assert.Equal(DateTimeOffset.Parse(expected), date);
    }

    [Theory]
    [InlineData("2026-10-06_19-08-18")]
    [InlineData("2026-10-06_19-08-23.wpilog")]
    [InlineData("2026-13-06_19-08-23.hoot")]
    public void Hoot_InvalidNames(string name)
    {
        Assert.False(LogFileNames.TryParseHoot(name, out _, RobotTimeZone));
    }

    /// <summary>
    /// Checks every hoot file in logsSample/ (if present, it is not committed): the date parsed from the name
    /// must match the UTC Unix time stored at offset 0x48 of the header.
    /// </summary>
    [Fact]
    public void Hoot_SampleFilesMatchHeader()
    {
        var samples = FindSamplesDirectory();
        if (samples == null) return;

        var files = Directory.GetFiles(samples, "*.hoot", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var header = new byte[0x4C];
            stream.ReadExactly(header);
            var headerDate = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(header, 0x48));

            Assert.True(LogFileNames.TryParseHoot(Path.GetFileName(file), out var date, RobotTimeZone));
            Assert.Equal(headerDate, date);
        }
    }

    private static string? FindSamplesDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var samples = Path.Combine(dir.FullName, "logsSample");
            if (Directory.Exists(samples)) return samples;
        }

        return null;
    }
}
