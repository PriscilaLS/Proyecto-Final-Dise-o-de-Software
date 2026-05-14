namespace ClientLocal.Services;

public interface IProjectService
{
    void CreateProject(string name);
    void CreateScript(string name);
    void CreateFolder(string name);
    string? CurrentProjectPath { get; }
}

