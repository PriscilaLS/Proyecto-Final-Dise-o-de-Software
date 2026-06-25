using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLocal.Services.VersionControl;

public class GitService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    public Task<GitCommandResult> InitAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "init");
    }

    public async Task<bool> IsRepositoryAsync(string repositoryPath)
    {
        var result = await RunGitAsync(repositoryPath, "rev-parse", "--is-inside-work-tree");
        return result.Success && result.Output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> GetCurrentBranchAsync(string repositoryPath)
    {
        var result = await RunGitAsync(repositoryPath, "branch", "--show-current");

        if (!result.Success)
            return "Sin rama";

        var branch = result.Output.Trim();
        return string.IsNullOrWhiteSpace(branch) ? "HEAD sin rama" : branch;
    }

    public Task<GitCommandResult> StatusAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "status", "--short");
    }

    public Task<GitCommandResult> DiffAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "diff", "--", ".");
    }

    public Task<GitCommandResult> DiffFileAsync(string repositoryPath, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(new GitCommandResult
            {
                Success = false,
                Error = "Selecciona un archivo para comparar sus cambios."
            });
        }

        return RunGitAsync(repositoryPath, "diff", "--", filePath);
    }

    public Task<GitCommandResult> LogAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "log", "--pretty=format:%h %s%n%b%n", "-10");
    }

    public Task<GitCommandResult> GetRemoteAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "remote", "get-url", "origin");
    }

    public async Task<GitCommandResult> SetRemoteAsync(string repositoryPath, string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "Escribe la URL del repositorio remoto antes de configurar origin."
            };
        }

        var currentRemote = await GetRemoteAsync(repositoryPath);
        if (currentRemote.Success)
            return await RunGitAsync(repositoryPath, "remote", "set-url", "origin", remoteUrl.Trim());

        return await RunGitAsync(repositoryPath, "remote", "add", "origin", remoteUrl.Trim());
    }

    public async Task<GitCommandResult> PushAsync(string repositoryPath)
    {
        var branch = await GetCurrentBranchAsync(repositoryPath);
        if (string.IsNullOrWhiteSpace(branch) || branch == "Sin rama" || branch == "HEAD sin rama")
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "No se puede hacer push porque no hay una rama activa."
            };
        }

        return await RunGitAsync(repositoryPath, "push", "-u", "origin", branch);
    }

    public Task<GitCommandResult> PullAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "pull", "--ff-only");
    }

    public Task<GitCommandResult> FetchAsync(string repositoryPath)
    {
        return RunGitAsync(repositoryPath, "fetch", "origin");
    }

    public async Task<GitSyncStatus> GetSyncStatusAsync(string repositoryPath)
    {
        var upstream = await RunGitAsync(repositoryPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}");
        if (!upstream.Success)
        {
            return new GitSyncStatus
            {
                HasUpstream = false,
                Message = "La rama todavia no tiene upstream. Usa Push para publicarla."
            };
        }

        var fetch = await FetchAsync(repositoryPath);
        if (!fetch.Success)
        {
            return new GitSyncStatus
            {
                HasUpstream = true,
                Message = fetch.DisplayText
            };
        }

        var counts = await RunGitAsync(repositoryPath, "rev-list", "--left-right", "--count", "HEAD...@{u}");
        if (!counts.Success)
        {
            return new GitSyncStatus
            {
                HasUpstream = true,
                Message = counts.DisplayText
            };
        }

        var parts = counts.Output
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2 || !int.TryParse(parts[0], out var ahead) || !int.TryParse(parts[1], out var behind))
        {
            return new GitSyncStatus
            {
                HasUpstream = true,
                Message = "No se pudo calcular la diferencia entre local y remoto."
            };
        }

        return new GitSyncStatus
        {
            HasUpstream = true,
            Ahead = ahead,
            Behind = behind,
            Message = "Sincronizacion revisada."
        };
    }

    public async Task<GitCommandResult> CommitAsync(string repositoryPath, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "Escribe un mensaje de commit antes de guardar cambios."
            };
        }

        var addResult = await RunGitAsync(repositoryPath, "add", "-A");
        if (!addResult.Success)
            return addResult;

        return await RunGitAsync(repositoryPath, "commit", "-m", message.Trim());
    }

    public async Task<GitCommandResult> CommitAsync(string repositoryPath, string summary, string description)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "Escribe un resumen antes de guardar la version."
            };
        }

        var addResult = await RunGitAsync(repositoryPath, "add", "-A");
        if (!addResult.Success)
            return addResult;

        if (string.IsNullOrWhiteSpace(description))
            return await RunGitAsync(repositoryPath, "commit", "-m", summary.Trim());

        return await RunGitAsync(repositoryPath, "commit", "-m", summary.Trim(), "-m", description.Trim());
    }

    private static async Task<GitCommandResult> RunGitAsync(string repositoryPath, params string[] arguments)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath) || !Directory.Exists(repositoryPath))
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "Selecciona una carpeta válida antes de usar Git."
            };
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        try
        {
            using var cancellation = new CancellationTokenSource(CommandTimeout);

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellation.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cancellation.Token);

            await process.WaitForExitAsync(cancellation.Token);

            var output = await outputTask;
            var error = await errorTask;

            return new GitCommandResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error
            };
        }
        catch (Win32Exception)
        {
            return new GitCommandResult
            {
                Success = false,
                Error = "No se encontró Git en el equipo. Instala Git o agrega git.exe al PATH."
            };
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Se evita que un proceso Git quede abierto si el timeout lo canceló.
            }

            return new GitCommandResult
            {
                Success = false,
                Error = "La operación Git tardó demasiado y fue cancelada."
            };
        }
    }
}
