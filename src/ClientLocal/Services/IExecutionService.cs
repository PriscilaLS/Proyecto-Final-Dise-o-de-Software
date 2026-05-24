using System;
using System.Threading.Tasks;

namespace ClientLocal.Services;

public interface IExecutionService
{
   bool IsRunning { get; }
   Task RunAsync(string filePath, Action<string, bool> onOutput);
   void SendInput(string input);
   void Stop();
}