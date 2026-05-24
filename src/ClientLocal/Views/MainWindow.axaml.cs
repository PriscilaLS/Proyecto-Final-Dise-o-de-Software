using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using ClientLocal.Services;
using ClientLocal.Helpers;
using Avalonia.Input;


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

    private readonly List<EditorTab> _tabs = new();
    private EditorTab? _activeTab;
    private EditorTab? _draggingTab;
    private int _dragStartIndex;
    

    private record EditorTab(string FilePath, string FileName)
    {
        public string Content { get; set; } = "";
        public bool IsModified { get; set; } = false;
        public bool IsCorrupt { get; set; } = false;
        
    }

    private static string GetDefaultProjectRoot()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "EduIde");
    }

    public MainWindow()
    {
        InitializeComponent();
        
        TextEditor.IsVisible = false;
        LineNumberScroller.IsVisible = false;
        EmptyEditorPlaceholder.IsVisible = true;

        NewProjectMenu.Click += OnNewProject;
        NewScriptMenu.Click += OnNewScript;
        NewFolderMenu.Click += OnNewFolder;
        TreeExplorer.SelectionChanged += OnFileSelected;
        SaveScriptMenu.Click += OnSaveScript;
        RunScriptBtn.Click += OnRunScript;
        ClearConsoleBtn.Click += (_, _) => ClearConsole();
        StopScriptBtn.Click += (_, _) => _executionService.Stop();
        OpenProjectMenu.Click += OnOpenProject;
        // SidebarHomeBtn.Click += (_, _) => { /* TODO */ };

        CloseExplorerBtn.Click += (_, _) => ToggleExplorer();
        CloseConsoleBtn.Click += (_, _) => ToggleConsole();
        SidebarExplorerBtn.Click += (_, _) => ToggleExplorer();
        SidebarConsoleBtn.Click += (_, _) => ToggleConsole();

        _clipboardGuard = new ClipboardGuard(TextEditor);

        TextEditor.KeyUp += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Return)
            {
                int caret = TextEditor.CaretIndex;
                string text = TextEditor.Text ?? "";

                int prevLineEnd = caret - 1;
                int prevLineStart = 0;
                for (int i = prevLineEnd - 1; i >= 0; i--)
                {
                    if (text[i] == '\n')
                    {
                        prevLineStart = i + 1;
                        break;
                    }
                }

                string prevLine = text.Substring(prevLineStart, prevLineEnd - prevLineStart);

                int spaces = 0;
                foreach (char c in prevLine)
                {
                    if (c == ' ') spaces++;
                    else break;
                }

                if (prevLine.TrimEnd().EndsWith(":"))
                    spaces += 4;

                if (spaces > 0)
                {
                    string indent = new string(' ', spaces);
                    string current = TextEditor.Text ?? "";
                    TextEditor.Text = current.Insert(caret, indent);
                    TextEditor.CaretIndex = caret + indent.Length;
                }
            }
        };
        
        TextEditor.AddHandler(
            InputElement.KeyDownEvent,
            (object? _, KeyEventArgs e) =>
            {
                if (e.Key == Avalonia.Input.Key.Back)
                {
                    int caret = TextEditor.CaretIndex;
                    string text = TextEditor.Text ?? "";

                    if (caret == 0) return;

                    int lineStart = 0;
                    for (int i = caret - 1; i >= 0; i--)
                    {
                        if (text[i] == '\n')
                        {
                            lineStart = i + 1;
                            break;
                        }
                    }

                    string beforeCaret = text.Substring(lineStart, caret - lineStart);
                    if (beforeCaret.Length > 0 && beforeCaret.All(c => c == ' '))
                    {
                        int spaces = beforeCaret.Length;
                        int toDelete = spaces % 4 == 0 ? 4 : spaces % 4;

                        e.Handled = true;
                        TextEditor.Text = text.Remove(caret - toDelete, toDelete);
                        TextEditor.CaretIndex = caret - toDelete;
                    }
                }
            },
            RoutingStrategies.Tunnel
        );

        TextEditor.TextChanged += (_, _) =>
        {
            string text = TextEditor.Text ?? "";
            int lineCount = Math.Max(1, text.Split('\n').Length);
            LineNumbers.Text = string.Join("\n", Enumerable.Range(1, lineCount));
            
            if (_activeTab != null)
                _activeTab.IsModified = true;
        };
        
        TextEditor.AddHandler(ScrollViewer.ScrollChangedEvent, (object? _, ScrollChangedEventArgs e) =>
        {
            LineNumberScroller.Offset = new Avalonia.Vector(0, e.OffsetDelta.Y + LineNumberScroller.Offset.Y);
        });
        
        ExplorerPanel.SizeChanged += (_, _) =>
        {
            if (TreeExplorer.Items.Count > 0 && 
                TreeExplorer.Items[0] is TreeViewItem root &&
                root.Header is Grid g &&
                g.Children[0] is TextBlock tb)
            {
                tb.MaxWidth = ExplorerPanel.Bounds.Width - 68;
            }
        };
        
        SidebarExplorerBtn.Classes.Add("active");
        CollapseConsole();

        ConsoleInput.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Return)
            {
                string input = ConsoleInput.Text ?? "";

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_currentLine != null)
                        _currentLine.Text += input;
                    else
                        AppendConsoleLine(input, false);

                    _currentLine = null;
                });

                _executionService.SendInput(input);
                ConsoleInput.Text = "";
            }
        };

        this.KeyDown += (_, e) =>
        {
            bool isSaveShortcut =
                e.Key == Avalonia.Input.Key.S &&
                (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control) ||
                 e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Meta));

            if (isSaveShortcut)
            {
                OnSaveScript(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    // ── Explorador ──────────

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
        
        SidebarExplorerBtn.Classes.Remove("active");
    }

    private void ExpandExplorer()
    {
        ExplorerPanel.IsVisible = true;
        HSplitter.IsVisible = true;

        ExplorerCol.MinWidth = 120;
        ExplorerCol.Width = new GridLength(_explorerLastWidth);
        HGapCol.Width = new GridLength(10);

        _explorerOpen = true;
        
        SidebarExplorerBtn.Classes.Add("active");
    }

    // ── Consola ─────────────────────

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
        
        SidebarConsoleBtn.Classes.Remove("active");
    }

    private void ExpandConsole()
    {
        ConsolePanel.IsVisible = true;
        VSplitter.IsVisible = true;

        ConsoleRow.MinHeight = 60;
        ConsoleRow.Height = new GridLength(_consoleLastHeight);
        VGapRow.Height = new GridLength(10);

        _consoleOpen = true;
        
        SidebarConsoleBtn.Classes.Add("active");
    }

    // ── Proyecto ─────────────────

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        var dialog = new TextInputDialog("Nuevo Proyecto", "Nombre del proyecto:", true);
        ProjectDialogResult? result = await dialog.ShowDialog<ProjectDialogResult?>(this);

        if (result is null)
            return;

        if (string.IsNullOrWhiteSpace(result.Name))
        {
            ErrorService.GetInstance().ShowError(this, "Nombre inválido", "El nombre del proyecto no puede estar vacío.");
            return;
        }

        string rootDirectory = string.IsNullOrWhiteSpace(result.Directory)
            ? GetDefaultProjectRoot()
            : result.Directory.Trim();

        if (!Directory.Exists(rootDirectory))
            Directory.CreateDirectory(rootDirectory);

        string projectName = result.Name.Trim();

        _projectService.CreateProject(projectName, rootDirectory);
        ShowInExplorer(_projectService.CurrentProjectPath!);
    }
    
    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Seleccionar Proyecto",
                    AllowMultiple = false
                }
            );

        
            if (folders == null || folders.Count == 0)
                return;

            var folder = folders[0];
            if (folder == null)
                return;

        
            string? rawPath = null;
            try
            {
                rawPath = folder.Path?.LocalPath;
            }
            catch
            {
                rawPath = null;
            }

        
            if (string.IsNullOrWhiteSpace(rawPath))
                rawPath = folder.Name;

            if (string.IsNullOrWhiteSpace(rawPath))
                return;

            string path = rawPath.TrimEnd('/', '\\');

            if (!Directory.Exists(path))
                return;

            try
            {
                _projectService.OpenProject(path);
                ShowInExplorer(path);
            }
            catch (Exception ex)
            {   
                ErrorService.GetInstance().ShowError(this, "Error al abrir proyecto", ex.Message);
            }
        }
        catch (Exception ex)
        {
            ErrorService.GetInstance().ShowError(this, "⚠ Error inesperado", ex.Message);
        }
}

    private async void OnNewScript(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nuevo Script", "Nombre del script:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;
        
        string parent = GetSelectedDirectory();
        _projectService.CreateScript(parent, name);
        string newFilePath =  Path.Combine(parent, name + ".py");
        _integrityService.Sign(newFilePath, "");
        ShowInExplorer(_projectService.CurrentProjectPath);
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nueva Carpeta", "Nombre de la carpeta:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        string parent = GetSelectedDirectory();
        _projectService.CreateFolder(parent, name);
        ShowInExplorer(_projectService.CurrentProjectPath);
    }

    private void ShowInExplorer(string folder)
    {
        TreeExplorer.Items.Clear();
        var rootNode = new TreeViewItem
        {
            Header = CreateProjectHeader(folder),
            Tag = folder,
            Classes = {"project-root"}
        };
        AddNodes(rootNode, folder);
        rootNode.IsExpanded = true;
        TreeExplorer.Items.Add(rootNode);
    }
    
    private string GetSelectedDirectory() 
    {
        if(TreeExplorer.SelectedItem is not TreeViewItem item) 
            return _projectService.CurrentProjectPath!;

        if (item.Tag is not  string path)
            return _projectService.CurrentProjectPath!;
        
        if (Directory.Exists(path)) return path; 
        
        return Path.GetDirectoryName(path)!;
    }

    private void AddNodes(TreeViewItem parent, string folder)
    {
        foreach (var dir in Directory.GetDirectories(folder))
        {
            var dirNode = new TreeViewItem { Header = CreateFolderHeader(dir), Tag = dir };
            AddNodes(dirNode, dir);
            AddDragDrop(dirNode);
            parent.Items.Add(dirNode);
        }
        foreach (var file in Directory.GetFiles(folder, "*.py"))
        {
            var fileNode = new TreeViewItem
            {
                Header = CreateFileHeader(file),
                Tag = file
            };
            AddDragDrop(fileNode);
            parent.Items.Add(fileNode);
        }
    }

    private void AddDragDrop(TreeViewItem node)
    {
        DragDrop.SetAllowDrop(node, true);

        node.AddHandler(DragDrop.DropEvent, OnTreeItemDrop);
        node.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });

        Point _dragStartPoint = default;
        bool _isDragPending = false;
        PointerPressedEventArgs? _dragTriggerEvent = null;

        node.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(node).Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = e.GetPosition(node);
                _isDragPending = true;
                _dragTriggerEvent = e;
            }
        };

        node.PointerMoved += async (_, e) =>
        {
            if (!_isDragPending || _dragTriggerEvent == null) return;
            if (!e.GetCurrentPoint(node).Properties.IsLeftButtonPressed)
            {
                _isDragPending = false;
                return;
            }

            var pos = e.GetPosition(node);
            var diff = pos - _dragStartPoint;
            if (Math.Abs(diff.X) < 8 && Math.Abs(diff.Y) < 8) return;

            _isDragPending = false;
            var trigger = _dragTriggerEvent;
            _dragTriggerEvent = null;

            var dragData = new DataTransfer();
            dragData.Add(DataTransferItem.CreateText(node.Tag as string ?? ""));
            await DragDrop.DoDragDropAsync(trigger, dragData, DragDropEffects.Move);
        };

        node.PointerReleased += (_, _) =>
        {
            _isDragPending = false;
            _dragTriggerEvent = null;
        };
    }

    private void OnTreeItemDrop(object? sender, Avalonia.Input.DragEventArgs e)
    {
        if (sender is not TreeViewItem targetNode) return;
        string? sourcePath = e.DataTransfer.TryGetText();
        string? targetPath = targetNode.Tag as string;

        if (sourcePath == null || targetPath == null) return;
        if (sourcePath == targetPath) return;

        string destFolder = Directory.Exists(targetPath) 
            ? targetPath 
            : Path.GetDirectoryName(targetPath)!;

        string destPath = Path.Combine(destFolder, Path.GetFileName(sourcePath)!);
        if (destPath == sourcePath) return;

        try
        {
            if (Directory.Exists(sourcePath))
                Directory.Move(sourcePath, destPath);
            else
                File.Move(sourcePath, destPath);

            var openTab = _tabs.FirstOrDefault(t => t.FilePath == sourcePath);
            if (openTab != null)
            {
                var newTab = openTab with { FilePath = destPath };
                int idx = _tabs.IndexOf(openTab);
                _tabs[idx] = newTab;
                if (_activeTab == openTab)
                {
                    _activeTab = newTab;
                    _currentFilePath = destPath;
                }
                RenderTabs();
            }

            ShowInExplorer(_projectService.CurrentProjectPath!);
        }
        catch (Exception ex)
        {
            ErrorService.GetInstance().ShowError(this, "Error al mover archivo", ex.Message);
        }

        e.Handled = true;
    }

    private Control CreateFolderHeader(string path)
    {
        return new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(
                        AssetLoader.Open(
                            new Uri("avares://ClientLocal/Assets/folder.png")
                        )
                    ),
                    Width = 16,
                    Height = 16   
                },
                new TextBlock
                {
                    Text = Path.GetFileName(path),
                    Foreground = Avalonia.Media.Brushes.LightGray,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                }
            }
        }; 
    }

    private Control CreateFileHeader(string path)
    {
        return new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(
                        AssetLoader.Open(
                            new Uri("avares://ClientLocal/Assets/python.png")
                        )
                    ),
                    Width = 16,
                    Height = 16
                },

                new TextBlock
                {
                    Text = Path.GetFileName(path),
                    Foreground = Avalonia.Media.Brushes.LightGray,
                    TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                }
            }
        };
    }

    private Control CreateProjectHeader(string path)
    {
        var panel = new DockPanel();
        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var addFolderBtn = new Button
        {
            Width = 24, Height = 24,
            Classes = { "project-btn" },
            Content = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/add-folder.png"))),
                Width = 14, Height = 14
            }
        };
        addFolderBtn.Click += OnNewFolder;

        var addScriptBtn = new Button
        {
            Width = 24, Height = 24,
            Classes = {  "project-btn" },
            Content = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/add-script.png"))),
                Width = 14, Height = 14
            }
        };
        addScriptBtn.Click += OnNewScript;

        buttons.Children.Add(addFolderBtn);
        buttons.Children.Add(addScriptBtn);

        DockPanel.SetDock(buttons, Dock.Right);
        panel.Children.Add(buttons);

        panel.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(path),
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
        });

        return panel;
    }


    // ── Editor ──────

    private async void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (TreeExplorer.SelectedItem is not TreeViewItem item) return;
        if (item.Tag is not string fullPath) return;
        if (Directory.Exists(fullPath)) return;

        string content = File.ReadAllText(fullPath);
        bool hasSig = _integrityService.HasSignature(fullPath);
        bool valid = _integrityService.Validate(fullPath, content);

        if (!valid)
        {
            if (!hasSig)
            { 
                var dialog = new IntegrityWarningDialog(IntegrityWarningDialog.Mode.External);
                string? result = await dialog.ShowDialog<string?>(this);

                if (result == "cancel") return;
                
                if (result == "delete")
                {
                    try
                    {
                        var existingTab = _tabs.FirstOrDefault(t => t.FilePath == fullPath);
                        if (existingTab != null)
                            CloseTab(existingTab);

                        File.Delete(fullPath);
                        _integrityService.RemoveSignature(fullPath);

                        if (!string.IsNullOrWhiteSpace(_projectService.CurrentProjectPath))
                            ShowInExplorer(_projectService.CurrentProjectPath!);
                    }
                    catch (Exception ex)
                    {
                        AppendConsoleLine($"Error al eliminar archivo: {ex.Message}\n", true);
                    }
                    return;
                }
            }
            else
            {
                var dialog = new IntegrityWarningDialog(IntegrityWarningDialog.Mode.Corrupt);
                string? result = await dialog.ShowDialog<string?>(this);

                if (result == "cancel") return;

                if (result == "delete")
                {
                    try
                    {
                        var existingTab = _tabs.FirstOrDefault(t => t.FilePath == fullPath);
                        if (existingTab != null)
                            CloseTab(existingTab);

                        File.Delete(fullPath);
                        _integrityService.RemoveSignature(fullPath);

                        if (!string.IsNullOrWhiteSpace(_projectService.CurrentProjectPath))
                            ShowInExplorer(_projectService.CurrentProjectPath!);
                    }
                    catch (Exception ex)
                    {
                        AppendConsoleLine($"Error al eliminar archivo: {ex.Message}\n", true);
                    }
                    return;
                }

                if (result == "restore")
                {
                    string? backup = _integrityService.Restore(fullPath);
                    if (backup == null)
                    {
                        await AlertDialog.Show(this, "Restauración fallida", "No hay una versión previa guardada para este archivo.");
                        return;
                    }
                    File.WriteAllText(fullPath, backup);
                    _integrityService.Sign(fullPath, backup);
                    TextEditor.IsReadOnly = false;
                    OpenTab(fullPath, backup);
                    return;
                }
                return;
            }
        }
        TextEditor.IsReadOnly = false;
        OpenTab(fullPath, content);     
    }

    private void OnSaveScript(object? sender, RoutedEventArgs e)
    {
        if (_activeTab == null || string.IsNullOrWhiteSpace(_currentFilePath))
            return;
        string content = TextEditor.Text ?? "";

        _activeTab.Content = content;
        _activeTab.IsModified = false;
        
        File.WriteAllText(_currentFilePath, content);
        _integrityService.Sign(_currentFilePath, content);
    }

    private async void OnRunScript(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath == null) return;

        if (_executionService.IsRunning)
            _executionService.Stop();
        
        await Task.Delay(100);
        
        if (!_consoleOpen)
            ExpandConsole();

        ClearConsole();
        ConsoleInput.IsEnabled = true;
        ConsoleInput.Text = "";
        AppendConsoleLine($"{GetShellPrompt()} python3 {_currentFilePath}\n", false);
        ConsoleInput.Focus();

        await _executionService.RunAsync(_currentFilePath, (text, isError) =>
        {
            AppendConsoleLine(text, isError);
        });

        ConsoleInput.IsEnabled = false;
        ConsoleInput.Text = "";
        AppendConsoleLine($"\n{GetShellPrompt()} ", false);
    }

    private void OpenTab(string fullPath, string content)
    {
        var existing = _tabs.FirstOrDefault(t => t.FilePath == fullPath);
        if (existing != null)
        {
            SetActiveTab(existing);
            return;
        }

        var tab = new EditorTab(fullPath, Path.GetFileName(fullPath))
        {
            Content = content
        };
        _tabs.Add(tab);
        RenderTabs();
        SetActiveTab(tab);
    }

    private void CloseTab(EditorTab tab)
    {
        int index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (_activeTab == tab)
        {
            _activeTab = null;
            if (_tabs.Count > 0)
                SetActiveTab(_tabs[Math.Max(0, index - 1)]);
            else
            {
                TextEditor.Text = "";
                _currentFilePath = null;
                
                TextEditor.IsVisible = false;
                LineNumberScroller.IsVisible = false;
                EmptyEditorPlaceholder.IsVisible = true;
            }
        }
        RenderTabs();
    }

    private void SetActiveTab(EditorTab tab)
    {
        if  (_activeTab != null)
            _activeTab.Content = TextEditor.Text ?? "";
        
        _activeTab = tab;
        _currentFilePath = tab.FilePath;

        EmptyEditorPlaceholder.IsVisible = false;
        TextEditor.IsVisible = true;
        LineNumberScroller.IsVisible = true;
        
        TextEditor.Text = tab.Content;
        RenderTabs();
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();
        foreach (var tab in _tabs)
        {
            bool isActive = tab == _activeTab;
            var tabBtn = new Button();
            if (isActive)
            {
                tabBtn.Classes.Add("tab-btn");
                tabBtn.Classes.Add("tab-active");
            }
            else
            {
                tabBtn.Classes.Add("tab-btn");
            }

            var panel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 6
            };

            var nameBlock = new TextBlock
            {
                Text = tab.IsCorrupt ? tab.FileName + " ⚠ (Corrupto)" : tab.FileName,
                Foreground = tab.IsCorrupt
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#f87171"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#dddddd")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            var closeBtn = new Button
            {
                Content = "✕",
                IsVisible = isActive
            };
            closeBtn.Classes.Add("tab-close");

            var capturedTab = tab;
            closeBtn.Click += (_, e) =>
            {
                e.Handled = true;
                CloseTab(capturedTab);
            };

            tabBtn.Click += (_, _) => SetActiveTab(capturedTab);

            tabBtn.PointerEntered += (_, _) =>
            {
                if (capturedTab != _activeTab)
                    closeBtn.IsVisible = true;
            };
            tabBtn.PointerExited += (_, _) =>
            {
                if (capturedTab != _activeTab)
                    closeBtn.IsVisible = false;
            };
            
            var capturedIndex = _tabs.IndexOf(tab);

            tabBtn.PointerPressed += (_, e) =>
            {
                _draggingTab = capturedTab;
                _dragStartIndex = _tabs.IndexOf(capturedTab);
                e.Pointer.Capture(tabBtn);
            };
            
            tabBtn.PointerReleased += (_, e) =>
            {
                _draggingTab = null;
                e.Pointer.Capture(null);
            };

            tabBtn.PointerMoved += (_, e) =>
            {
                if (_draggingTab == null || _draggingTab != capturedTab) return;
                if (!e.GetCurrentPoint(tabBtn).Properties.IsLeftButtonPressed) return;

                var pos = e.GetPosition(TabsPanel);
                int newIndex = 0;
                double x = 0;

                foreach (var child in TabsPanel.Children)
                {
                    if (x + child.Bounds.Width / 2 > pos.X) break;
                    x += child.Bounds.Width + 4;
                    newIndex++;
                }

                newIndex = Math.Clamp(newIndex, 0, _tabs.Count - 1);
                int currentIndex = _tabs.IndexOf(_draggingTab);

                if (newIndex != currentIndex)
                {
                    _tabs.RemoveAt(currentIndex);
                    _tabs.Insert(newIndex, _draggingTab);
                    RenderTabs();
                }
            };
            panel.Children.Add(nameBlock);
            panel.Children.Add(closeBtn);
            tabBtn.Content = panel;
            TabsPanel.Children.Add(tabBtn);
        }
    }

    private void ClearConsole()
    {
        ConsoleOutput.Items.Clear();
        _consoleBuffer.Clear();
        _currentLine = null;
    }

    private readonly StringBuilder _consoleBuffer = new();
    private TextBlock? _currentLine;

    private void AppendConsoleLine(string text, bool isError)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var color = new Avalonia.Media.SolidColorBrush(
                isError
                    ? Avalonia.Media.Color.Parse("#f87171")
                    : Avalonia.Media.Color.Parse("#d4d4d4"));

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    _currentLine = null;
                }
                else
                {
                    if (_currentLine == null)
                    {
                        _currentLine = new TextBlock
                        {
                            Foreground = color,
                            FontFamily = new Avalonia.Media.FontFamily("Menlo,Consolas,monospace"),
                            FontSize = 12,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        };
                        ConsoleOutput.Items.Add(_currentLine);
                    }
                    _currentLine.Text += c;
                }
            }

            ConsoleScroller.ScrollToEnd();
        });
    }
    
    private string GetShellPrompt()
    {
        string user = Environment.UserName;
        string host = Environment.MachineName;
        return $"{user}@{host} ~ %";
    }
    
}