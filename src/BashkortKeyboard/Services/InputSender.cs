using System.Runtime.InteropServices;
using BashkortKeyboard.Interop;

namespace BashkortKeyboard.Services;

internal sealed class InputSender
{
    public bool Replace(char replacement)
    {
        // One batch: backspace down/up + Unicode down/up. Shift remains physical;
        // Unicode injection carries the already determined character/case.
        NativeMethods.Input Key(ushort vk, ushort scan, uint flags) => new()
        {
            Type = 1,
            Data = new() { Keyboard = new() { Vk = vk, Scan = scan, Flags = flags, Extra = NativeMethods.InjectionTag } }
        };
        NativeMethods.Input[] inputs = [Key(8, 0, 0), Key(8, 0, NativeMethods.KeyUp),
            Key(0, replacement, NativeMethods.Unicode), Key(0, replacement, NativeMethods.Unicode | NativeMethods.KeyUp)];
        return NativeMethods.SendInput(4, inputs, Marshal.SizeOf<NativeMethods.Input>()) == 4;
    }
}
