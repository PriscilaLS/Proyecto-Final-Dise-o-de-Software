using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClientLocal.Controls;
using ClientLocal.Models;
using ClientLocal.Services.Editor;

namespace ClientLocal.Views;

public partial class EditorView : Window
{
    // ── Estado UI ──────
    private double _explorerLastWidth  = 210;
    private double _consoleLastHeight  = 180;
    private bool   _explorerOpen       = true;
    private bool   _consoleOpen        = true;

    private RowDefinition    ConsoleRow  => RootGrid.RowDefinitions[2];
    private RowDefinition    VGapRow     => RootGrid.RowDefinitions[1];
    private ColumnDefinition ExplorerCol => TopGrid.ColumnDefinitions[0];
    private ColumnDefinition HGapCol     => TopGrid.ColumnDefinitions[1];

    // ── Servicios ───────
    private readonly IProjectService   _projectService   = new ProjectService();
    private readonly IIntegrityService _integrityService = FileIntegrityService.Instance;
    private readonly IExecutionService _executionService = new PythonExecutionService();
    private readonly EditorTabService  _tabService       = new();
    private readonly ExplorerService   _explorerService;
    private readonly FileWatcherService _watcherService;
    private readonly ClipboardGuard    _clipboardGuard;
    public event Action? HomeRequested;

    // ── Estado editor ────────
    private string?                   _currentFilePath;
    private EditorTabService.EditorTab? _draggingTab;
    private bool _switchingTab = false;

    // ── Consola ───
    private readonly StringBuilder _consoleBuffer = new();
    private TextBlock? _currentLine;
    

