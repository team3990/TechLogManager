using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TechLogManager;

public static class Utils
{
    public static List<string>? ParseJsonStringList(string json)
    {
        return JsonSerializer.Deserialize<List<string>>(json);
    }

    internal static string GetRioHostname(string teamNumber)
    {
        return $"roboRIO-{teamNumber}-FRC.local";
    }

    public static async Task ShowMessageDialog(this Window window, string title, string message)
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
        await dialog.ShowDialog(window);
    }

    extension(Action action)
    {
        internal bool IsDownload()
        {
            return action is Action.Download or Action.DownloadAndDelete;
        }

        internal bool IsDelete()
        {
            return action is Action.Delete or Action.DownloadAndDelete;
        }
    }
}

internal enum Action
{
    DownloadAndDelete,
    Download,
    Delete
}
