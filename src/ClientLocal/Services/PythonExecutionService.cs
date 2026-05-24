using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLocal.Services;

public class PythonExecutionService : IExecutionService
{
    private Process? _process;
    private CancellationTokenSource? _cts;
    private bool _manuallyStopped = false;

    public bool IsRunning => _process != null && !_process.HasExited;

    public async Task RunAsync(string filePath, Action<string, bool> onOutput)
    {
        if (IsRunning)
        {
            Stop();
            await Task.Delay(100);
        }

        _manuallyStopped = false;
        _cts = new CancellationTokenSource();
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList = { "-u", filePath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        _process.Start();

        var stdoutTask = Task.Run(async () =>
        {
            var buffer = new char[1];
            while (!_process.StandardOutput.EndOfStream)
            {
                int read = await _process.StandardOutput.ReadAsync(buffer, 0, 1);
                if (read > 0)
                    onOutput(new string(buffer, 0, read), false);
            }
        });

        var stderrTask = Task.Run(async () =>
        {
            var buffer = new char[1];
            while (!_process.StandardError.EndOfStream)
            {
                int read = await _process.StandardError.ReadAsync(buffer, 0, 1);
                if (read > 0)
                    onOutput(new string(buffer, 0, read), true);
            }
        });

        try
        {
            await _process.WaitForExitAsync(_cts.Token);
            await stdoutTask;
            await stderrTask;
            onOutput($"\n[Proceso terminado con código {_process.ExitCode}]", false);
        }
        catch (OperationCanceledException)
        {
            if (_manuallyStopped)
                onOutput("\n⚠ Proceso detenido.", true);
        }
        finally
        {
            _process?.Dispose();
            _process = null;
        }
    }

    public void SendInput(string input)
    {
        if (IsRunning)
        {
            _process!.StandardInput.WriteLine(input);
            _process!.StandardInput.Flush();
        }
    }

    public void Stop()
    {
        _manuallyStopped = true;
        try
        {
            if (_process != null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch { }
        _cts?.Cancel();
    }
}
