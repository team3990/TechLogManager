using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using static TechLogManager.Utils;

namespace TechLogManager;

public partial class MainWindow : Window
{
    private string? _realDestFolder;
    private string? _selectedTeam;
    private readonly ObservableCollection<LogEntryViewModel> _logEntries = [];
    private readonly List<LogEntry> _allLogEntries = [];

    public MainWindow()
    {
        InitializeComponent();

        var i = SettingsManager.Instance;
        SetFolderPathDate();
        TeamNumberTextBox.Text = i.DefaultTeamNumber;
        
        LogListBox.ItemsSource = _logEntries;
    }

    private void SetFolderPathDate()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
        var i = SettingsManager.Instance;
        _realDestFolder = Path.Combine(i.RepositoryLocation, date);
        DestinationFolderTextBox.Text = date;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(this);
    }

    private void TeamNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var teamNumber = TeamNumberTextBox.Text?.Trim();

        _selectedTeam = ushort.TryParse(teamNumber, out _) ? teamNumber : null;
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

        try
        {
            Directory.CreateDirectory(_realDestFolder);
            var downloadRoborio = RoborioCheckbox.IsChecked ?? false;
            var downloadLimelight = LimelightCheckbox.IsChecked ?? false;
            var downloadDsLogs = DsLogsCheckbox.IsChecked ?? false;
            var downloadHoot = HootCheckbox.IsChecked ?? false;

            if (!downloadRoborio && !downloadLimelight && !downloadDsLogs && !downloadHoot)
            {
                await this.ShowMessageDialog("Error", "Please select at least one log type!");
                return;
            }

            Log("Starting log discovery...");
            Log($"Team: {_selectedTeam}");
            Log("");

            _allLogEntries.Clear();
            _logEntries.Clear();

            using var conn = new ClientManager(_selectedTeam);

            try
            {
                await conn.ConnectAsync();
            }
            catch (Exception exception)
            {
                await this.ShowMessageDialog("Error", $"Could not connect to roborio : {exception.Message}");
            }

            if (downloadLimelight)
            {
                try
                {
                    var entries = await LimelightProcessing.GetLogs(conn);
                    _allLogEntries.AddRange(entries);
                }
                catch (Exception ex)
                {
                    Log($"Error downloading Limelight logs: {ex.Message}");
                }
            }
            
            if (downloadRoborio)
            {
                try
                {
                    var entries = await RioProcessing.GetLogs(conn);
                    _allLogEntries.AddRange(entries);
                }
                catch (Exception ex)
                {
                    Log($"Error downloading RoboRIO logs: {ex.Message}");
                }
            }
            
            if (downloadDsLogs)
            {
                try
                {
                    var entries = DriverStationProcessing.GetLogs();
                    _allLogEntries.AddRange(entries);
                }
                catch (Exception ex)
                {
                    Log($"Error downloading Driver Station logs: {ex.Message}");
                }
            }
            
            if (downloadHoot)
            {
                try
                {
                    var entries = await HootProcessing.ProcessLogs(_selectedTeam, conn);
                    _allLogEntries.AddRange(entries);
                }
                catch (Exception ex)
                {
                    Log($"Error downloading Hoot logs: {ex.Message}");
                }
            }

            // Populate the UI list
            foreach (var entry in _allLogEntries)
            {
                _logEntries.Add(new LogEntryViewModel(entry, this));
            }

            Log("");
            Log($"Found {_allLogEntries.Count} log(s)!");

            CommitButton.IsEnabled = true;
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

    private async Task DownloadLog(LogEntryViewModel viewModel)
    {
        if (_realDestFolder == null) return;
        
        try
        {
            viewModel.IsDownloading = true;
            Log($"Downloading {viewModel.Name}...");
            await viewModel.Entry.ActionCallback(_realDestFolder, Action.Download);
            Log($"Downloaded {viewModel.Name}");
        }
        catch (Exception ex)
        {
            Log($"Error downloading {viewModel.Name}: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Download failed: {ex.Message}");
        }
        finally
        {
            viewModel.IsDownloading = false;
        }
    }

    private async Task DeleteLog(LogEntryViewModel viewModel)
    {
        try
        {
            viewModel.IsDeleting = true;
            Log($"Deleting {viewModel.Name}...");
            await viewModel.Entry.ActionCallback(_realDestFolder ?? "", Action.Delete);
            Log($"Deleted {viewModel.Name}");
            _logEntries.Remove(viewModel);
            _allLogEntries.Remove(viewModel.Entry);
        }
        catch (Exception ex)
        {
            Log($"Error deleting {viewModel.Name}: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Delete failed: {ex.Message}");
        }
        finally
        {
            viewModel.IsDeleting = false;
        }
    }

    private async void DownloadAllButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_realDestFolder == null) return;

        foreach (var entry in _logEntries.ToList())
        {
            await DownloadLog(entry);
        }
        
        Log("All downloads completed!");
    }

    private async void DeleteAllButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var entry in _logEntries.ToList())
        {
            await DeleteLog(entry);
        }
        
        Log("All deletes completed!");
    }

    private async void DownloadSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_realDestFolder == null) return;

        var selected = _logEntries.Where(x => x.IsSelected).ToList();
        foreach (var entry in selected)
        {
            await DownloadLog(entry);
        }
        
        Log($"Downloaded {selected.Count} selected log(s)!");
    }

    private async void DeleteSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        var selected = _logEntries.Where(x => x.IsSelected).ToList();
        foreach (var entry in selected)
        {
            await DeleteLog(entry);
        }
        
        Log($"Deleted {selected.Count} selected log(s)!");
    }

    private void SelectAllButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var entry in _logEntries)
        {
            entry.IsSelected = true;
        }
    }

    private void DeselectAllButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var entry in _logEntries)
        {
            entry.IsSelected = false;
        }
    }

    private void DownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LogEntryViewModel viewModel })
        {
            _ = DownloadLog(viewModel);
        }
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LogEntryViewModel viewModel })
        {
            _ = DeleteLog(viewModel);
        }
    }
}

public class LogEntryViewModel : ObservableObject
{
    public readonly LogEntry Entry;

    public LogEntryViewModel(LogEntry entry, MainWindow mainWindow)
    {
        Entry = entry;
    }

    public string Name => Entry.Name;
    public string Source => Entry.Source.ToString();

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsDownloading
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsDeleting
    {
        get;
        set => SetProperty(ref field, value);
    }
}
