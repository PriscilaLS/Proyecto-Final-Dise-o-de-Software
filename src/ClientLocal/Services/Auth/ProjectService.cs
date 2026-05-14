using System;
using System.IO;

namespace ClientLocal.Services;

public class ProjectService : IProjectService
{
    public string? CurrentProjectPath { get; private set; }

    public void CreateProject(string name)
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string path = Path.Combine(documents, "EduIDE-Proyectos", name);
        Directory.CreateDirectory(path);
        CurrentProjectPath = path;
    }

    public void CreateScript(string name)
    {
        if (CurrentProjectPath == null) return;
        string path = Path.Combine(CurrentProjectPath, name + ".py");
        File.WriteAllText(path, "");
    }

    public void CreateFolder(string name)
    {
        if (CurrentProjectPath == null) return;
        string path = Path.Combine(CurrentProjectPath, name);
        Directory.CreateDirectory(path);
    }
}