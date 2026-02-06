using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadSettings()
    {
        DefaultTeamTextBox?.Text = SettingsManager.Instance.DefaultTeamNumber;
        RepoFolderTextBox?.Text = SettingsManager.Instance.RepositoryLocation;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

        if (DefaultTeamTextBox.Text != null)
        {
            SettingsManager.Instance.DefaultTeamNumber = DefaultTeamTextBox.Text;
        }
        
        if (RepoFolderTextBox.Text != null)
        {
            SettingsManager.Instance.RepositoryLocation = RepoFolderTextBox.Text;
        }
        
        SettingsManager.Instance.Save();

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