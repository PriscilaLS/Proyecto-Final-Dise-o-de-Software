using System;
using System.Collections.Generic;
using System.IO;

namespace ClientLocal.Decorator;

public static class SignatureStore
{
    private static readonly string CsvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EduIDE", "signatures.csv"
    );

    // Guarda o actualiza la firma de un archivo
    public static void Save(string filePath, string hash)
    {
        var entries = LoadAll();
        entries[filePath] = new SignatureEntry(filePath, hash, DateTime.UtcNow.ToString("o"));
        WriteAll(entries);
    }

    // Devuelve el hash guardado, o null si no existe
    public static string? GetHash(string filePath)
    {
        var entries = LoadAll();
        return entries.TryGetValue(filePath, out var entry) ? entry.Hash : null;
    }
    
    public static void Delete(string filePath)
    {
        var entries = LoadAll();
        if (entries.Remove(filePath))
            WriteAll(entries);
    }

    // ── Privados ───────

    private static Dictionary<string, SignatureEntry> LoadAll()
    {
        var result = new Dictionary<string, SignatureEntry>();

        if (!File.Exists(CsvPath)) return result;

        foreach (var line in File.ReadAllLines(CsvPath))
        {
            var parts = line.Split(',');
            if (parts.Length < 3) continue;
            result[parts[0]] = new SignatureEntry(parts[0], parts[1], parts[2]);
        }

        return result;
    }

    private static void WriteAll(Dictionary<string, SignatureEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CsvPath)!);
        var lines = new List<string>();
        foreach (var e in entries.Values)
            lines.Add($"{e.FilePath},{e.Hash},{e.SignedAt}");
        File.WriteAllLines(CsvPath, lines);
    }
}

// Representa una fila del CSV
public record SignatureEntry(string FilePath, string Hash, string SignedAt);