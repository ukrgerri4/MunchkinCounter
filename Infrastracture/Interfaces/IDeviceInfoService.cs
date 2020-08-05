namespace Infrastracture.Interfaces
{
    public interface IDeviceInfoService
    {
        string DeviceId { get; }

        bool IsIgorPhone { get; }
    }
}
