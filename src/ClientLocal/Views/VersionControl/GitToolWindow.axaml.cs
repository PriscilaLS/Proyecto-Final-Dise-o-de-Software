using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ClientLocal.Services.VersionControl;

namespace ClientLocal.Views.VersionControl;

public partial class GitToolWindow : Window
{
    private readonly GitService _gitService = new();
    private readonly string? _initialProjectPath;

    private TextBox? _projectPathTextBox;
    private TextBlock? _repositoryInfoTextBlock;
    private TextBox? _commitSummaryTextBox;
    private TextBox? _commitDescriptionTextBox;
    private TextBox? _outputTextBox;
    private ListBox? _changedFilesListBox;
    private TextBlock? _selectedFileTextBlock;
    private TextBox? _remoteUrlTextBox;
    private TextBlock? _remoteInfoTextBlock;
    private Button? _pushButton;
    private Button? _pullButton;

    public GitToolWindow() : this(null)
    {
    }

    public GitToolWindow(string? initialProjectPath)
    {
        _initialProjectPath = initialProjectPath;

        InitializeComponent();

        _projectPathTextBox = this.FindControl<TextBox>("ProjectPathTextBox");
        _repositoryInfoTextBlock = this.FindControl<TextBlock>("RepositoryInfoTextBlock");
        _commitSummaryTextBox = this.FindControl<TextBox>("CommitSummaryTextBox");
        _commitDescriptionTextBox = this.FindControl<TextBox>("CommitDescriptionTextBox");
        _outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        _changedFilesListBox = this.FindControl<ListBox>("ChangedFilesListBox");
        _selectedFileTextBlock = this.FindControl<TextBlock>("SelectedFileTextBlock");
        _remoteUrlTextBox = this.FindControl<TextBox>("RemoteUrlTextBox");
        _remoteInfoTextBlock = this.FindControl<TextBlock>("RemoteInfoTextBlock");
        _pushButton = this.FindControl<Button>("PushButton");
        _pullButton = this.FindControl<Button>("PullButton");

        Loaded += GitToolWindow_Loaded;
    }

