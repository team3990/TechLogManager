using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public static class DriverStationProcessing
{
    public static List<LogEntry> GetLogs(ClientManager conn)
    {
        Log("Processing driver station logs...");
        var files = Directory.EnumerateFiles(@"C:\Users\Public\Documents\FRC\Log Files\DSLogs");
        files = files.OrderByDescending(File.GetLastWriteTime);

        return (from file in files
            let fname = file.GetFileName()!
            select new LogEntry(fname, LogSource.DriverStation, (dest, action) =>
            {
                var rDest = Path.Combine(dest, "dslog");
                Directory.CreateDirectory(rDest);

                if (action.IsDownload()) File.Copy(file, Path.Combine(rDest, fname));
                if (action.IsDelete()) File.Delete(file);

                return Task.CompletedTask;
            })).ToList();
    }
}
