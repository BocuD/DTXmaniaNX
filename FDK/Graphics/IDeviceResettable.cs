namespace FDK;

public interface IDeviceResettable
{
    void OnDeviceLost();
    void OnDeviceReset();
}