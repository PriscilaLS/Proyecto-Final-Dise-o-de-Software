namespace ClientLocal.Services.VersionControl;

public class GitCommandResult
{
    public bool Success { get; init; }

    public string Output { get; init; } = string.Empty;

    public string Error { get; init; } = string.Empty;

    public string DisplayText
    {
        get
        {
            var text = string.Empty;

            if (!string.IsNullOrWhiteSpace(Output))
                text += Output.TrimEnd();

            if (!string.IsNullOrWhiteSpace(Error))
                text += (text.Length > 0 ? "\n\n" : string.Empty) + Error.TrimEnd();

            return string.IsNullOrWhiteSpace(text)
                ? "Comando ejecutado sin salida."
                : text;
        }
    }
}
