using System.Diagnostics;
using System.Threading.Tasks;

namespace ClientLocal.Services;

public class PythonExecutionService : IExecutionService
{
    public async Task<string> RunAsync(string filePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return string.IsNullOrEmpty(errors) ? output : output + "\n⚠ " + errors;
    }
}