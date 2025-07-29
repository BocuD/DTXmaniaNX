using System.Diagnostics;

namespace FDK;

public static class DeviceResetManager
{
    private static readonly List<IDeviceResettable> _resources = [];

    public static void Register(IDeviceResettable resource)
    {
        if (!_resources.Contains(resource))
            _resources.Add(resource);
    }

    public static void Unregister(IDeviceResettable resource)
    {
        _resources.Remove(resource);
    }

    public static void NotifyDeviceLost()
    {
        Trace.TraceInformation("Device lost, notifying all registered resources...");
        foreach (var res in _resources)
            res.OnDeviceLost();
    }

    public static void NotifyDeviceReset()
    {
        Trace.TraceInformation("Device reset, notifying all registered resources...");
        foreach (var res in _resources)
            res.OnDeviceReset();
    }

    public static void Clear()
    {
        _resources.Clear();
    }
}
