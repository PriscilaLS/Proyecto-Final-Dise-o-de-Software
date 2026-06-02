using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClientLocal.Decorator;

public class SignatureEntry
{
    public string FilePath  { get; set; } = "";
    public string Hash      { get; set; } = "";
    public string SignedAt  { get; set; } = "";
    public string Backup    { get; set; } = "";
    public bool   IsCorrupt { get; set; } = false;

    public SignatureEntry() { }

    public SignatureEntry(string filePath, string hash, string signedAt, string backup, bool isCorrupt)
    {
        FilePath  = filePath;
        Hash      = hash;
        SignedAt  = signedAt;
        Backup    = backup;
        IsCorrupt = isCorrupt;
    }
}

public static class SignatureStore
{
    private static readonly string CsvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EduIDE", "signatures.csv");

    public static void Save(SignatureEntry entry)
    {
        var entries = LoadAll();
        entries[entry.FilePath] = entry;
        WriteAll(entries);
    }

    public static bool Exists(string filePath) => GetEntry(filePath) != null;

    private static SignatureEntry? GetEntry(string filePath)
    {
        var entries = LoadAll();
        return entries.TryGetValue(filePath, out var entry) ? entry : null;
    }
    
    public static string? GetHash(string filePath) => GetEntry(filePath)?.Hash;
    
    public static SignatureEntry? GetSignature(string filePath) => GetEntry(filePath);

    public static void Delete(string filePath)
    {
        var entries = LoadAll();
        if (entries.Remove(filePath))
            WriteAll(entries);
    }

    public static void UpdatePath(string oldPath, string newPath)
    {
        var entries = LoadAll();
        if (!entries.TryGetValue(oldPath, out var entry)) return;
        entries.Remove(oldPath);
        entries[newPath] = new SignatureEntry(newPath, entry.Hash, entry.SignedAt, entry.Backup, entry.IsCorrupt);
        WriteAll(entries);
    }
    
    private static Dictionary<string, SignatureEntry> LoadAll()
    {
        var result = new Dictionary<string, SignatureEntry>();
        if (!File.Exists(CsvPath)) return result;

        foreach (var line in File.ReadAllLines(CsvPath))
        {
            var parts = line.Split('\t');

            if (parts.Length == 6 && parts[0] == "v2")
            {
                var filePath = Decode(parts[1]);
                result[filePath] = new SignatureEntry(
                    filePath,
                    parts[2],
                    parts[3],
                    Decode(parts[4]),
                    parts[5] == "1"
                );
                continue;
            }

            if (parts.Length != 5) continue;
            result[parts[0]] = new SignatureEntry(
                parts[0],
                parts[1],
                parts[2],
                parts[3],
                parts[4] == "1"
            );
        }
        return result;
    }

    private static void WriteAll(Dictionary<string, SignatureEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath)!);
        var lines = new List<string>();
        foreach (var e in entries.Values)
        {
            lines.Add(
                $"v2\t{Encode(e.FilePath)}\t{e.Hash}\t{e.SignedAt}\t{Encode(e.Backup)}\t{(e.IsCorrupt ? "1" : "0")}");
        }
        File.WriteAllLines(CsvPath, lines);
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string Decode(string value)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
}
    
