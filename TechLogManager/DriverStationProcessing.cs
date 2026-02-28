using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public static class DriverStationProcessing
{
    public static List<LogEntry> GetLogs()
    {
        Log("Processing driver station logs...");
        var files = Directory.EnumerateFiles(@"C:\Users\Public\Documents\FRC\Log Files\DSLogs")
            .OrderByDescending(File.GetLastWriteTime)
            .Select(it => it.Replace("..dslog", "").Replace("..dsevents", ""))
            .ToHashSet();

        return (from file in files
            let fname = file.GetFileName()!
            select new LogEntry(fname, LogSource.DriverStation, (dest, action) =>
            {
                if (action.IsDownload())
                {
                    File.Copy($"{file}..dsevents", dest.Combine("dslogs", fname).CreateDirectory());
                    File.Copy($"{file}..dslog", dest.Combine("dslogs", fname).CreateDirectory());
                }
                if (action.IsDelete()) File.Delete(file);
                return Task.CompletedTask;
            })).ToList();
    }
}
