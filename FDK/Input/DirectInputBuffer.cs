using System.Runtime.InteropServices;

namespace FDK;

internal static unsafe class DirectInputBuffer
{
    private const int AcquireSlot = 7;
    private const int PollSlot = 25;
    
    public static bool Ready(IntPtr device)
    {
        if (device == IntPtr.Zero)
        {
            return false;
        }

        void** vtable = *(void***)device;

        var acquire = (delegate* unmanaged[Stdcall]<IntPtr, int>)vtable[AcquireSlot];

        //S_FALSE means it was already acquired, which is the usual answer
        int result = acquire(device);

        if (result < 0)
        {
            return false;
        }

        var poll = (delegate* unmanaged[Stdcall]<IntPtr, int>)vtable[PollSlot];

        return poll(device) >= 0;
    }
}
