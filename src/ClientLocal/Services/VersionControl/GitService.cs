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
