using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace TechLogManager;

public partial class MainWindow : Window
{
    private string? _selectedTeam;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void TeamNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var teamNumber = TeamNumberTextBox.Text.Trim();

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

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedTeam))
        {
            await ShowMessageDialog("Error", "Please select a team first!");
            return;
        }

        StartButton.IsEnabled = false;
        LogOutput.Text = "";

        try
        {
            var action = GetSelectedAction();
            var downloadRoborio = RoborioCheckbox.IsChecked ?? false;
            var downloadLimelight = LimelightCheckbox.IsChecked ?? false;
            var downloadDsLogs = DsLogsCheckbox.IsChecked ?? false;
            var downloadHoot = HootCheckbox.IsChecked ?? false;

            if (!downloadRoborio && !downloadLimelight && !downloadDsLogs && !downloadHoot)
            {
                await ShowMessageDialog("Error", "Please select at least one log type!");
                return;
            }

            Log("Starting log operations...");
            Log($"Team: {_selectedTeam}");
            Log($"Action: {action}");
            Log("");

            if (downloadLimelight) await ProcessLimelightLogs(_selectedTeam, action);
            if (downloadRoborio) await ProcessRoborioLogs(_selectedTeam, action);
            if (downloadDsLogs) await ProcessDsLogs(_selectedTeam, action);
            if (downloadHoot) await ProcessHootLogs(_selectedTeam, action);

            Log("");
            Log("Operations completed!");

            CommitButton.IsEnabled = true;
            PushButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            await ShowMessageDialog("Error", $"Operation failed: {ex.Message}");
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
            GitCommit(DateTime.Now.ToLongDateString());
            Log("Git commit successful!");
        }
        catch (Exception ex)
        {
            Log($"Git commit failed: {ex.Message}");
            await ShowMessageDialog("Error", $"Git commit failed: {ex.Message}");
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
            await GitPush();
            Log("Git push successful!");
        }
        catch (Exception ex)
        {
            Log($"Git push failed: {ex.Message}");
            await ShowMessageDialog("Error", $"Git push failed: {ex.Message}");
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

    private async Task ShowMessageDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var button = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 100
        };
        button.Click += (_, _) => dialog.Close();
        stack.Children.Add(button);

        dialog.Content = stack;
        await dialog.ShowDialog(this);
    }

    public void Log(string message)
    {
        Dispatcher.UIThread.Post(() => { LogOutput.Text += message + Environment.NewLine; });
    }
}
