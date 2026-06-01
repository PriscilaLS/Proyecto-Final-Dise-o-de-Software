namespace ClientLocal.Services.Decorator;

public abstract class ScriptDecorator : IScript
{
    protected readonly IScript InnerScript;

    protected ScriptDecorator(IScript script)
    {
        InnerScript = script;
    }

    public virtual string GetPath() => InnerScript.GetPath();

    public virtual string GetText() => InnerScript.GetText();
}