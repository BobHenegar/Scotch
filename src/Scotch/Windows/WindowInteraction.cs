using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Scotch.Windows;

internal static class WindowInteraction
{
    private const int ExtendedStyle=-20;
    private const long ClickThrough=0x20,NoActivate=0x08000000,ToolWindow=0x80;
    public static void SetAvoidance(nint hwnd,bool enabled)
    {
        var current=(nint.Size==8?GetWindowLongPtr(hwnd,ExtendedStyle):GetWindowLong(hwnd,ExtendedStyle)).ToInt64();
        var next=(enabled?current|ClickThrough|NoActivate:current&~(ClickThrough|NoActivate))|ToolWindow;
        if(current==next)return;
        Marshal.SetLastPInvokeError(0);
        var previous=nint.Size==8?SetWindowLongPtr(hwnd,ExtendedStyle,(nint)next):(nint)SetWindowLong(hwnd,ExtendedStyle,(int)next);
        var error=Marshal.GetLastPInvokeError();
        if(previous==0 && error!=0)throw new Win32Exception(error);
        NativeMethods.SetWindowPos(hwnd,0,0,0,0,0,0x1|0x2|0x4|0x10|0x20);
    }
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)] private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW",SetLastError=true)] private static extern int GetWindowLong(nint hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] private static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("user32.dll",EntryPoint="SetWindowLongW",SetLastError=true)] private static extern int SetWindowLong(nint hwnd,int index,int value);
}
