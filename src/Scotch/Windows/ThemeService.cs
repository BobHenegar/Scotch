using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Scotch.Windows;

public readonly record struct ClockPalette(Color Background,Color Foreground,Color Accent,bool Opaque);
public sealed class ThemeService : IDisposable
{
    public event Action? Changed;
    public ThemeService(){SystemParameters.StaticPropertyChanged+=OnSystemChange;}
    public ClockPalette Read()
    {
        var dark=false;
        try
        {
            using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            dark=key?.GetValue("AppsUseLightTheme") is int value && value==0;
        }
        catch(Exception e) when(e is SecurityException or UnauthorizedAccessException or System.IO.IOException) { }
        var accent=SystemColors.HighlightColor;
        if(DwmGetColorizationColor(out var argb,out _)==0)
            accent=Color.FromRgb((byte)(argb>>16),(byte)(argb>>8),(byte)argb);
        return Select(dark,SystemParameters.HighContrast,accent,SystemColors.WindowTextColor,SystemColors.WindowColor);
    }
    public static ClockPalette Select(bool dark,bool contrast,Color accent,Color systemForeground,Color systemBackground)
        => contrast?new(systemBackground,systemForeground,systemForeground,true)
        :new(Color.FromArgb(238,dark?(byte)30:(byte)248,dark?(byte)30:(byte)248,dark?(byte)30:(byte)248),dark?Colors.White:Color.FromRgb(24,24,24),accent,false);
    public void NotifyChanged()=>Changed?.Invoke();
    private void OnSystemChange(object? sender,PropertyChangedEventArgs e)=>NotifyChanged();
    public void Dispose()=>SystemParameters.StaticPropertyChanged-=OnSystemChange;
    [DllImport("dwmapi.dll")] private static extern int DwmGetColorizationColor(out uint color,[MarshalAs(UnmanagedType.Bool)] out bool opaque);
}
