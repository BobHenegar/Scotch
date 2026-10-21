using System.Globalization;
using System.Windows.Media;
using Scotch;
using Scotch.Windows;

namespace Scotch.Windows.Tests;

public class WindowsIntegrationTests
{
    [Fact] public void Shell_is_topmost_nonactivating_and_off_taskbar()
    {
        Sta.Run(()=>
        {
            var window=new ClockWindow();
            Assert.True(window.Topmost);
            Assert.False(window.ShowActivated);
            Assert.False(window.ShowInTaskbar);
            Assert.Equal(System.Windows.WindowStyle.None,window.WindowStyle);
            window.Close();
        });
    }
    [Fact] public void Enabled_clock_passes_clicks_through_and_paused_clock_accepts_them()
    {
        Sta.Run(()=>
        {
            var window=new ClockWindow();
            try
            {
                window.Show();
                var hwnd=new System.Windows.Interop.WindowInteropHelper(window).Handle;
                Assert.True((GetWindowLongPtr(hwnd,-20).ToInt64()&0x20)!=0);
                Assert.True((GetWindowLongPtr(hwnd,-20).ToInt64()&0x08000000)!=0);
                SendMessage(hwnd,0x312,HotkeyService.Id,0);
                Assert.True((GetWindowLongPtr(hwnd,-20).ToInt64()&0x20)==0);
                Assert.True((GetWindowLongPtr(hwnd,-20).ToInt64()&0x08000000)==0);
            }
            finally {window.Close();}
        });
    }
    [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint SendMessage(nint hwnd,uint message,nint wParam,nint lParam);

    [Fact] public void Palette_uses_light_dark_accent_and_contrast_colors()
    {
        var accent=Colors.CornflowerBlue;
        var light=ThemeService.Select(false,false,accent,Colors.Yellow,Colors.Black);
        var dark=ThemeService.Select(true,false,accent,Colors.Yellow,Colors.Black);
        Assert.True(light.Background.R>dark.Background.R);
        Assert.Equal(accent,dark.Accent);
        var contrast=ThemeService.Select(true,true,accent,Colors.Yellow,Colors.Black);
        Assert.Equal(Colors.Yellow,contrast.Foreground);
        Assert.Equal(Colors.Black,contrast.Background);
        Assert.Equal(255,contrast.Background.A);
    }
    [Fact] public void Clock_text_follows_regional_format_and_date_toggle()
    {
        var date=new DateTime(2026,10,21,16,25,30);
        Assert.Equal("16:25",ClockText.Format(date,CultureInfo.GetCultureInfo("en-GB"),false).Time);
        Assert.Equal("4:25 PM",ClockText.Format(date,CultureInfo.GetCultureInfo("en-US"),false).Time);
        Assert.Equal("",ClockText.Format(date,CultureInfo.GetCultureInfo("en-US"),false).Date);
        Assert.NotEmpty(ClockText.Format(date,CultureInfo.GetCultureInfo("en-US"),true).Date);
    }
    [Fact] public void Hotkey_requests_no_repeat_and_failed_registration_does_not_emit_toggle()
    {
        uint observedFlags=0;
        using var service=new HotkeyService((_,_,flags,_)=>{observedFlags=flags;return false;},(_,_)=>true);
        var toggles=0;service.ToggleRequested+=()=>toggles++;
        Assert.False(service.TryRegister(1,out var error));
        Assert.NotEmpty(error!);
        Assert.True((observedFlags&0x4000)!=0);
        service.ProcessMessage(0x312,HotkeyService.Id);
        Assert.Equal(0,toggles);
    }
    [Fact] public void Hotkey_emits_once_for_each_registered_message_and_releases_registration()
    {
        var released=false;
        var service=new HotkeyService((_,_,_,_)=>true,(_,_)=>{released=true;return true;});
        var toggles=0;service.ToggleRequested+=()=>toggles++;
        Assert.True(service.TryRegister(1,out _));
        Assert.False(service.ProcessMessage(0x312,99));
        Assert.True(service.ProcessMessage(0x312,HotkeyService.Id));
        Assert.Equal(1,toggles);
        service.Dispose();Assert.True(released);
    }
    [Fact] public void Startup_failure_leaves_actual_enabled_state_unchanged()
    {
        var current=false;
        var service=new StartupService(()=>current,_=>throw new UnauthorizedAccessException("Denied"));
        Assert.False(service.TrySetEnabled(true,out var error));
        Assert.Contains("Denied",error);
        Assert.False(service.IsEnabled);
    }
    [Fact] public void Startup_success_updates_actual_state()
    {
        var current=false;
        var service=new StartupService(()=>current,value=>current=value);
        Assert.True(service.TrySetEnabled(true,out _));Assert.True(service.IsEnabled);
        Assert.True(service.TrySetEnabled(false,out _));Assert.False(service.IsEnabled);
    }
    [Fact] public void Explorer_restart_restores_tray_and_toggle_stays_available_after_hotkey_failure()
    {
        Sta.Run(()=>
        {
            var toggles=0;
            using var tray=new TrayService(()=>toggles++,()=>{},()=>{},()=>{},()=>{});
            tray.Refresh(false,false,false);
            ((System.Windows.Forms.ToolStripMenuItem)tray.Menu.Items[0]).PerformClick();
            Assert.Equal(1,toggles);
            Assert.True(tray.RestoreIcon());
        });
    }
}

internal static class Sta
{
    public static void Run(Action action)
    {
        Exception? error=null;
        var thread=new Thread(()=>{try{action();}catch(Exception e){error=e;}});
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();thread.Join();
        if(error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
