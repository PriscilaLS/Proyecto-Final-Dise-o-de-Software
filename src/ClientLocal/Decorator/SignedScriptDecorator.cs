using ClientLocal.Services.Editor;

namespace ClientLocal.Decorator;

public enum SignatureStatus { NotSigned, Valid, Invalid }

public class SignedScriptDecorator : ScriptDecorator
{
    private readonly IIntegrityService _integrity = FileIntegrityService.Instance;

    public SignedScriptDecorator(IScript inner) : base(inner) { }

    public void Sign()
    {
        _integrity.Sign(_inner.GetPath(), _inner.GetText());
    }

    public SignatureStatus VerifySignature()
    {
        if (!_integrity.HasSignature(_inner.GetPath()))
            return SignatureStatus.NotSigned;

        bool valid = _integrity.Validate(_inner.GetPath(), _inner.GetText());
        return valid ? SignatureStatus.Valid : SignatureStatus.Invalid;
    }

    public void RegenerateSignature()
    {
        _integrity.RemoveSignature(_inner.GetPath());
        _integrity.Sign(_inner.GetPath(), _inner.GetText());
    }
}