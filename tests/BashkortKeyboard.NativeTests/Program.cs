using System.Runtime.InteropServices;
using BashkortKeyboard.Core;
using BashkortKeyboard.Interop;
using BashkortKeyboard.Services;

int count = 0;
void Assert(bool value, string description)
{
    if (!value) { Console.Error.WriteLine($"FAIL {description}"); Environment.Exit(1); }
    count++;
}
Assert(Marshal.SizeOf<NativeMethods.Input>() == (IntPtr.Size == 8 ? 40 : 28), "SendInput struct size");
Assert(Marshal.SizeOf<NativeMethods.KeyboardHook>() == (IntPtr.Size == 8 ? 24 : 20), "keyboard hook struct size");
Assert(Marshal.SizeOf<NativeMethods.GuiThreadInfo>() == (IntPtr.Size == 8 ? 72 : 48), "GUI thread struct size");
int size = GetKeyboardLayoutList(0, null);
var layouts = new nint[size]; GetKeyboardLayoutList(size, layouts);
var ru = layouts.FirstOrDefault(KeyboardLayoutService.IsRussian);
bool loadedForTest = false;
if (ru == 0 && args.Contains("--load-test-layout"))
{
    // Test-process only; do not activate it or notify the shell.
    ru = LoadKeyboardLayout("00000419", 0x80);
    loadedForTest = ru != 0;
}
if (ru == 0) { Console.Error.WriteLine("SKIP native translation: Russian layout not installed"); Environment.Exit(2); }
var service = new KeyboardLayoutService();
foreach (var mapping in Mapping.All)
foreach (bool shift in new[] { false, true })
foreach (bool caps in new[] { false, true })
{
    char expected = shift ^ caps ? char.ToUpperInvariant(mapping.Russian) : mapping.Russian;
    uint scan = MapVirtualKeyEx((uint)mapping.VirtualKey, 0, ru);
    Assert(service.Translate((uint)mapping.VirtualKey, scan, ru, shift, caps) == expected, "RU mapping / Shift / Caps");
}
var en = layouts.FirstOrDefault(layout => ((long)layout & 0xFFFF) == 0x0409);
if (en != 0)
{
    Assert(!KeyboardLayoutService.IsRussian(en), "EN layout rejected");
    Assert(service.Translate(0x4A, MapVirtualKeyEx(0x4A, 0, en), en, false, false) == 'j', "EN ordinary J unchanged");
}
Assert(!IntegrityService.CanInject(0), "unknown window rejected");
var gui = new NativeMethods.GuiThreadInfo { Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
if (NativeMethods.GetGUIThreadInfo(0, ref gui) && gui.Focus != 0 && (gui.Flags & 0x1E) == 0)
    Assert(service.Current().Focus == gui.Focus, "active focus retained, including blinking caret");
Console.WriteLine($"PASS {count} native assertions (real WinAPI, no keyboard input injected).");
if (loadedForTest) UnloadKeyboardLayout(ru);

[DllImport("user32.dll")] static extern int GetKeyboardLayoutList(int count, [Out] nint[]? list);
[DllImport("user32.dll")] static extern uint MapVirtualKeyEx(uint code, uint mapType, nint layout);
[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern nint LoadKeyboardLayout(string id, uint flags);
[DllImport("user32.dll")] static extern bool UnloadKeyboardLayout(nint layout);
