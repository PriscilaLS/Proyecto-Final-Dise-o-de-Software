using System;
using System.Collections.Generic;
using System.IO;

namespace ClientLocal.Services.Editor;

public class EditorTabService
{
    public class EditorTab
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Content  { get; set; } = "";
        public bool IsModified { get; set; } = false;
        public bool IsCorrupt  { get; set; } = false;

        public EditorTab(string filePath, string fileName)
        {
            FilePath = filePath;
            FileName = fileName;
        }
    }

    private readonly List<EditorTab> _tabs = new();
    public IReadOnlyList<EditorTab> Tabs => _tabs;
    public EditorTab? ActiveTab { get; private set; }

    public EditorTab Open(string fullPath, string content)
    {
        var existing = _tabs.Find(t => t.FilePath == fullPath);
        if (existing != null)
        {
            ActiveTab = existing;
            return existing;
        }

        var tab = new EditorTab(fullPath, Path.GetFileName(fullPath))
        {
            Content = content
        };
        _tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    public EditorTab? Close(EditorTab tab)
    {
        int index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (ActiveTab != tab) return ActiveTab;

        ActiveTab = _tabs.Count > 0 ? _tabs[Math.Max(0, index - 1)] : null;
        return ActiveTab;
    }

    public void SetActive(EditorTab tab)
    {
        ActiveTab = tab;
    }

    public void Reorder(EditorTab tab, int newIndex)
    {
        int current = _tabs.IndexOf(tab);
        if (current < 0 || current == newIndex) return;
        _tabs.RemoveAt(current);
        _tabs.Insert(newIndex, tab);
    }

    public void UpdatePath(string oldPath, string newPath)
    {
        var tab = _tabs.Find(t => t.FilePath == oldPath);
        if (tab == null) return;
        tab.FilePath = newPath;
        tab.FileName = Path.GetFileName(newPath); 
        if (ActiveTab?.FilePath == newPath)
            ActiveTab = tab;
    }

    public EditorTab? FindByPath(string path) =>
        _tabs.Find(t => t.FilePath == path);
}