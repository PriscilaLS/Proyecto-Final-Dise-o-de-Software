using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ClientLocal.Decorator;

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
        SignatureStore.Save(new SignatureEntry(
            filePath,
            hash,
            DateTime.UtcNow.ToString("o"),
            content,
            false
            )
        );
    }

    public bool Validate(string filePath, string content)
    {
        var entry = SignatureStore.GetSignature(filePath);
        if (entry == null) return false;
        if (entry.IsCorrupt) return false;
        return entry.Hash == ComputeHash(content);
    }

    public void Trust(string filePath, string content)
    {
        Sign(filePath, content);
    }

    public bool HasSignature(string filePath)
    {
        return SignatureStore.Exists(filePath);
    }

    public void RemoveSignature(string filePath)
    {
        SignatureStore.Delete(filePath);
    }
    
    public string? Restore(string filePath)
    {
        return SignatureStore.GetSignature(filePath)?.Backup;
    }

    public void MoveBackup(string oldPath, string newPath)
    {
        SignatureStore.UpdatePath(oldPath, newPath);
    }
    
    public void MarkAsCorrupt(string filePath)
    {
        var entry = SignatureStore.GetSignature(filePath);
        if (entry == null) return;
        SignatureStore.Save(new SignatureEntry(
            filePath,
            entry.Hash,
            entry.SignedAt,
            entry.Backup,
            true
        ));
    }

    private string ComputeHash(string content)
    {
        byte[] key = GetOrCreateKey();
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        byte[] hash = HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hash);
    }

    private byte[] GetOrCreateKey()
    {
        if (File.Exists(KeyPath))
            return Convert.FromHexString(File.ReadAllText(KeyPath));

        Directory.CreateDirectory(Path.GetDirectoryName(KeyPath)!);
        byte[] key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllText(KeyPath, Convert.ToHexString(key));
        return key;
    }
}