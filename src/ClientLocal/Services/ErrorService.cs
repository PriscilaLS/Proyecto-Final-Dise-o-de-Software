using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClientLocal.Helpers;

namespace ClientLocal.Services;

public class ErrorService
{
    private static readonly ErrorService Instance = new();

    public static ErrorService GetInstance() => Instance;

    public async void ShowError(Window parent, string title, string message)
    {
        var content = WarningDialogViewBuilder.BuildBaseContent(title, message);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var okBtn = WarningDialogViewBuilder.BuildButton("Aceptar");

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
            Content = content
        };

        okBtn.Click += (_, _) => dialog.Close();
        buttonPanel.Children.Add(okBtn);
        content.Children.Add(buttonPanel);

        if (parent != null)
            await dialog.ShowDialog(parent);
    }
}