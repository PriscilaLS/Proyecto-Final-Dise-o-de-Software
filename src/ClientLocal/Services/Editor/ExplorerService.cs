using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace ClientLocal.Services.Editor;

public class ExplorerService
{
    private readonly TreeView _treeView;
    private readonly IProjectService _projectService;
    private readonly IIntegrityService _integrityService;
    
    private readonly Func<string, string, Task> _onMoveError;
    private readonly Action<string, string> _onTabPathChanged;
    private readonly Func<string, Task<string>> _onRenameRequest;
    private readonly Action<string> _onFileDeleted;
    private Action? _onNewFolder;
    private Action? _onNewScript;

    public ExplorerService(
        TreeView treeView,
        IProjectService projectService,
        IIntegrityService integrityService,
        Func<string, string, Task> onMoveError,
        Action<string, string> onTabPathChanged,
        Func<string, Task<string>> onRenameRequest,
        Action<string> onFileDeleted)
        
    {
        _treeView         = treeView;
        _projectService   = projectService;
        _onMoveError      = onMoveError;
        _onTabPathChanged = onTabPathChanged;
        _onRenameRequest   = onRenameRequest;
        _onFileDeleted = onFileDeleted;
        _integrityService = integrityService;
    }

    public void Show(string folder, Action? onNewFolder = null, Action? onNewScript = null)
    {
        if (onNewFolder != null) _onNewFolder = onNewFolder;
        if (onNewScript != null) _onNewScript = onNewScript;

        _treeView.Items.Clear();
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
        foreach (var dir in Directory.GetDirectories(folder))
        {
            var dirNode = new TreeViewItem { Header = CreateFolderHeader(dir), Tag = dir };
            AddNodes(dirNode, dir);
            AddDragDrop(dirNode);
            parent.Items.Add(dirNode);
        }
        foreach (var file in Directory.GetFiles(folder, "*.py"))
        {
            var fileNode = new TreeViewItem { Header = CreateFileHeader(file), Tag = file };
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
                string content = File.ReadAllText(nodePath);
                bool wasCorrupt = !_integrityService.Validate(nodePath, content);
                
                string? newName = await _onRenameRequest(Path.GetFileNameWithoutExtension(nodePath));
                if (string.IsNullOrWhiteSpace(newName)) return;

                string newPath = Path.Combine(Path.GetDirectoryName(nodePath)!, newName + ".py");
                if (newPath == nodePath) return;
                
                try
                {
                    File.Move(nodePath, newPath);
                    _integrityService.RemoveSignature(nodePath);
                    _integrityService.MoveBackup(nodePath, newPath);

                    if (!wasCorrupt)
                        _integrityService.Sign(newPath, File.ReadAllText(newPath));
                    else
                        _integrityService.MarkAsCorrupt(newPath);
                    
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
        string? sourcePath = e.DataTransfer.TryGetText();
        string? targetPath = targetNode.Tag as string;

        if (sourcePath == null || targetPath == null || sourcePath == targetPath) return;

        string destFolder = Directory.Exists(targetPath) ? targetPath : Path.GetDirectoryName(targetPath)!;
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

    private Control CreateFolderHeader(string path) => new StackPanel
    {
        Orientation = Avalonia.Layout.Orientation.Horizontal,
        Spacing     = 8,
        Children =
        {
            new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/folder.png"))),
                Width  = 16, Height = 16
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
            new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://ClientLocal/Assets/python.png"))),
                Width  = 16, Height = 16
            },
            new TextBlock
            {
                Text         = Path.GetFileName(path),
                Foreground   = Avalonia.Media.Brushes.LightGray,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            }
        }
    };

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