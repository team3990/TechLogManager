using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace TechLogManager;

public partial class SettingsWindow : Window
{
    private string? _repoFolder;
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var i = SettingsManager.Instance;
        DefaultTeamTextBox.Text = i.DefaultTeamNumber;
        RepoFolderTextBox.Text = i.RepositoryLocation;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var i = SettingsManager.Instance;
        if (DefaultTeamTextBox.Text != null) i.DefaultTeamNumber = DefaultTeamTextBox.Text;
        if (RepoFolderTextBox.Text != null) i.RepositoryLocation = RepoFolderTextBox.Text;
        i.Save();
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