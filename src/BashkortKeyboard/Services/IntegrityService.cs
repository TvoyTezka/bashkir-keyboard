using System.Runtime.InteropServices;
using BashkortKeyboard.Interop;

namespace BashkortKeyboard.Services;

internal static class IntegrityService
{
    private static readonly int Self = Level(NativeMethods.GetCurrentProcess());
    public static bool CanInject(nint window)
    {
        NativeMethods.GetWindowThreadProcessId(window, out var pid);
        var process = NativeMethods.OpenProcess(0x1000, false, pid);
        if (process == 0) return false;
        try { var target = Level(process); return Self >= 0 && target >= 0 && target <= Self; }
        finally { NativeMethods.CloseHandle(process); }
    }
    private static int Level(nint process)
    {
        if (!NativeMethods.OpenProcessToken(process, 8, out var token)) return -1;
        nint buffer = 0;
        try
        {
            NativeMethods.GetTokenInformation(token, 25, 0, 0, out int size);
            if (size <= 0 || size > 65536) return -1;
            buffer = Marshal.AllocHGlobal(size);
            if (!NativeMethods.GetTokenInformation(token, 25, buffer, size, out _)) return -1;
            var sid = Marshal.ReadIntPtr(buffer);
            byte count = Marshal.ReadByte(NativeMethods.GetSidSubAuthorityCount(sid));
            return count == 0 ? -1 : Marshal.ReadInt32(NativeMethods.GetSidSubAuthority(sid, (uint)(count - 1)));
        }
        finally { if (buffer != 0) Marshal.FreeHGlobal(buffer); NativeMethods.CloseHandle(token); }
    }
}