    private static string GetDefaultProjectRoot()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "EduIde");
    }

    public EditorView()
    {
        InitializeComponent();

        TextEditor.IsVisible = false;
        LineNumberScroller.IsVisible = false;
        EmptyEditorPlaceholder.IsVisible = true;

        _explorerService = new ExplorerService(
            TreeExplorer,
            _projectService,
            integrityService: _integrityService,
            onMoveError: async (title, msg) => await AlertDialog.Show(this, title, msg),
            onTabPathChanged: (oldPath, newPath) =>
            {
                _tabService.UpdatePath(oldPath, newPath);
                if (_tabService.ActiveTab?.FilePath == newPath)
                    _currentFilePath = newPath;
                RenderTabs();
            },
            onRenameRequest: async (currentName) =>
            {
                var dialog = new TextInputDialog("Renombrar Script", "Nuevo Nombre:", false, currentName);
                return await dialog.ShowDialog<string?>(this);
            },
            onFileDeleted: (path) =>
            {
                var tab = _tabService.FindByPath(path);
                if (tab != null) CloseTab(tab);
            }
        );

        // Menú y botones
        NewProjectMenu.Click += OnNewProject;
        NewScriptMenu.Click += OnNewScript;
        NewFolderMenu.Click += OnNewFolder;
        OpenProjectMenu.Click += OnOpenProject;
        SaveScriptMenu.Click += OnSaveScript;
        RunScriptBtn.Click += OnRunScript;
        ClearConsoleBtn.Click += (_, _) => ClearConsole();
        SidebarHomeBtn.Click += (_, _) => HomeRequested?.Invoke();
        StopScriptBtn.Click += async (_, _) =>
        {
            _executionService.Stop();
            await Task.Delay(200);
            ClearConsole();
            _ = _executionService.StartReplAsync((text, isError) => AppendConsoleLine(text, isError));
        };
        CloseExplorerBtn.Click += (_, _) => ToggleExplorer();
        CloseConsoleBtn.Click += (_, _) => ToggleConsole();
        SidebarExplorerBtn.Click += (_, _) => ToggleExplorer();
        SidebarConsoleBtn.Click += (_, _) => ToggleConsole();

        TreeExplorer.SelectionChanged += OnFileSelected;

        _clipboardGuard = new ClipboardGuard(TextEditor);

        // Auto indentación
        TextEditor.KeyUp += (_, e) =>
        {
            if (e.Key == Key.Return)
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

                if (prevLine.TrimEnd().EndsWith(":")) spaces += 4;

                if (spaces > 0)
                {
                    string indent = new string(' ', spaces);
                    TextEditor.Text = (TextEditor.Text ?? "").Insert(caret, indent);
                    TextEditor.CaretIndex = caret + indent.Length;
                }
            }
        };

        // Retroceso de indentación
        TextEditor.AddHandler(
            InputElement.KeyDownEvent,
            (object? _, KeyEventArgs e) =>
            {
                if (e.Key == Key.Back)
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

        // Actualizar números de línea
        TextEditor.TextChanged += (_, _) =>
        {
            string text = TextEditor.Text ?? "";
            int lineCount = Math.Max(1, text.Split('\n').Length);
            LineNumbers.Text = string.Join("\n", Enumerable.Range(1, lineCount));

            if (_tabService.ActiveTab != null && !_switchingTab)
                _tabService.ActiveTab.IsModified = true;
        };

        // Sincronizar scroll de números de línea
        TextEditor.AddHandler(ScrollViewer.ScrollChangedEvent,
            (object? _, ScrollChangedEventArgs e) =>
            {
                LineNumberScroller.Offset = new Vector(0, e.OffsetDelta.Y + LineNumberScroller.Offset.Y);
            });

        // Ajustar ancho del explorador del proyecto
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

        // Input de consola
        ConsoleInput.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Return)
            {
                string input = ConsoleInput.Text ?? "";

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_currentLine != null) _currentLine.Text += input;
                    else AppendConsoleLine(input, false);
                    _currentLine = null;
                });

                _executionService.SendInput(input);
                ConsoleInput.Text = "";
            }
        };

        // Atajo Cmd/Ctrl+S
        this.KeyDown += (_, e) =>
        {
            bool isSave = e.Key == Key.S &&
                          (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                           e.KeyModifiers.HasFlag(KeyModifiers.Meta));

            if (isSave)
            {
                OnSaveScript(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };

        SidebarExplorerBtn.Classes.Add("active");
        CollapseConsole();
        ExpandConsole();
        ConsoleInput.IsEnabled = true;
        
        _watcherService = new FileWatcherService(
            _integrityService,
            onFileCorrupted: (path) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    var tab = _tabService.FindByPath(path);
                    if (tab == null) return;

                    tab.IsCorrupt = true;
                    RenderTabs();

                    if (_tabService.ActiveTab == tab)
                        TextEditor.IsReadOnly = true;  // bloquear edición

                    var dialog = new IntegrityWarningDialog(IntegrityWarningDialog.Mode.Corrupt);
                    string? result = await dialog.ShowDialog<string?>(this);

                    if (result == "delete")
                    {
                        try
                        {
                            CloseTab(tab);
                            File.Delete(path);
                            _integrityService.RemoveSignature(path);
                            _explorerService.Show(_projectService.CurrentProjectPath!);
                        }
                        catch (Exception ex)
                        {
                            AppendConsoleLine($"Error al eliminar archivo: {ex.Message}\n", true);
                        }
                    }
                    else if (result == "restore")
                    {
                        string? backup = _integrityService.Restore(path);
                        if (backup == null)
                        {
                            await AlertDialog.Show(this, "Restauración fallida", "No hay una versión previa guardada para este archivo.");
                            return;
                        }

                        File.WriteAllText(path, backup);
                        _integrityService.Sign(path, backup);
                        tab.IsCorrupt        = false;
                        tab.Content          = backup;
                        TextEditor.IsReadOnly = false; 

                        if (_tabService.ActiveTab == tab)
                        {
                            _switchingTab = true;
                            TextEditor.Text = backup;
                            _switchingTab = false;
                        }

                        RenderTabs();
                    }
                    else if (result == "cancel")
                    { }
                });
            }
        );
        
        _ = _executionService.StartReplAsync((text, isError) => AppendConsoleLine(text, isError));
        
    }

    // ── Explorador ────

    private void ToggleExplorer()
    {
        if (_explorerOpen) CollapseExplorer();
        else ExpandExplorer();
    }

    private void CollapseExplorer()
    {
        if (ExplorerCol.Width.Value > 10) _explorerLastWidth = ExplorerCol.Width.Value;

        ExplorerPanel.IsVisible = false;
        HSplitter.IsVisible     = false;
        ExplorerCol.MinWidth    = 0;
        HGapCol.MinWidth        = 0;
        ExplorerCol.Width       = new GridLength(0);
        HGapCol.Width           = new GridLength(0);
        _explorerOpen           = false;

        SidebarExplorerBtn.Classes.Remove("active");
    }

    private void ExpandExplorer()
    {
        ExplorerPanel.IsVisible = true;
        HSplitter.IsVisible     = true;
        ExplorerCol.MinWidth    = 120;
        ExplorerCol.Width       = new GridLength(_explorerLastWidth);
        HGapCol.Width           = new GridLength(10);
        _explorerOpen           = true;

        SidebarExplorerBtn.Classes.Add("active");
    }

    // ── Consola ──────

    private void ToggleConsole()
    {
        if (_consoleOpen) CollapseConsole();
        else ExpandConsole();
    }

    private void CollapseConsole()
    {
        if (ConsoleRow.Height.Value > 10) _consoleLastHeight = ConsoleRow.Height.Value;

        ConsolePanel.IsVisible = false;
        VSplitter.IsVisible    = false;
        ConsoleRow.MinHeight   = 0;
        VGapRow.MinHeight      = 0;
        ConsoleRow.Height      = new GridLength(0);
        VGapRow.Height         = new GridLength(0);
        _consoleOpen           = false;

        SidebarConsoleBtn.Classes.Remove("active");
    }

    private void ExpandConsole()
    {
        ConsolePanel.IsVisible = true;
        VSplitter.IsVisible    = true;
        ConsoleRow.MinHeight   = 60;
        ConsoleRow.Height      = new GridLength(_consoleLastHeight);
        VGapRow.Height         = new GridLength(10);
        _consoleOpen           = true;

        SidebarConsoleBtn.Classes.Add("active");
    }

    private void ClearConsole()
    {
        ConsoleOutput.Items.Clear();
        _consoleBuffer.Clear();
        _currentLine = null;
    }

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
                            Foreground   = color,
                            FontFamily   = new Avalonia.Media.FontFamily("Menlo,Consolas,monospace"),
                            FontSize     = 12,
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

    // ── Proyecto ─────────────────────────────────────────────────────────────

    private async void OnNewProject(object? sender, RoutedEventArgs e)
    {
        var dialog = new TextInputDialog("Nuevo Proyecto", "Nombre del proyecto:", true);
        ProjectDialogResult? result = await dialog.ShowDialog<ProjectDialogResult?>(this);

        if (result is null) return;

        if (string.IsNullOrWhiteSpace(result.Name))
        {
            await AlertDialog.Show(this, "Nombre inválido", "El nombre del proyecto no puede estar vacío.");
            return;
        }

        string rootDirectory = string.IsNullOrWhiteSpace(result.Directory)
            ? GetDefaultProjectRoot()
            : result.Directory.Trim();

        if (!Directory.Exists(rootDirectory))
            Directory.CreateDirectory(rootDirectory);

        _projectService.CreateProject(result.Name.Trim(), rootDirectory);
        _explorerService.Show(_projectService.CurrentProjectPath!,
            onNewFolder: () => OnNewFolder(null, new RoutedEventArgs()),
            onNewScript: () => OnNewScript(null, new RoutedEventArgs())
        );
        _watcherService.Watch(_projectService.CurrentProjectPath!);
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Seleccionar Proyecto", AllowMultiple = false }
            );

            if (folders == null || folders.Count == 0) return;

            var folder = folders[0];
            if (folder == null) return;

            string? rawPath = null;
            try { rawPath = folder.Path?.LocalPath; } catch { rawPath = null; }

            if (string.IsNullOrWhiteSpace(rawPath)) rawPath = folder.Name;
            if (string.IsNullOrWhiteSpace(rawPath)) return;

            string path = rawPath.TrimEnd('/', '\\');
            if (!Directory.Exists(path)) return;

            try
            {
                _projectService.OpenProject(path);
                _explorerService.Show(_projectService.CurrentProjectPath!,
                    onNewFolder: () => OnNewFolder(null, new RoutedEventArgs()),
                    onNewScript: () => OnNewScript(null, new RoutedEventArgs())
                );
                _watcherService.Watch(_projectService.CurrentProjectPath!);
            }
            catch (Exception ex)
            {
                await AlertDialog.Show(this, "Error al abrir proyecto", ex.Message);
            }
        }
        catch (Exception ex)
        {
            await AlertDialog.Show(this, "⚠ Error inesperado", ex.Message);
        }
    }

    private async void OnNewScript(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nuevo Script", "Nombre del script:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        string parent      = _explorerService.GetSelectedDirectory();
        string newFilePath = Path.Combine(parent, name + ".py");

        _projectService.CreateScript(parent, name);
        _integrityService.Sign(newFilePath, "");
        _explorerService.Show(_projectService.CurrentProjectPath);
    }

    private async void OnNewFolder(object? sender, RoutedEventArgs e)
    {
        if (_projectService.CurrentProjectPath == null) return;

        var dialog = new TextInputDialog("Nueva Carpeta", "Nombre de la carpeta:");
        var name = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        string parent = _explorerService.GetSelectedDirectory();
        _projectService.CreateFolder(parent, name);
        _explorerService.Show(_projectService.CurrentProjectPath);
    }

    // ── Editor ───────────────────────────────────────────────────────────────

    private async void OnFileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (TreeExplorer.SelectedItem is not TreeViewItem item) return;
        if (item.Tag is not string fullPath)                    return;
        if (Directory.Exists(fullPath))                         return;

        string content = File.ReadAllText(fullPath);
        bool   hasSig  = _integrityService.HasSignature(fullPath);
        bool   valid   = _integrityService.Validate(fullPath, content);

        if (!valid)
        {
            var mode   = hasSig ? IntegrityWarningDialog.Mode.Corrupt : IntegrityWarningDialog.Mode.External;
            var dialog = new IntegrityWarningDialog(mode);
            string? result = await dialog.ShowDialog<string?>(this);

            if (result == "cancel") return;

            if (result == "delete")
            {
                try
                {
                    var existingTab = _tabService.FindByPath(fullPath);
                    if (existingTab != null) CloseTab(existingTab);

                    File.Delete(fullPath);
                    _integrityService.RemoveSignature(fullPath);

                    if (!string.IsNullOrWhiteSpace(_projectService.CurrentProjectPath))
                        _explorerService.Show(_projectService.CurrentProjectPath!);
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

        TextEditor.IsReadOnly = false;
        OpenTab(fullPath, content);
    }

    private void OnSaveScript(object? sender, RoutedEventArgs e)
    {
        if (_tabService.ActiveTab == null || string.IsNullOrWhiteSpace(_currentFilePath)) return;

        string content = TextEditor.Text ?? "";
        _tabService.ActiveTab.Content    = content;
        _tabService.ActiveTab.IsModified = false;

        File.WriteAllText(_currentFilePath, content);
        _integrityService.Sign(_currentFilePath, content);
    }

    private async void OnRunScript(object? sender, RoutedEventArgs e)
    {
        if (_currentFilePath == null) return;
        OnSaveScript(null, new RoutedEventArgs());

        if (!_consoleOpen) ExpandConsole();

        AppendConsoleLine($"\n{GetShellPrompt()} python3 {_currentFilePath}\n", false);
        ConsoleInput.Focus();

        await _executionService.RunAsync(_currentFilePath, (text, isError) =>
        {
            AppendConsoleLine(text, isError);
        });
    }

    // ── Tabs ──

    private void OpenTab(string fullPath, string content)
    {
        var tab = _tabService.Open(fullPath, content);
        SetActiveTab(tab);
    }

    private void CloseTab(EditorTabService.EditorTab tab)
    {
        var next = _tabService.Close(tab);

        if (next != null)
        {
            SetActiveTab(next);
        }
        else
        {
            TextEditor.Text                  = "";
            _currentFilePath                 = null;
            TextEditor.IsVisible             = false;
            LineNumberScroller.IsVisible     = false;
            EmptyEditorPlaceholder.IsVisible = true;
        }
    }

    private void SetActiveTab(EditorTabService.EditorTab tab)
    {
        if (_tabService.ActiveTab != null && _tabService.ActiveTab != tab)
            _tabService.ActiveTab.Content = TextEditor.Text ?? "";

        _tabService.SetActive(tab);
        _currentFilePath = tab.FilePath;
        TextEditor.IsReadOnly = tab.IsCorrupt;

        _switchingTab = true;
        EmptyEditorPlaceholder.IsVisible = false;
        TextEditor.IsVisible             = true;
        LineNumberScroller.IsVisible     = true;
        TextEditor.Text                  = tab.Content;
        _switchingTab = false;

        RenderTabs();
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();

        foreach (var tab in _tabService.Tabs)
        {
            bool isActive = tab == _tabService.ActiveTab;

            var tabBtn = new Button();
            tabBtn.Classes.Add("tab-btn");
            if (isActive) tabBtn.Classes.Add("tab-active");

            var nameBlock = new TextBlock
            {
                Text = tab.IsCorrupt ? tab.FileName + " ⚠ (Corrupto)" : tab.FileName,
                Foreground = tab.IsCorrupt
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#f87171"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#dddddd")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var closeBtn = new Button { Content = "✕", IsVisible = isActive };
            closeBtn.Classes.Add("tab-close");

            var capturedTab = tab;
            closeBtn.Click += (_, e) => { e.Handled = true; CloseTab(capturedTab); };
            tabBtn.Click   += (_, _) => SetActiveTab(capturedTab);

            tabBtn.PointerEntered += (_, _) => { if (capturedTab != _tabService.ActiveTab) closeBtn.IsVisible = true;  };
            tabBtn.PointerExited  += (_, _) => { if (capturedTab != _tabService.ActiveTab) closeBtn.IsVisible = false; };

            tabBtn.PointerPressed  += (_, e) => { _draggingTab = capturedTab; e.Pointer.Capture(tabBtn); };
            tabBtn.PointerReleased += (_, e) => { _draggingTab = null; e.Pointer.Capture(null); };

            tabBtn.PointerMoved += (_, e) =>
            {
                if (_draggingTab == null || _draggingTab != capturedTab) return;
                if (!e.GetCurrentPoint(tabBtn).Properties.IsLeftButtonPressed) return;

                var    pos      = e.GetPosition(TabsPanel);
                int    newIndex = 0;
                double x        = 0;

                foreach (var child in TabsPanel.Children)
                {
                    if (x + child.Bounds.Width / 2 > pos.X) break;
                    x += child.Bounds.Width + 4;
                    newIndex++;
                }

                newIndex = Math.Clamp(newIndex, 0, _tabService.Tabs.Count - 1);
                _tabService.Reorder(capturedTab, newIndex);
                RenderTabs();
            };

            var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            panel.Children.Add(nameBlock);
            panel.Children.Add(closeBtn);
            tabBtn.Content = panel;
            TabsPanel.Children.Add(tabBtn);
        }
    }
}