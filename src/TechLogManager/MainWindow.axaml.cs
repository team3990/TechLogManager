using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using Raphdf201.FileUtils;
using static TechLogManager.Utils;

namespace TechLogManager;

public partial class MainWindow : Window
{
    private readonly List<LogEntry> _allLogEntries = [];
    private readonly ObservableCollection<LogEntryViewModel> _logEntries = [];
    private readonly SettingsManager _settings = SettingsManager.Load();
    private LogManager? _logManager;
    private string? _realDestFolder;

    public MainWindow()
    {
        InitializeComponent();

        SetFolderPathDate();
        RobotHostTextBox.Text = _settings.RobotHost;

        LogListBox.ItemsSource = _logEntries;
        UpdateLogCountDisplay();
        SetProgress("Progress", 0, 0);

        Closing += (_, _) => DisposeLogManager();
    }

    private void DisposeLogManager()
    {
        _logManager?.Dispose();
        _logManager = null;
    }

    private void SetFolderPathDate()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd-HH'h'mm");
        _realDestFolder = Path.Combine(_settings.RepositoryLocation, date);
        DestinationFolderText.Text = _realDestFolder.WrapPath();
        DestinationFolderTextBox.Text = date;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        DisposeLogManager();
        var settingsWindow = new SettingsWindow();
        settingsWindow.LoadSettings(_settings);
        await settingsWindow.ShowDialog(this);
        SetFolderPathDate();
        RobotHostTextBox.Text = _settings.RobotHost;
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
            _realDestFolder = Path.Combine(_settings.RepositoryLocation, folderEnd);
            DestinationFolderText.Text = _realDestFolder.WrapPath();
        }
        catch
        {
            SetFolderPathDate();
        }
    }

    private void UpdateLogCountDisplay()
    {
        LogCountTextBlock.Text = $"Visible logs: {_logEntries.Count} / {_allLogEntries.Count}";
    }

    private void SetProgress(string label, int completed, int total)
    {
        SharedProgressBar.Minimum = 0;
        SharedProgressBar.Maximum = Math.Max(1, total);
        SharedProgressBar.Value = Math.Min(completed, SharedProgressBar.Maximum);

        var percent = total == 0 ? 100 : (int)Math.Round(completed * 100.0 / total);
        SharedProgressTextBlock.Text = $"{label}: {completed}/{total} ({percent}%)";
    }

    private void RebuildVisibleLogEntries(IEnumerable<LogEntry> entries)
    {
        _logEntries.Clear();

        foreach (var entry in entries) _logEntries.Add(new LogEntryViewModel(entry));

        UpdateLogCountDisplay();
    }

    private static List<LogSource> CheckedSources(CheckBox wpilog, CheckBox hoot, CheckBox limelight)
    {
        var sources = new List<LogSource>();
        if (wpilog.IsChecked ?? false) sources.Add(LogSource.Wpilog);
        if (hoot.IsChecked ?? false) sources.Add(LogSource.Hoot);
        if (limelight.IsChecked ?? false) sources.Add(LogSource.Limelight);
        return sources;
    }

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_realDestFolder == null)
        {
            await this.ShowMessageDialog("Error", "Please enter a valid folder name");
            return;
        }

        var sources = CheckedSources(WpilogCheckbox, HootCheckbox, LimelightCheckbox);
        if (sources.Count == 0)
        {
            await this.ShowMessageDialog("Error", "Please select at least one log type!");
            return;
        }

        StartButton.IsEnabled = false;

        try
        {
            _allLogEntries.Clear();
            _logEntries.Clear();
            UpdateLogCountDisplay();

            DisposeLogManager();
            _logManager = new LogManager(RobotHostTextBox.Text ?? "", Log);

            Log($"Starting log discovery on {_logManager.Host}...");

            try
            {
                await _logManager.ConnectAsync();
            }
            catch (Exception exception)
            {
                Log(exception.Message);
                DisposeLogManager();
                await this.ShowMessageDialog("Error", exception.Message);
                return;
            }

            var result = await _logManager.ListAsync(sources);
            _allLogEntries.AddRange(result.Entries);

            RebuildVisibleLogEntries(_allLogEntries);

            Log("");
            Log($"Found {_allLogEntries.Count} log(s)!");

            if (result.Errors.Count > 0)
                await this.ShowMessageDialog("Warning", string.Join("\n", result.Errors));
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

    private async void FilterFlyout_Closed(object? sender, EventArgs e)
    {
        try
        {
            FilterButton.IsEnabled = false;
            Log("Filtering logs...");

            var sources = CheckedSources(WpilogFilterCheckbox, HootFilterCheckbox, LimelightFilterCheckbox);
            var filteredEntries = _allLogEntries.Where(entry => sources.Contains(entry.Source)).ToList();

            RebuildVisibleLogEntries(filteredEntries);

            Log($"Filtered to show {filteredEntries.Count} log(s)!");
        }
        catch (Exception ex)
        {
            Log($"Filter failed: {ex.Message}");
            await this.ShowMessageDialog("Error", $"Filter failed: {ex.Message}");
        }
        finally
        {
            FilterButton.IsEnabled = true;
        }
    }

    private async Task DownloadLog(LogEntryViewModel viewModel)
    {
        if (_realDestFolder == null || _logManager == null) return;

        try
        {
            viewModel.IsDownloading = true;
            await _logManager.DownloadAsync(viewModel.Entry, _realDestFolder);
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
        if (_logManager == null) return;

        try
        {
            viewModel.IsDeleting = true;
            await _logManager.DeleteAsync(viewModel.Entry);
            _logEntries.Remove(viewModel);
            _allLogEntries.Remove(viewModel.Entry);
            UpdateLogCountDisplay();
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
        var entries = _logEntries.ToList();
        SetProgress("Download progress", 0, entries.Count);

        var completed = 0;
        foreach (var entry in entries)
        {
            await DownloadLog(entry);
            completed++;
            SetProgress("Download progress", completed, entries.Count);
        }

        Log($"All downloads completed! ({completed}/{entries.Count})");
    }

    private async void DeleteAllButton_Click(object? sender, RoutedEventArgs e)
    {
        var entries = _logEntries.ToList();
        SetProgress("Delete progress", 0, entries.Count);

        var completed = 0;
        foreach (var entry in entries)
        {
            await DeleteLog(entry);
            completed++;
            SetProgress("Delete progress", completed, entries.Count);
        }

        Log($"All deletes completed! ({completed}/{entries.Count})");
    }

    private async void DownloadSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        var selected = _logEntries.Where(x => x.IsSelected).ToList();
        SetProgress("Download progress", 0, selected.Count);

        var completed = 0;
        foreach (var entry in selected)
        {
            await DownloadLog(entry);
            completed++;
            SetProgress("Download progress", completed, selected.Count);
        }

        Log($"Downloaded {completed}/{selected.Count} selected log(s)!");
    }

    private async void DeleteSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        var selected = _logEntries.Where(x => x.IsSelected).ToList();
        SetProgress("Delete progress", 0, selected.Count);

        var completed = 0;
        foreach (var entry in selected)
        {
            await DeleteLog(entry);
            completed++;
            SetProgress("Delete progress", completed, selected.Count);
        }

        Log($"Deleted {completed}/{selected.Count} selected log(s)!");
    }

    private void SelectAllButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var entry in _logEntries) entry.IsSelected = true;
    }

    private void DeselectAllButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var entry in _logEntries) entry.IsSelected = false;
    }

    private void DownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LogEntryViewModel viewModel }) _ = DownloadLog(viewModel);
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LogEntryViewModel viewModel }) _ = DeleteLog(viewModel);
    }
}

public class LogEntryViewModel(LogEntry entry) : ObservableObject
{
    public readonly LogEntry Entry = entry;

    public string Name => Entry.Name;

    /// <summary>"Source · local date · size · match", only with the parts that are known.</summary>
    public string Details => string.Join(" · ", new[]
    {
        Entry.Source.ToString(),
        Entry.Date?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        Entry.Size is { } size ? $"{size / 1024.0 / 1024.0:0.0} MB" : null,
        Entry.Match is { } match ? $"{match.Event} {match.Type} {match.Number}" : null
    }.Where(part => part != null));

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
