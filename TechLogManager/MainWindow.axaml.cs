using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace TechLogManager;

public partial class MainWindow : Window
{
    private string? _realDestFolder;
    private string? _selectedTeam;

    public MainWindow()
    {
        InitializeComponent();

        var i = SettingsManager.Instance;
        SetFolderPathDate();
        TeamNumberTextBox.Text = i.DefaultTeamNumber;
    }

    private void SetFolderPathDate()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
        var i = SettingsManager.Instance;
        _realDestFolder = Path.Combine(i.RepositoryLocation, date);
        DestinationFolderText.Text = _realDestFolder;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(this);
    }

    private void TeamNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var teamNumber = TeamNumberTextBox.Text?.Trim();

        if (ushort.TryParse(teamNumber, out _))
        {
            _selectedTeam = teamNumber;
            SelectedTeamText.Text = $"Team {teamNumber} selected";
        }
        else
        {
            SelectedTeamText.Text = "No team entered";
            _selectedTeam = null;
        }
    }

    private void DestinationFolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var folderEnd = DestinationFolderTextBox.Text?.Trim();

        if (folderEnd == null
            || folderEnd.IsWhiteSpace()
            || Path.HasExtension(folderEnd)
            || Path.GetInvalidPathChars().Any(ch => folderEnd.Contains(ch))
            || Path.GetInvalidFileNameChars().Any(ch => folderEnd.Contains(ch)))
        {
            SetFolderPathDate();
            return;
        }

        try
        {
            _realDestFolder = Path.Combine(SettingsManager.Instance.RepositoryLocation, folderEnd);
            DestinationFolderText.Text = _realDestFolder;
        }
        catch
        {
            SetFolderPathDate();
        }
    }

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedTeam))
        {
            await this.ShowMessageDialog("Error", "Please select a team first!");
            return;
        }

        if (_realDestFolder == null)
        {
            await this.ShowMessageDialog("Error", "Please enter a valid folder name");
            return;
        }

        StartButton.IsEnabled = false;
        LogOutput.Text = "";

        try
        {
            Utils.DirCheck(_realDestFolder);
            var action = (Action)GetSelectedAction();
            var downloadRoborio = RoborioCheckbox.IsChecked ?? false;
            var downloadLimelight = LimelightCheckbox.IsChecked ?? false;
            var downloadDsLogs = DsLogsCheckbox.IsChecked ?? false;
            var downloadHoot = HootCheckbox.IsChecked ?? false;
            var all = AllRadio.IsChecked ?? false;

            if (!downloadRoborio && !downloadLimelight && !downloadDsLogs && !downloadHoot)
            {
                await this.ShowMessageDialog("Error", "Please select at least one log type!");
                return;
            }

            Log("Starting log operations...");
            Log($"Team: {_selectedTeam}");
            Log($"Action: {action}");
            Log("");

            if (downloadLimelight) await ProcessLimelightLogs(_selectedTeam, action, all, _realDestFolder);
            if (downloadRoborio) await ProcessRoborioLogs(_selectedTeam, action, all, _realDestFolder);
            if (downloadDsLogs) ProcessDsLogs(action, all, _realDestFolder);
            if (downloadHoot) await ProcessHootLogs(_selectedTeam, action, all, _realDestFolder);

            Log("");
            Log("Operations completed!");

            CommitButton.IsEnabled = true;
            PushButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Operation failed: {ex.Message}");
        }
        finally
        {
            StartButton.IsEnabled = true;
        }
    }

    private async void CommitButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            CommitButton.IsEnabled = false;
            Log("Committing to git...");
            GitOperations.GitCommit(SettingsManager.Instance.RepositoryLocation, DateTime.Now.ToLongDateString());
            Log("Git commit successful!");
        }
        catch (Exception ex)
        {
            Log($"Git commit failed: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Git commit failed: {ex.Message}");
        }
        finally
        {
            CommitButton.IsEnabled = true;
        }
    }

    private async void PushButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            PushButton.IsEnabled = false;
            Log("Pushing to remote...");
            await GitOperations.GitPushAsync(SettingsManager.Instance.RepositoryLocation);
            Log("Git push successful!");
        }
        catch (Exception ex)
        {
            Log($"Git push failed: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Git push failed: {ex.Message}");
        }
        finally
        {
            PushButton.IsEnabled = true;
        }
    }

    private int GetSelectedAction()
    {
        if (DlAndDelRadio.IsChecked == true) return 0; // Download and delete
        if (DownloadRadio.IsChecked == true) return 1; // Download only
        if (DeleteRadio.IsChecked == true) return 2; // Delete only
        return 0;
    }

    private void Log(string message)
    {
        Console.WriteLine(message);
        Dispatcher.UIThread.Post(() => { LogOutput.Text += message + Environment.NewLine; });
    }
}
