using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace ClientLocal.Services.Editor;

public class ExplorerService
{
    private static readonly HashSet<string> IgnoredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        ".idea",
        ".vscode",
        "bin",
        "obj",
        "node_modules",
        "packages",
        "__pycache__"
    };

    private readonly TreeView _treeView;
    private readonly IProjectService _projectService;
    private readonly IIntegrityService _integrityService;
    
    private readonly Func<string, string, Task> _onMoveError;
    private readonly Action<string, string> _onTabPathChanged;
    private readonly Func<string, Task<string>> _onRenameRequest;
    private readonly Action<string> _onFileDeleted;
    private readonly Func<string, IReadOnlyList<string>, Task> _onExternalFilesDropped;
    private Action? _onNewFolder;
    private Action? _onNewScript;

    public ExplorerService(
        TreeView treeView,
        IProjectService projectService,
        IIntegrityService integrityService,
        Func<string, string, Task> onMoveError,
        Action<string, string> onTabPathChanged,
        Func<string, Task<string>> onRenameRequest,
        Action<string> onFileDeleted,
        Func<string, IReadOnlyList<string>, Task> onExternalFilesDropped)
        
    {
        _treeView         = treeView;
        _projectService   = projectService;
        _onMoveError      = onMoveError;
        _onTabPathChanged = onTabPathChanged;
        _onRenameRequest   = onRenameRequest;
        _onFileDeleted = onFileDeleted;
        _onExternalFilesDropped = onExternalFilesDropped;
        _integrityService = integrityService;

        DragDrop.SetAllowDrop(_treeView, true);
        _treeView.AddHandler(DragDrop.DropEvent, OnTreeDrop);
        _treeView.AddHandler(DragDrop.DragOverEvent, OnTreeDragOver);
    }

    public void Show(string folder, Action? onNewFolder = null, Action? onNewScript = null)
    {
        if (onNewFolder != null) _onNewFolder = onNewFolder;
        if (onNewScript != null) _onNewScript = onNewScript;

        _treeView.Items.Clear();
        if (!Directory.Exists(folder))
            return;

        var rootNode = new TreeViewItem
        {
            Header  = CreateProjectHeader(folder),
            Tag     = folder,
            Classes = { "project-root" }
        };
        AddNodes(rootNode, folder);
        rootNode.IsExpanded = true;
        _treeView.Items.Add(rootNode);
    }

    public string GetSelectedDirectory()
    {
        if (_treeView.SelectedItem is not TreeViewItem item) return _projectService.CurrentProjectPath!;
        if (item.Tag is not string path)                    return _projectService.CurrentProjectPath!;
        if (Directory.Exists(path)) return path;
        return Path.GetDirectoryName(path)!;
    }

    private void AddNodes(TreeViewItem parent, string folder)
    {
        if (!Directory.Exists(folder))
            return;

        string[] directories;
        string[] files;

        try
        {
            directories = Directory.GetDirectories(folder)
                .Where(ShouldShowDirectory)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            files = Directory.GetFiles(folder)
                .Where(ShouldShowFile)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var dir in directories)
        {
            var dirNode = new TreeViewItem { Header = CreateFolderHeader(dir), Tag = dir };
            AddNodes(dirNode, dir);
            AddDragDrop(dirNode);
            parent.Items.Add(dirNode);
        }
        foreach (var file in files)
        {
            var fileNode = new TreeViewItem { Header = CreateFileHeader(file), Tag = file };
            AddDragDrop(fileNode);
            parent.Items.Add(fileNode);
        }
    }

    private static bool ShouldShowDirectory(string path)
    {
        var name = Path.GetFileName(path);
        if (IgnoredFolders.Contains(name))
            return false;

        return IsVisibleFileSystemEntry(path);
    }

    private static bool ShouldShowFile(string path)
    {
        return IsVisibleFileSystemEntry(path);
    }

    private static bool IsVisibleFileSystemEntry(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return !attributes.HasFlag(FileAttributes.Hidden)
                && !attributes.HasFlag(FileAttributes.System)
                && !attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    private void AddDragDrop(TreeViewItem node)
    {
        DragDrop.SetAllowDrop(node, true);
        node.AddHandler(DragDrop.DropEvent, OnTreeItemDrop);
        node.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = HasExternalFiles(e)
                ? DragDropEffects.Copy
                : DragDropEffects.Move;
            e.Handled     = true;
        });

        Point dragStartPoint = default;
        bool  isDragPending  = false;
        PointerPressedEventArgs? dragTriggerEvent = null;

        node.AddHandler(
            InputElement.PointerPressedEvent,
            (object? _, PointerPressedEventArgs e) =>
            {
                if (e.GetCurrentPoint(node).Properties.IsLeftButtonPressed)
                {
                    dragStartPoint   = e.GetPosition(node);
                    isDragPending    = true;
                    dragTriggerEvent = e;
                }
            },
            RoutingStrategies.Tunnel
        );

        node.AddHandler(
            InputElement.PointerMovedEvent,
            async (object? _, PointerEventArgs e) =>
            {
                if (!isDragPending || dragTriggerEvent == null) return;
                if (!e.GetCurrentPoint(node).Properties.IsLeftButtonPressed) { isDragPending = false; return; }

                var diff = e.GetPosition(node) - dragStartPoint;
                if (Math.Abs(diff.X) < 8 && Math.Abs(diff.Y) < 8) return;

                isDragPending    = false;
                var trigger      = dragTriggerEvent;
                dragTriggerEvent = null;
                trigger.Pointer.Capture(null);

                var dragData = new DataTransfer();
                dragData.Add(DataTransferItem.CreateText(node.Tag as string ?? ""));
                await DragDrop.DoDragDropAsync(trigger, dragData, DragDropEffects.Move);
            },
            RoutingStrategies.Tunnel
        );

        node.AddHandler(
            InputElement.PointerReleasedEvent,
            (object? _, PointerReleasedEventArgs e) => { isDragPending = false; dragTriggerEvent = null; },
            RoutingStrategies.Tunnel
        );

        if (node.Tag is string nodePath && !Directory.Exists(nodePath))
        {
            var contextMenu = new ContextMenu();
            var renameItem = new MenuItem { Header = "Renombrar" };
            
            renameItem.Click += async (_, _) =>
            {
                bool isPythonFile = string.Equals(Path.GetExtension(nodePath), ".py", StringComparison.OrdinalIgnoreCase);
                string? content = isPythonFile ? File.ReadAllText(nodePath) : null;
                bool wasCorrupt = isPythonFile && !_integrityService.Validate(nodePath, content!);
                
                string currentName = isPythonFile ? Path.GetFileNameWithoutExtension(nodePath) : Path.GetFileName(nodePath);
                string? newName = await _onRenameRequest(currentName);
                if (string.IsNullOrWhiteSpace(newName)) return;

                string targetName = isPythonFile ? newName + ".py" : Path.GetFileName(newName);
                string newPath = Path.Combine(Path.GetDirectoryName(nodePath)!, targetName);
                if (newPath == nodePath) return;
                
                try
                {
                    File.Move(nodePath, newPath);
                    if (isPythonFile)
                    {
                        _integrityService.RemoveSignature(nodePath);
                        _integrityService.MoveBackup(nodePath, newPath);

                        if (!wasCorrupt)
                            _integrityService.Sign(newPath, File.ReadAllText(newPath));
                        else
                            _integrityService.MarkAsCorrupt(newPath);
                    }
                    
                    _onTabPathChanged(nodePath, newPath);
                    Show(_projectService.CurrentProjectPath!);
                }
                catch (Exception ex)
                {
                    await _onMoveError("Error al renombrar archivo", ex.Message);
                }
            };
            var deleteItem = new MenuItem { Header = "Eliminar" };
            deleteItem.Click += async (_, _) =>
            {
                try
                {
                    File.Delete(nodePath);
                    _integrityService.RemoveSignature(nodePath);
                    _onFileDeleted(nodePath);
                    Show(_projectService.CurrentProjectPath!);
                }
                catch (Exception ex)
                {
                    await _onMoveError("Error al eliminar archivo", ex.Message);
                }
            };
            contextMenu.Items.Add(renameItem);
            contextMenu.Items.Add(deleteItem);
            node.ContextMenu = contextMenu;
        }
    }

    private async void OnTreeItemDrop(object? sender, DragEventArgs e)
    {
        if (sender is not TreeViewItem targetNode) return;
        string? targetPath = targetNode.Tag as string;

        if (targetPath == null) return;

        var externalFiles = GetExternalFilePaths(e);
        if (externalFiles.Count > 0)
        {
            e.Handled = true;
            string targetDirectory = GetDropTargetDirectory(targetPath);
            await _onExternalFilesDropped(targetDirectory, externalFiles);
            return;
        }

        string? sourcePath = e.DataTransfer.TryGetText();

        if (sourcePath == null || sourcePath == targetPath) return;

        string destFolder = GetDropTargetDirectory(targetPath);
        string destPath   = Path.Combine(destFolder, Path.GetFileName(sourcePath)!);
        if (destPath == sourcePath) return;

        try
        {
            if (Directory.Exists(sourcePath)) Directory.Move(sourcePath, destPath);
            else                              File.Move(sourcePath, destPath);

            _onTabPathChanged(sourcePath, destPath);
            Show(_projectService.CurrentProjectPath!);
        }
        catch (Exception ex)
        {
            await _onMoveError("Error al mover archivo", ex.Message);
        }

        e.Handled = true;
    }

    private async void OnTreeDrop(object? sender, DragEventArgs e)
    {
        if (e.Handled) return;
        if (IsDropOnTreeViewItem(e.Source)) return;

        var externalFiles = GetExternalFilePaths(e);
        if (externalFiles.Count == 0) return;

        string? projectPath = _projectService.CurrentProjectPath;
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            return;

        e.Handled = true;
        await _onExternalFilesDropped(projectPath, externalFiles);
    }

    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        if (!HasExternalFiles(e)) return;

        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private static bool HasExternalFiles(DragEventArgs e) => GetExternalFilePaths(e).Count > 0;

    private static bool IsDropOnTreeViewItem(object? source)
    {
        if (source is not Visual visual)
            return false;

        return visual is TreeViewItem
            || visual.GetVisualAncestors().OfType<TreeViewItem>().Any();
    }

    private static IReadOnlyList<string> GetExternalFilePaths(DragEventArgs e)
    {
        try
        {
            return e.DataTransfer.TryGetFiles()?
                .Select(file =>
                {
                    try { return file.Path?.LocalPath; }
                    catch { return null; }
                })
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Select(path => path!)
                .ToArray()
                ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string GetDropTargetDirectory(string targetPath)
    {
        return Directory.Exists(targetPath)
            ? targetPath
            : Path.GetDirectoryName(targetPath)!;
    }

    private Control CreateFolderHeader(string path) => new StackPanel
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal,
        Spacing     = 8,
        Children =
        {
            new TextBlock
            {
                Text = "📁",
                Width = 18,
                FontSize = 14,
                FontFamily = FontFamily.Default
            },
            new TextBlock
            {
                Text         = Path.GetFileName(path),
                Foreground   = Avalonia.Media.Brushes.LightGray,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            }
        }
    };

    private Control CreateFileHeader(string path) => new StackPanel
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal,
        Spacing     = 8,
        Children =
        {
            CreateFileIcon(path),
            new TextBlock
            {
                Text         = Path.GetFileName(path),
                Foreground   = Avalonia.Media.Brushes.LightGray,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            }
        }
    };

    private Control CreateFileIcon(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".py", StringComparison.OrdinalIgnoreCase))
        {
            return new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/python.png"))),
                Width  = 16,
                Height = 16
            };
        }

        return new TextBlock
        {
            Text = GetFileIcon(path),
            Width = 18,
            FontSize = 14,
            FontFamily = FontFamily.Default
        };
    }

    private static string GetFileIcon(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" or ".log" => "📄",
            ".csv" or ".json" or ".xml" or ".yml" or ".yaml" => "📊",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" => "🖼️",
            ".pdf" => "📕",
            ".zip" or ".rar" or ".7z" => "📦",
            _ => "📎"
        };
    }

    private Control CreateProjectHeader(string path)
    {
        var panel   = new DockPanel();
        var buttons = new StackPanel
        {
            Orientation         = Avalonia.Layout.Orientation.Horizontal,
            Spacing             = 6,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var addFolderBtn = new Button
        {
            Width = 24, Height = 24,
            Classes = { "project-btn" },
            Content = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/add-folder.png"))),
                Width  = 14, Height = 14
            }
        };

        var addScriptBtn = new Button
        {
            Width = 24, Height = 24,
            Classes = { "project-btn" },
            Content = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/add-script.png"))),
                Width  = 14, Height = 14
            }
        };

        if (_onNewFolder != null) addFolderBtn.Click += (_, _) => _onNewFolder();
        if (_onNewScript != null) addScriptBtn.Click += (_, _) => _onNewScript();

        buttons.Children.Add(addFolderBtn);
        buttons.Children.Add(addScriptBtn);

        DockPanel.SetDock(buttons, Dock.Right);
        panel.Children.Add(buttons);
        panel.Children.Add(new TextBlock
        {
            Text              = Path.GetFileName(path),
            FontWeight        = Avalonia.Media.FontWeight.Bold,
            Foreground        = Avalonia.Media.Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming      = Avalonia.Media.TextTrimming.CharacterEllipsis
        });

        return panel;
    }
}
