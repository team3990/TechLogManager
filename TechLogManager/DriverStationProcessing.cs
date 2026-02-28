using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public static class DriverStationProcessing
{
    public static List<LogEntry> GetLogs()
    {
        Log("Processing driver station logs...");
        var files = Directory.EnumerateFiles(@"C:\Users\Public\Documents\FRC\Log Files\DSLogs")
            .OrderBy(File.GetLastWriteTime)
            .Select(it => it.Replace(".dslog", "").Replace(".dsevents", ""))
            .ToHashSet();

        return (from file in files
            let fname = file.GetFileName()!
            select new LogEntry(fname, LogSource.DriverStation, (dest, action) =>
            {
                if (action.IsDownload())
                {
                    File.Copy($"{file}.dsevents", dest.Combine("dslogs".CreateDirectory(), $"{fname}.dsevents"));
                    File.Copy($"{file}.dslog", dest.Combine("dslogs".CreateDirectory(), $"{fname}.dslog"));
                }

                if (action.IsDelete())
                {
                    File.Delete(file + ".dslog");
                    File.Delete(file + ".dsevents");
                }
                return Task.CompletedTask;
            })).OrderBy(e => e.Name).ToList();
    }
}
