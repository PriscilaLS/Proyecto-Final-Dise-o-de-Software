using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClientLocal.Services.Decorator;

public class SignedScriptDecorator : ScriptDecorator
{
    private readonly string _signatureStorePath;

    public SignedScriptDecorator(IScript script) : base(script)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "EduIDE", "DecoratorSignatures");
        Directory.CreateDirectory(folder);

        _signatureStorePath = Path.Combine(folder, "script_signatures.csv");
    }

    private string GetCurrentTextForSignature()
    {
        var path = GetPath();

        if (File.Exists(path))
            return File.ReadAllText(path);

        return GetText();
    }

    public string ComputeSignature()
    {
        var bytes = Encoding.UTF8.GetBytes(GetCurrentTextForSignature());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public void Sign()
    {
        var path = GetPath();
        var signature = ComputeSignature();

        var lines = File.Exists(_signatureStorePath)
            ? File.ReadAllLines(_signatureStorePath)
            : Array.Empty<string>();

        using var writer = new StreamWriter(_signatureStorePath, false, Encoding.UTF8);
        var updated = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', 2);

            if (parts.Length == 2 && string.Equals(parts[0], path, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteLine($"{path},{signature}");
                updated = true;
            }
            else
            {
                writer.WriteLine(line);
            }
        }

        if (!updated)
            writer.WriteLine($"{path},{signature}");
    }

    public bool VerifySignature()
    {
        if (!File.Exists(_signatureStorePath))
            return false;

        var path = GetPath();
        var currentSignature = ComputeSignature();

        foreach (var line in File.ReadAllLines(_signatureStorePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', 2);

            if (parts.Length == 2 && string.Equals(parts[0], path, StringComparison.OrdinalIgnoreCase))
                return string.Equals(parts[1], currentSignature, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public void RegenerateSignature()
    {
        Sign();
    }

    public bool HasStoredSignature()
    {
        if (!File.Exists(_signatureStorePath))
            return false;

        var path = GetPath();

        foreach (var line in File.ReadAllLines(_signatureStorePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', 2);

            if (parts.Length == 2 && string.Equals(parts[0], path, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public string GetSignatureStorePath() => _signatureStorePath;
}