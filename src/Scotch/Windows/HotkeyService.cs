using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Scotch.Windows;

public sealed class HotkeyService : IDisposable
{
    public const int Id=0x5343;
    private readonly Func<nint,int,uint,uint,bool> register;
    private readonly Func<nint,int,bool> unregister;
    private nint window;
    private bool registered;
    public event Action? ToggleRequested;
    public HotkeyService():this(RegisterHotKey,UnregisterHotKey) { }
    public HotkeyService(Func<nint,int,uint,uint,bool> register,Func<nint,int,bool> unregister)
    {this.register=register;this.unregister=unregister;}
    public bool TryRegister(nint hwnd,out string? error)
    {
        Dispose();window=hwnd;
        registered=register(hwnd,Id,0x2|0x1|0x4000,0x43); // Ctrl+Alt+C, MOD_NOREPEAT
        error=registered?null:"Ctrl+Alt+C could not be registered. Another app may be using it. Use the tray menu to toggle avoidance. ("+new Win32Exception(Marshal.GetLastWin32Error()).Message+")";
        return registered;
    }
    public bool ProcessMessage(uint message,nint wParam)
    {
        if(!registered || message!=0x312 || wParam!=Id)return false;
        ToggleRequested?.Invoke();return true;
    }
    public void Dispose(){if(registered)unregister(window,Id);registered=false;}
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterHotKey(nint window,int id,uint flags,uint key);
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterHotKey(nint window,int id);
}
