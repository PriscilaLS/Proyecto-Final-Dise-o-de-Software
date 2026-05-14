using System.Threading.Tasks;

namespace ClientLocal.Services;

public interface IExecutionService
{
    Task<string> RunAsync(string filePath);
}