using System;
using System.Threading.Tasks;

namespace ClientLocal.Services.Editor;

public interface IExecutionService
{
   bool IsRunning { get; }
   Task RunAsync(string filePath, Action<string, bool> onOutput);
   Task StartReplAsync(Action<string, bool> onOutput);
   void SendInput(string input);
   void Stop();
}