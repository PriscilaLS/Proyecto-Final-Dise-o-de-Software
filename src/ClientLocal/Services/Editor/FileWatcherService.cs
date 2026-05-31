using System;
using System.IO;
using ClientLocal.Services;

namespace ClientLocal.Services.Editor;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly IIntegrityService _integrityService;
    private readonly Action<string> _onFileCorrupted;
    private readonly Action? _onProjectStructureChanged;

    public FileWatcherService(
        IIntegrityService integrityService,
        Action<string> onFileCorrupted,
        Action? onProjectStructureChanged = null)
    {
        _integrityService = integrityService;
        _onFileCorrupted = onFileCorrupted;
        _onProjectStructureChanged = onProjectStructureChanged;
    }

    public void Watch(string folder)
    {
        Stop();
        _watcher = new FileSystemWatcher(folder)
        {
            Filter = "*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };
        
        _watcher.Changed += OnChanged;
        _watcher.Created += OnProjectStructureChanged;
        _watcher.Deleted += OnProjectStructureChanged;
        _watcher.Renamed += OnProjectStructureChanged;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(e.FullPath)) return;
        if (!string.Equals(Path.GetExtension(e.FullPath), ".py", StringComparison.OrdinalIgnoreCase)) return;
        if (!_integrityService.HasSignature(e.FullPath)) return;

        try
        {
            string content = File.ReadAllText(e.FullPath);
            if (!_integrityService.Validate(e.FullPath, content))
                _onFileCorrupted(e.FullPath);
        }
        catch {}
    }

    private void OnProjectStructureChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _onProjectStructureChanged?.Invoke();
        }
        catch {}
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        try
        {
            _onProjectStructureChanged?.Invoke();
        }
        catch {}
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
    
    public void Dispose() => Stop();

}
