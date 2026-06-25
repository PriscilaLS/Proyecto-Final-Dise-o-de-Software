namespace ClientLocal.Services.VersionControl;

public class GitSyncStatus
{
    public bool HasUpstream { get; init; }

    public int Ahead { get; init; }

    public int Behind { get; init; }

    public string Message { get; init; } = string.Empty;
}
