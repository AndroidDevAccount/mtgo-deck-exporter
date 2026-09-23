using Microsoft.Win32;

namespace MtgoDeckExporter;

public static class FileAssociation
{
    private const string ProgId = "MtgoDeckExporter.dek";

    public static void Register()
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Could not locate the program executable.");
        using (var extension = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.dek"))
            extension.SetValue("", ProgId);
        using (var type = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            type.SetValue("", "Magic: The Gathering Online Deck");
        using (var icon = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            icon.SetValue("", $"\"{executable}\",0");
        using (var command = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            command.SetValue("", $"\"{executable}\" \"%1\"");
        NativeMethods.SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);
    }
}
