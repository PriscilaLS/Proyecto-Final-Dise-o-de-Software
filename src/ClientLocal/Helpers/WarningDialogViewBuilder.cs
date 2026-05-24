using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ClientLocal.Helpers;

public static class WarningDialogViewBuilder
{
    public static StackPanel BuildBaseContent(string title, string message, bool includeIcon = true)
    {
        var stackPanel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16
        };

        if (includeIcon)
        {
            try
            {
                var stream = AssetLoader.Open(new Uri("avares://ClientLocal/Assets/error.png"));
                stackPanel.Children.Add(new Image
                {
                    Source = new Bitmap(stream),
                    Width = 32,
                    Height = 32,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            catch {}
        }

        stackPanel.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.Parse("#f87171")),
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Color.Parse("#aaaaaa")),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Left
        });

        return stackPanel;
    }

    public static Button BuildButton(string text, double width = 100)
    {
        return new Button
        {
            Content = text,
            Width = width,
            Padding = new Thickness(12, 6),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#242424")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2a2a2a")),
            Foreground = new SolidColorBrush(Color.Parse("#dddddd"))
        };
    }
}