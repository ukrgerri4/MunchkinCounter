using System.Threading.Tasks;

namespace Infrastracture.Interfaces
{
    public interface IScreenshotService
    {
        Task<string> CaptureAndSaveAsync();
    }
}
