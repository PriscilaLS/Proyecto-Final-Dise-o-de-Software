using System;
using System.IO;

namespace ClientLocal.Services;

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
    }

    public bool Validate(string filePath, string content)
    {
        string sigPath = GetSigPath(filePath);
        if (!File.Exists(sigPath)) return true; // sin firma = archivo nuevo, válido
        
        string savedHash = File.ReadAllText(sigPath);
        string currentHash = ComputeHash(content);
        return savedHash == currentHash;
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
}