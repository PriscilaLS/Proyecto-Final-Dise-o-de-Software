namespace ClientLocal.Services.Editor;

public interface IIntegrityService
{
    void Sign(string filePath, string content);
    bool Validate(string filePath, string content);
    void Trust(string filePath, string content);
    
    bool HasSignature(string filePath);
    void RemoveSignature(string filePath);
    string? Restore(string filePath);
    void MoveBackup(string oldPath, string newPath);
    void MarkAsCorrupt(string filePath);
}