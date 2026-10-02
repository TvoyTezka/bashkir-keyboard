using System.Runtime.InteropServices;
using System.Text;
using BashkortKeyboard.Interop;

namespace BashkortKeyboard.Services;

internal sealed class KeyboardLayoutService
{
    public InputContext Current()
    {
        var window = NativeMethods.GetForegroundWindow();
        if (window == 0) return default;
        uint thread = NativeMethods.GetWindowThreadProcessId(window, out _);
        var info = new NativeMethods.GuiThreadInfo { Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
        // GUI_CARETBLINKING (bit 0) is normal in an editable field. Only menu /
        // move-size modes invalidate the typing context.
        if (!NativeMethods.GetGUIThreadInfo(thread, ref info) || info.Focus == 0 || (info.Flags & 0x1E) != 0) return default;
        var focusThread = NativeMethods.GetWindowThreadProcessId(info.Focus, out _);
        return new(window, info.Focus, NativeMethods.GetKeyboardLayout(focusThread));
    }
    public static bool IsRussian(nint layout) => ((long)layout & 0xFFFF) == 0x0419;
    public char Translate(uint vk, uint scan, nint layout, bool shift, bool caps)
    {
        // An explicit state avoids stale GetKeyboardState on our own thread.
        var state = new byte[256];
        state[vk] = 0x80; state[0x10] = shift ? (byte)0x80 : (byte)0;
        state[0x14] = caps ? (byte)1 : (byte)0;
        var result = new StringBuilder(8);
        // Flag 4 does not disturb dead-key state (Windows 10 1607+).
        return NativeMethods.ToUnicodeEx(vk, scan, state, result, result.Capacity, 4, layout) == 1 ? result[0] : '\0';
    }
    public static bool Down(int key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;
    public static bool ShortcutDown() => Down(0x11) || Down(0x12) || Down(0x5B) || Down(0x5C);
}
