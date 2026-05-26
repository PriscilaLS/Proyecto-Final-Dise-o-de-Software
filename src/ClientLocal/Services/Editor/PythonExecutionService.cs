using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLocal.Services.Editor;

public class PythonExecutionService : IExecutionService
{
    private Process? _process;
    private CancellationTokenSource? _cts;
    private Action<string, bool>? _onOutput;

    public bool IsRunning => _process != null && !_process.HasExited;

    public async Task StartReplAsync(Action<string, bool> onOutput)
    {
        if (IsRunning) Stop();
        await Task.Delay(100);
        
        _onOutput = onOutput;
        _cts = new CancellationTokenSource();

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList = { "-u", "-i" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        _process.Start();
        StartReading();
    }

    public async Task RunAsync(string filePath, Action<string, bool> onOutput)
    {
        _onOutput = onOutput;
        
        if (IsRunning)
        {
            _process!.StandardInput.WriteLine();
            await Task.Delay(50);
            _process!.StandardInput.WriteLine($"exec(open('{filePath}').read())");
            _process!.StandardInput.Flush();
            return;
        }
        
        await StartReplAsync(onOutput);
        await Task.Delay(200);
        _process!.StandardInput.WriteLine($"exec(open('{filePath}').read())");
        _process!.StandardInput.Flush();
    }

    private void StartReading()
    {
        Task.Run(async () =>
        {
            var buffer = new char[1];
            while (_process != null && !_process.StandardOutput.EndOfStream)
            {
                int read = await _process.StandardOutput.ReadAsync(buffer, 0, 1);
                if (read > 0)
                    _onOutput?.Invoke(new string(buffer, 0, read), false);
            }
        });

        Task.Run(async () =>
        {
            var buffer = new char[1];
            while (_process != null && !_process.StandardError.EndOfStream)
            {
                int read = await _process.StandardError.ReadAsync(buffer, 0, 1);
                if (read > 0)
                    _onOutput?.Invoke(new string(buffer, 0, read), false);
            }
        });
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
        try
        {
            if (_process != null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch { }
        _cts?.Cancel();
        _process?.Dispose();
        _process = null;
    }
}
