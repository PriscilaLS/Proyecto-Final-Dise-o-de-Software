using System;
using System.IO;
using ClientLocal.Services;

namespace ClientLocal.Services.Editor;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly IIntegrityService _integrityService;
    private readonly Action<string> _onFileCorrupted;

    public FileWatcherService(IIntegrityService integrityService, Action<string> onFileCorrupted)
    {
        _integrityService = integrityService;
        _onFileCorrupted = onFileCorrupted;
    }

    public void Watch(string folder)
    {
        Stop();
        _watcher = new FileSystemWatcher(folder)
        {
            Filter = "*.py",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        
    _watcher.Changed += OnChanged;
    _watcher.EnableRaisingEvents = true;
    }
    
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(e.FullPath)) return;
        if (!_integrityService.HasSignature(e.FullPath)) return;

        try
        {
            string content = File.ReadAllText(e.FullPath);
            if (!_integrityService.Validate(e.FullPath, content))
                _onFileCorrupted(e.FullPath);
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