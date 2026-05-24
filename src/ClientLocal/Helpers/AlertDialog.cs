using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ClientLocal.Helpers;

public class AlertDialog : Window
{
    public AlertDialog(string title, string message)
    {
        Title = title;
        Width = 430;
        Height = 210;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#181818"));

        // Grid con filas para centrar verticalmente
        var root = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto,*")
        };

        var content = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Icono (carga desde Assets)
        Image? icon = null;
        try
        {
            var stream = AssetLoader.Open(new Uri("avares://ClientLocal/Assets/error.png"));
            icon = new Image
            {
                Source = new Bitmap(stream),
                Width = 48,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
        }
        catch
        {
            icon = null;
        }

        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.Parse("#cccccc")),
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8
        };
        
        var okBtn = new Button
        {
            Width = 100,
            Background = new SolidColorBrush(Color.Parse("#242424")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2a2a2a")),
            Padding = new Thickness(6, 4),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        
        var okText = new TextBlock
        {
            Text = "Aceptar",
            Foreground = new SolidColorBrush(Color.Parse("#dddddd")),
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        okBtn.Content = okText;

        okBtn.Click += (_, _) => Close(null);

        if (icon != null)
            content.Children.Add(icon);

        content.Children.Add(messageBlock);
        buttonRow.Children.Add(okBtn);
        content.Children.Add(buttonRow);

        Grid.SetRow(content, 1);
        root.Children.Add(content);

        Content = root;
    }

    public static Task Show(Window parent, string title, string message)
    {
        var dlg = new AlertDialog(title, message);
        return dlg.ShowDialog<object?>(parent);
    }
}