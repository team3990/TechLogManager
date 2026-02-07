using Raphdf201.FileUtils;

namespace TechLogManager;

public partial class MainWindow
{
    private void ProcessDsLogs(Action action, bool all, string destination)
    {
        Log("Processing driver station logs...");
        var files = Directory.EnumerateFiles(@"C:\Users\Public\Documents\FRC\Log Files\DSLogs");
        var rDest = Path.Combine(destination, "dslog");
        Utils.DirCheck(rDest);

        if (all)
        {
            foreach (var file in files)
            {
                Log($"Processing file {file}");
                if (action.IsDownload()) File.Copy(file, Path.Combine(rDest, File.GetName(file)));
                if (action.IsDelete()) File.Delete(file);
            }
        }
        else
        {
            var latestFile = files.OrderByDescending(File.GetLastWriteTime).FirstOrDefault();

            if (latestFile == null) return;
            Log($"Processing file {latestFile}");
            if (action.IsDownload()) File.Copy(latestFile, Path.Combine(rDest, File.GetName(latestFile)));
            if (action.IsDelete()) File.Delete(latestFile);
        }
    }
}
