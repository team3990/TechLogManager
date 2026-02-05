namespace TechLogManager;

public partial class MainWindow
{
    private void ProcessDsLogs(Action action, bool amount, string destination)
    {
        Log("Processing driver station logs...");
        var files = Directory.EnumerateFiles(@"C:\Users\Public\Documents\FRC\Log Files\DSLogs");

        foreach (var file in files)
        {
            var rDest = Path.Combine(destination, "dslog");
            Utils.DirCheck(rDest);
            if (action.IsDownload()) File.Copy(file, Path.Combine(rDest, Path.GetFileName(file)));
            if (action.IsDelete()) File.Delete(file);
        }
    }
}
