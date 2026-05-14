using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClientLocal.Services;
using ClientLocal.Helpers;

namespace ClientLocal.Views;

public partial class MainWindow : Window
{
    private double _explorerLastWidth = 210;
    private double _consoleLastHeight = 180;

    private bool _explorerOpen = true;
    private bool _consoleOpen = true;

    private RowDefinition ConsoleRow => RootGrid.RowDefinitions[2];
    private RowDefinition VGapRow => RootGrid.RowDefinitions[1];
    private ColumnDefinition ExplorerCol => TopGrid.ColumnDefinitions[0];
    private ColumnDefinition HGapCol => TopGrid.ColumnDefinitions[1];

    private readonly IProjectService _projectService = new ProjectService();
    private readonly IIntegrityService _integrityService = FileIntegrityService.Instance;
    private readonly IExecutionService _executionService = new PythonExecutionService();
    private readonly ClipboardGuard _clipboardGuard;

    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        NewProjectMenu.Click += OnNewProject;
        NewScriptMenu.Click += OnNewScript;
        NewFolderMenu.Click += OnNewFolder;
        TreeExplorer.SelectionChanged += OnFileSelected;
        SaveScriptMenu.Click += OnSaveScript;
        RunScriptMenu.Click += OnRunScript;

        CloseExplorerBtn.Click += (_, _) => ToggleExplorer();
        CloseConsoleBtn.Click += (_, _) => ToggleConsole();

        ToggleExplorerMenu.Click += (_, _) => ToggleExplorer();
        ToggleConsoleMenu.Click += (_, _) => ToggleConsole();

        _clipboardGuard = new ClipboardGuard(TextEditor);
    }

    // ── Explorador ──────────────────────────────────────────────────────────

    private void ToggleExplorer()
    {
        if (_explorerOpen) CollapseExplorer();
        else ExpandExplorer();
    }

    private void CollapseExplorer()
    {
        if (ExplorerCol.Width.Value > 10)
            _explorerLastWidth = ExplorerCol.Width.Value;

        ExplorerPanel.IsVisible = false;
        HSplitter.IsVisible = false;

        ExplorerCol.MinWidth = 0;
        HGapCol.MinWidth = 0;
        ExplorerCol.Width = new GridLength(0);
        HGapCol.Width = new GridLength(0);

        _explorerOpen = false;
        ToggleExplorerMenu.Header = "Mostrar Explorador";
    }

    private void ExpandExplorer()
    {
        ExplorerPanel.IsVisible = true;
        HSplitter.IsVisible = true;

        ExplorerCol.MinWidth = 120;
        ExplorerCol.Width = new GridLength(_explorerLastWidth);
        HGapCol.Width = new GridLength(10);

        _explorerOpen = true;
        ToggleExplorerMenu.Header = "Explorador";
    }

    // ── Consola ─────────────────────────────────────────────────────────────

    private void ToggleConsole()
    {
        if (_consoleOpen) CollapseConsole();
        else ExpandConsole();
    }

    private void CollapseConsole()
    {
        if (ConsoleRow.Height.Value > 10)
            _consoleLastHeight = ConsoleRow.Height.Value;

        ConsolePanel.IsVisible = false;
        VSplitter.IsVisible = false;

        ConsoleRow.MinHeight = 0;
        VGapRow.MinHeight = 0;
        ConsoleRow.Height = new GridLength(0);
        VGapRow.Height = new GridLength(0);

        _consoleOpen = false;
        ToggleConsoleMenu.Header = "Mostrar Consola";
    }

    private void ExpandConsole()
    {
        ConsolePanel.IsVisible = true;
        VSplitter.IsVisible = true;

        ConsoleRow.MinHeight = 60;
        ConsoleRow.Height = new GridLength(_consoleLastHeight);
        VGapRow.Height = new GridLength(10);

        _consoleOpen = true;
        ToggleConsoleMenu.Header = "Consola";
    }

    // ── Proyecto ─────────────────────────────────────────────────────────────

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        var dialog = new TextInputDialog("Nuevo Proyecto", "Nombre del proyecto:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        _projectService.CreateProject(name);
        ShowInExplorer(_projectService.CurrentProjectPath!);
    }

    private async void OnNewScript(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nuevo Script", "Nombre del script:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        _projectService.CreateScript(name);
        ShowInExplorer(_projectService.CurrentProjectPath);
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nueva Carpeta", "Nombre de la carpeta:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        _projectService.CreateFolder(name);
        ShowInExplorer(_projectService.CurrentProjectPath);
    }

    private void ShowInExplorer(string folder)
    {
        TreeExplorer.Items.Clear();
        var rootNode = new TreeViewItem { Header = Path.GetFileName(folder), Tag = null };
        AddNodes(rootNode, folder);
        rootNode.IsExpanded = true;
        TreeExplorer.Items.Add(rootNode);
    }

    private void AddNodes(TreeViewItem parent, string folder)
    {
        foreach (var dir in Directory.GetDirectories(folder))
        {
            var dirNode = new TreeViewItem { Header = Path.GetFileName(dir), Tag = null };
            AddNodes(dirNode, dir);
            parent.Items.Add(dirNode);
        }
        foreach (var file in Directory.GetFiles(folder, "*.py"))
        {
            parent.Items.Add(new TreeViewItem
            {
                Header = Path.GetFileName(file),
                Tag = file
            });
        }
    }

    // ── Editor ───────────────────────────────────────────────────────────────

    private void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (TreeExplorer.SelectedItem is not TreeViewItem item) return;
        if (item.Tag is not string fullPath) return;

        string content = File.ReadAllText(fullPath);

        if (!_integrityService.Validate(fullPath, content))
        {
            TextEditor.Text = content;
            TextEditor.IsReadOnly = true;
            TitleEditor.Text = Path.GetFileName(fullPath) + " ⚠ CORRUPTO";
            _currentFilePath = null;
            return;
        }

        TextEditor.IsReadOnly = false;
        TextEditor.Text = content;
        TitleEditor.Text = Path.GetFileName(fullPath);
        _currentFilePath = fullPath;
    }

    private void OnSaveScript(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath == null) return;
        string content = TextEditor.Text ?? "";
        File.WriteAllText(_currentFilePath, content);
        _integrityService.Sign(_currentFilePath, content);
    }

    private async void OnRunScript(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath == null) return;
        ConsoleOutput.Text = "";
        ConsoleOutput.Text = await _executionService.RunAsync(_currentFilePath);
    }
}