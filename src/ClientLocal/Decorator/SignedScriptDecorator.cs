using System;
using System.Security.Cryptography;
using System.Text;
using ClientLocal.Services.Editor;

namespace ClientLocal.Decorator;

public enum SignatureStatus { NotSigned, Valid, Invalid }

public class SignedScriptDecorator : ScriptDecorator
{
    public SignedScriptDecorator(IScript inner) : base(inner) { }

    public void Sign()
    {
        string hash = ComputeHash(_inner.GetText());
        SignatureStore.Save(_inner.GetPath(), hash);
    }

    public SignatureStatus VerifySignature()
    {
        string? savedHash = SignatureStore.GetHash(_inner.GetPath());
        
        if(savedHash == null)
            return SignatureStatus.NotSigned;
        
        string currentHash= ComputeHash(_inner.GetText());
        return savedHash == currentHash ? SignatureStatus.Valid : SignatureStatus.Invalid;
    }

    public void RegenerateSignature()
    {
        SignatureStore.Delete(_inner.GetPath());
        Sign();
    }

    private static string ComputeHash(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}