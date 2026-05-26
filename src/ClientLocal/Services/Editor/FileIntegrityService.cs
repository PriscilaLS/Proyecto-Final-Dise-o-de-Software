using System;
using System.IO;

namespace ClientLocal.Services.Editor;

public class FileIntegrityService : IIntegrityService
{
    private static FileIntegrityService? _instance;
    public static FileIntegrityService Instance => _instance ??= new FileIntegrityService();

    private static readonly string KeyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EduIDE", "master.key"
    );

    private FileIntegrityService() { }

    public void Sign(string filePath, string content)
    {
        string hash = ComputeHash(content);
        File.WriteAllText(GetSigPath(filePath), hash);
        File.WriteAllText(GetBakPath(filePath), content);
    }

    public bool Validate(string filePath, string content)
    {
        string sigPath = GetSigPath(filePath);
        if (!File.Exists(sigPath)) return false; // sin firma = archivo creado fuera
        string savedHash = File.ReadAllText(sigPath);
        string currentHash = ComputeHash(content);
        return savedHash == currentHash;
    }

    public void Trust(string filePath, string content)
    {
        Sign(filePath, content);
    }

    public bool HasSignature(string filePath)
    {
        string sigPath = GetSigPath(filePath);
        return File.Exists(sigPath);
    }

    public void RemoveSignature(string filePath)
    {
        string sigPath = GetSigPath(filePath);
        try
        {
            if (File.Exists(sigPath))
                File.Delete(sigPath);
        }
        catch
        {
        }
    }

    private string ComputeHash(string content)
    {
        byte[] key = GetOrCreateKey();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        byte[] hash = System.Security.Cryptography.HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hash);
    }

    private byte[] GetOrCreateKey()
    {
        if (File.Exists(KeyPath))
            return Convert.FromHexString(File.ReadAllText(KeyPath));

        Directory.CreateDirectory(Path.GetDirectoryName(KeyPath)!);
        byte[] key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(KeyPath, Convert.ToHexString(key));
        return key;
    }

    private string GetSigPath(string filePath)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(filePath);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        string name = Convert.ToHexString(hash);

        string sigsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EduIDE", "signatures"
        );
        Directory.CreateDirectory(sigsFolder);
        return Path.Combine(sigsFolder, name + ".sig");
    }

    public string? Restore(string filePath)
    {
        string bakPath = GetBakPath(filePath);
        if (!File.Exists(bakPath)) return null;
        return File.ReadAllText(bakPath);
    }

    private string GetBakPath(string filePath)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(filePath);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
        string name = Convert.ToHexString(hash);

        string bakFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EduIDE", "backups"
        );
        Directory.CreateDirectory(bakFolder);
        return Path.Combine(bakFolder, name + ".bak");
    }

    public void MoveBackup(string oldPath, string newPath)
    {
        string oldBak = GetBakPath(oldPath);
        string newBak = GetBakPath(newPath);
        if(File.Exists(oldBak))
            File.Move(oldBak, newBak);
    }

    public void MarkAsCorrupt(string filePath)
    {
        File.WriteAllText(GetSigPath(filePath), "CORRUPT");
    }
}