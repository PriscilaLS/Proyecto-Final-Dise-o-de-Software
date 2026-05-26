namespace ClientLocal.Services.Editor;

public interface IProjectService
{
    void CreateProject(string name, string directory);
    void CreateScript(string parenthPath, string name);
    void CreateFolder(string parenthPath, string name);
    string? CurrentProjectPath { get; }
    void OpenProject(string path);
}

