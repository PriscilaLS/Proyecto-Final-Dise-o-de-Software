using System;
using System.IO;

namespace ClientLocal.Services;

public class ProjectService : IProjectService
{
    public string? CurrentProjectPath { get; private set; }

    public void CreateProject(string name, string directory)
	{
    	string path = Path.Combine(directory, name);
		Directory.CreateDirectory(path);
        CurrentProjectPath = path;
	}

    public void CreateScript(string parentPath, string name)
    {
        string path = Path.Combine(parentPath, name + ".py");
        File.WriteAllText(path, "");
    }

    public void CreateFolder(string parentPath, string name)
    {
        string path = Path.Combine(parentPath, name);
        Directory.CreateDirectory(path);
    }
    public void OpenProject(string path)
    {
        CurrentProjectPath = path;
    }
}