namespace ClientLocal.Services;

public interface IIntegrityService
{
    void Sign(string filePath, string content);
    bool Validate(string filePath, string content);
}