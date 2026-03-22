using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TechLogManager;

public partial class SettingsWindow : Window
{
    private SettingsManager? _settings;
    private string? _repoFolder;

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public void LoadSettings(SettingsManager settings)
    {
        _settings = settings;
        DefaultTeamTextBox.Text = _settings.DefaultTeamNumber;
        RepoFolderTextBox.Text = _settings.RepositoryLocation;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null) Console.WriteLine("No settings to save to");
        if (DefaultTeamTextBox.Text != null) _settings?.DefaultTeamNumber = DefaultTeamTextBox.Text;
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