    private async void GitToolWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_initialProjectPath) && Directory.Exists(_initialProjectPath))
        {
            SetProjectPath(_initialProjectPath);
            await DetectRepositoryAsync();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void PickFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selecciona la carpeta del proyecto",
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault();
        if (selected == null)
            return;

        SetProjectPath(selected.Path.LocalPath);
        await DetectRepositoryAsync();
        await RefreshChangesAsync();
    }

    private async void DetectRepositoryButton_Click(object? sender, RoutedEventArgs e)
    {
        await DetectRepositoryAsync();
    }

    private async void InitButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = GetProjectPath();
        var result = await _gitService.InitAsync(path);
        SetOutput("git init", result);
        await DetectRepositoryAsync();
    }

    private async void StatusButton_Click(object? sender, RoutedEventArgs e)
    {
        await RefreshChangesAsync(showOutput: true);
    }

    private async void DiffButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var result = await _gitService.DiffAsync(GetProjectPath());
        SetOutput("git diff -- .", result);
    }

    private async void LogButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var result = await _gitService.LogAsync(GetProjectPath());
        SetOutput("git log -10", result);
    }

    private async void CheckRemoteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        await RefreshRemoteAsync(showOutput: true);
    }

    private async void SetRemoteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var remoteUrl = _remoteUrlTextBox?.Text ?? string.Empty;
        var result = await _gitService.SetRemoteAsync(GetProjectPath(), remoteUrl);
        SetOutput("git remote origin", result);
        await RefreshRemoteAsync();
    }

    private async void PushButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var result = await _gitService.PushAsync(GetProjectPath());
        SetOutput("git push -u origin ramaActual", result);
        await RefreshRemoteAsync();
        await RefreshSyncStatusAsync();
    }

    private async void PullButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var result = await _gitService.PullAsync(GetProjectPath());
        SetOutput("git pull --ff-only", result);
        await DetectRepositoryAsync();
        await RefreshChangesAsync();
        await RefreshSyncStatusAsync();
    }

    private async void CommitButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var summary = _commitSummaryTextBox?.Text ?? string.Empty;
        var description = _commitDescriptionTextBox?.Text ?? string.Empty;
        var result = await _gitService.CommitAsync(GetProjectPath(), summary, description);
        SetOutput("git add -A + git commit", result);

        if (result.Success)
        {
            if (_commitSummaryTextBox != null)
                _commitSummaryTextBox.Text = string.Empty;

            if (_commitDescriptionTextBox != null)
                _commitDescriptionTextBox.Text = string.Empty;
        }

        await DetectRepositoryAsync();
        await RefreshChangesAsync();
    }

    private async void ChangedFilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var selectedFile = _changedFilesListBox?.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(selectedFile))
            return;

        SetSelectedFile(selectedFile);
        var result = await _gitService.DiffFileAsync(GetProjectPath(), selectedFile);

        if (result.Success && string.IsNullOrWhiteSpace(result.Output))
        {
            SetOutputText($"Archivo seleccionado: {selectedFile}\n\nEste archivo es nuevo o no tiene diff disponible todavia.");
            return;
        }

        SetOutput($"git diff -- {selectedFile}", result);
    }

    private async System.Threading.Tasks.Task DetectRepositoryAsync()
    {
        var path = GetProjectPath();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            SetRepositoryInfo("Repositorio: selecciona una carpeta válida", Brushes.Orange);
            return;
        }

        var isRepo = await _gitService.IsRepositoryAsync(path);

        if (!isRepo)
        {
            SetRepositoryInfo("Repositorio: no inicializado. Usa Git init para crear uno local.", Brushes.Orange);
            SetOutputText("La carpeta seleccionada todavía no es un repositorio Git.\n\nOperación recomendada: Git init.");
            return;
        }

        var branch = await _gitService.GetCurrentBranchAsync(path);
        SetRepositoryInfo($"Repositorio: detectado | Rama actual: {branch}", Brushes.LightGreen);
        await RefreshRemoteAsync();
    }

    private async System.Threading.Tasks.Task RefreshRemoteAsync(bool showOutput = false)
    {
        var result = await _gitService.GetRemoteAsync(GetProjectPath());

        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            SetRemoteInfo("Origin: no configurado", Brushes.Orange);
            SetPushEnabled(false);
            SetPullEnabled(false);
            SetButtonText(_pushButton, "Push");
            SetButtonText(_pullButton, "Pull");
            if (showOutput)
                SetOutput("git remote get-url origin", result);
            return;
        }

        var remote = result.Output.Trim();
        SetRemoteInfo($"Origin: {remote}", Brushes.LightGreen);
        await RefreshSyncStatusAsync();

        if (_remoteUrlTextBox != null)
            _remoteUrlTextBox.Text = remote;

        if (showOutput)
            SetOutput("git remote get-url origin", result);
    }

    private async System.Threading.Tasks.Task RefreshSyncStatusAsync()
    {
        var sync = await _gitService.GetSyncStatusAsync(GetProjectPath());

        if (!sync.HasUpstream)
        {
            SetButtonText(_pushButton, "Push");
            SetButtonText(_pullButton, "Pull");
            SetPushEnabled(true);
            SetPullEnabled(false);
            return;
        }

        SetButtonText(_pushButton, sync.Ahead > 0 ? $"Push ↑ {sync.Ahead}" : "Push");
        SetButtonText(_pullButton, sync.Behind > 0 ? $"Pull ↓ {sync.Behind}" : "Pull");
        SetPushEnabled(sync.Ahead > 0);
        SetPullEnabled(sync.Behind > 0);

        if (!string.IsNullOrWhiteSpace(sync.Message) && sync.Message != "Sincronizacion revisada.")
            SetRemoteInfo($"Origin: {sync.Message}", Brushes.Orange);
    }

    private async System.Threading.Tasks.Task RefreshChangesAsync(bool showOutput = false)
    {
        if (!await EnsureRepositoryAsync())
            return;

        var result = await _gitService.StatusAsync(GetProjectPath());

        if (!result.Success)
        {
            SetOutput("git status --short", result);
            SetChangedFiles(Array.Empty<string>());
            return;
        }

        var files = ParseChangedFiles(result.Output).ToArray();
        SetChangedFiles(files);

        if (showOutput)
            SetOutput("git status --short", result);
        else if (files.Length == 0)
            SetOutputText("No hay cambios pendientes. El proyecto esta limpio.");
    }

    private async System.Threading.Tasks.Task<bool> EnsureRepositoryAsync()
    {
        var path = GetProjectPath();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            SetRepositoryInfo("Repositorio: selecciona una carpeta válida", Brushes.Orange);
            SetOutputText("No hay una carpeta válida seleccionada.");
            return false;
        }

        if (await _gitService.IsRepositoryAsync(path))
            return true;

        SetRepositoryInfo("Repositorio: no inicializado", Brushes.Orange);
        SetOutputText("Esta carpeta no es un repositorio Git. Usa Git init antes de status, diff, log o commit.");
        return false;
    }

    private string GetProjectPath()
    {
        return _projectPathTextBox?.Text?.Trim() ?? string.Empty;
    }

    private void SetProjectPath(string path)
    {
        if (_projectPathTextBox != null)
            _projectPathTextBox.Text = path;
    }

    private void SetRepositoryInfo(string text, IBrush color)
    {
        if (_repositoryInfoTextBlock == null)
            return;

        _repositoryInfoTextBlock.Text = text;
        _repositoryInfoTextBlock.Foreground = color;
    }

    private void SetChangedFiles(IReadOnlyList<string> files)
    {
        if (_changedFilesListBox == null)
            return;

        _changedFilesListBox.ItemsSource = files;

        if (files.Count == 0)
            SetSelectedFile("Sin archivo seleccionado");
    }

    private void SetSelectedFile(string text)
    {
        if (_selectedFileTextBlock != null)
            _selectedFileTextBlock.Text = text;
    }

    private void SetRemoteInfo(string text, IBrush color)
    {
        if (_remoteInfoTextBlock == null)
            return;

        _remoteInfoTextBlock.Text = text;
        _remoteInfoTextBlock.Foreground = color;
    }

    private void SetPushEnabled(bool enabled)
    {
        if (_pushButton == null)
            return;

        _pushButton.IsEnabled = enabled;
        _pushButton.Background = enabled ? Brushes.DeepSkyBlue : Brushes.DarkSlateGray;
        _pushButton.Foreground = enabled ? Brushes.Black : Brushes.LightSlateGray;
    }

    private void SetPullEnabled(bool enabled)
    {
        if (_pullButton == null)
            return;

        _pullButton.IsEnabled = enabled;
        _pullButton.Background = enabled ? Brushes.DeepSkyBlue : Brushes.DarkSlateGray;
        _pullButton.Foreground = enabled ? Brushes.Black : Brushes.LightSlateGray;
    }

    private static void SetButtonText(Button? button, string text)
    {
        if (button != null)
            button.Content = text;
    }

    private void SetOutput(string title, GitCommandResult result)
    {
        var status = result.Success ? "OK" : "ERROR";
        SetOutputText($"[{status}] {title}\n\n{result.DisplayText}");
    }

    private void SetOutputText(string text)
    {
        if (_outputTextBox != null)
            _outputTextBox.Text = text;
    }

    private static IEnumerable<string> ParseChangedFiles(string statusOutput)
    {
        foreach (var line in statusOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length < 4)
                continue;

            var file = line[3..].Trim();
            var renameSeparator = " -> ";
            var renameIndex = file.IndexOf(renameSeparator, StringComparison.Ordinal);

            if (renameIndex >= 0)
                file = file[(renameIndex + renameSeparator.Length)..];

            if (!string.IsNullOrWhiteSpace(file))
                yield return file;
        }
    }
}
