using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Scotch.Windows;

public sealed class StartupService
{
    private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly Func<bool> read;
    private readonly Action<bool> write;
    public StartupService():this(ReadRegistry,WriteRegistry) { }
    public StartupService(Func<bool> read,Action<bool> write){this.read=read;this.write=write;}
    public bool IsEnabled {get {try{return read();}catch(Exception e) when(IsExpected(e)){return false;}}}
    public bool TrySetEnabled(bool enabled,out string? error)
    {
        try {write(enabled);if(read()!=enabled)throw new IOException("Windows did not retain the sign-in setting.");error=null;return true;}
        catch(Exception e) when(IsExpected(e)){error=e.Message;return false;}
    }
    private static bool IsExpected(Exception e)=>e is UnauthorizedAccessException or SecurityException or IOException;
    private static string Command => "\""+(Environment.ProcessPath??throw new IOException("Unable to locate Scotch.exe."))+"\"";
    private static bool ReadRegistry()
    {
        using var key=Registry.CurrentUser.OpenSubKey(RunKey);
        return string.Equals(key?.GetValue("Scotch") as string,Command,StringComparison.OrdinalIgnoreCase);
    }
    private static void WriteRegistry(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(RunKey,true)??throw new IOException("Unable to open Windows sign-in settings.");
        if(enabled)key.SetValue("Scotch",Command,RegistryValueKind.String);else key.DeleteValue("Scotch",false);
    }
}
