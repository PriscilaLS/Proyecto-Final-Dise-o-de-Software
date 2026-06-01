// Temporal esperando a P1
namespace ClientLocal.Decorator;

public class BasicScript : IScript
{
    private readonly string _path;
    private readonly string _text;

    public BasicScript(string path, string text)
    {
        _path = path;
        _text = text;
    }

    public string GetPath() => _path;
    public string GetText() => _text;
}