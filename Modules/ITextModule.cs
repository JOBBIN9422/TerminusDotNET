using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    /// <summary>
    /// Interface for simple modules which don't rely on services/dependency injection (text replies/commands only)
    /// </summary>
    public interface ITextModule
    {
        Task SayAsync();
    }
}