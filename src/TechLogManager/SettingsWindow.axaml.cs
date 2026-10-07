using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TechLogManager;

public partial class SettingsWindow : Window
{
    private string? _repoFolder;
    private SettingsManager? _settings;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public void LoadSettings(SettingsManager settings)
    {
        _settings = settings;
        RobotHostTextBox.Text = _settings.RobotHost;
        RepoFolderTextBox.Text = _settings.RepositoryLocation;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null) Console.WriteLine("No settings to save to");
        if (RobotHostTextBox.Text != null) _settings?.RobotHost = RobotHostTextBox.Text.Trim();
        if (RepoFolderTextBox.Text != null) _settings?.RepositoryLocation = RepoFolderTextBox.Text;
        _settings?.Save();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void BrowseRepoFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a folder",
            AllowMultiple = false
        });
        if (folders.Count <= 0) return;
        var selectedFolder = folders[0];
        _repoFolder = selectedFolder.Path.LocalPath;
        if (_repoFolder == null) return;
        RepoFolderTextBox.Text = _repoFolder;
    }
}
