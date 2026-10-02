using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BashkortKeyboard.Services;

internal static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static string ShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Bashkort Keyboard.lnk");

    public static void Apply(bool enabled)
    {
        if (enabled)
        {
            string executable = Path.Combine(AppContext.BaseDirectory, "BashkortKeyboard.exe");
            if (!File.Exists(executable)) throw new IOException("Не найден BashkortKeyboard.exe. Запустите собранное приложение.");
            Directory.CreateDirectory(Path.GetDirectoryName(ShortcutPath)!);
            object? shell = null, shortcut = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!;
                shell = Activator.CreateInstance(shellType)!;
                shortcut = ((dynamic)shell).CreateShortcut(ShortcutPath);
                dynamic link = shortcut;
                link.TargetPath = executable;
                link.Arguments = "--background";
                link.WorkingDirectory = AppContext.BaseDirectory;
                link.IconLocation = executable + ",0";
                link.Description = "Bashkort Keyboard — башкирский ввод";
                link.Save();
            }
            finally
            {
                if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
                if (shell is not null) Marshal.FinalReleaseComObject(shell);
            }
        }
        else if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
        // Migrate the previous implementation only after the shortcut is saved.
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue("BashkortKeyboard", throwOnMissingValue: false);
    }

    public static void MigrateLegacy(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        bool legacyExists = key?.GetValue("BashkortKeyboard") is not null;
        if (legacyExists) Apply(enabled);
    }
}
