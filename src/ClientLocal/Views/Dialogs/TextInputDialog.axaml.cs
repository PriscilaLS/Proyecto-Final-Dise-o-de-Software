using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClientLocal.Models;

namespace ClientLocal.Views;

public partial class TextInputDialog : Window
{
    private readonly bool _projectMode;

    public TextInputDialog() : this("", "", false)
    {
        
    }

    public TextInputDialog(string title, string label, bool projectMode = false, string initialValue = "")
    {
        InitializeComponent();

        _projectMode = projectMode;

        Title = title;
        Label.Text = label;
        Entry.Text = initialValue; // nuevo

        DirectoryPanel.IsVisible = projectMode;

        if (projectMode)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            DirectoryEntry.Text = Path.Combine(home, "EduIde");
        }

        AcceptBtn.Click += OnAccept;
        CancelBtn.Click += (_, _) => Close(null);

        BrowseBtn.Click += OnBrowse;
    }
    
    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Seleccionar directorio",
                AllowMultiple = false
            });

        if (folders.Count > 0)
        {
            DirectoryEntry.Text = folders[0].Path.LocalPath;
        }
    }

    private void OnAccept(object? sender, RoutedEventArgs e)
    {
        if (_projectMode)
        {
            Close(new ProjectDialogResult
            {
                Name = Entry.Text,
                Directory = DirectoryEntry.Text
            });

            return;
        }

        Close(Entry.Text);
    }
}