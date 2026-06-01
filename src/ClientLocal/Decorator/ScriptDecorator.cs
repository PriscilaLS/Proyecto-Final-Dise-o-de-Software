// STUB TEMPORAL - reemplazar con versión de P1
namespace ClientLocal.Decorator;

public abstract class ScriptDecorator : IScript
{
    protected readonly IScript _inner;

    protected ScriptDecorator(IScript inner)
    {
        _inner = inner;
    }

    public virtual string GetPath() => _inner.GetPath();
    public virtual string GetText() => _inner.GetText();
}