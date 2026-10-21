using System.Globalization;
using System.Windows.Media;
using Scotch;
using Scotch.Windows;

namespace Scotch.Windows.Tests;

public class WindowsIntegrationTests
{
    [Fact] public void Size_menu_selects_exactly_one_of_three_presets()
    {
        Sta.Run(()=>
        {
            var chosen=Scotch.Core.ClockSizePreset.Medium;
            using var tray=new TrayService(()=>{},()=>{},()=>{},()=>{},()=>{},size=>chosen=size);
            var menu=tray.Menu.Items.OfType<System.Windows.Forms.ToolStripMenuItem>().Single(x=>x.Text=="Size");
            Assert.Equal(3,menu.DropDownItems.Count);
            foreach(var preset in new[]{Scotch.Core.ClockSizePreset.Small,Scotch.Core.ClockSizePreset.Medium,Scotch.Core.ClockSizePreset.Large})
            {
                ((System.Windows.Forms.ToolStripMenuItem)menu.DropDownItems[(int)preset]).PerformClick();
                Assert.Equal(preset,chosen);
                tray.Refresh(true,false,false,preset);
                Assert.Single(menu.DropDownItems.OfType<System.Windows.Forms.ToolStripMenuItem>(),x=>x.Checked);
            }
        });
    }
    [Fact] public void Live_resize_stays_on_screen_and_preserves_avoidance_mode()
    {
        Sta.Run(()=>
        {
            var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Scotch","settings.json");
            var original=System.IO.File.Exists(path)?System.IO.File.ReadAllText(path):null;
            ClockWindow? window=null;
            try
            {
                var screen=System.Windows.Forms.Screen.PrimaryScreen!;
                var store=new Scotch.Core.SettingsStore(path);
                Assert.True(store.TrySave(new Scotch.Core.ClockSettings(MonitorId:screen.DeviceName,Home:new(999999,999999)),out _));
                window=new ClockWindow();window.Show();
                var hwnd=new System.Windows.Interop.WindowInteropHelper(window).Handle;
                foreach(var enabled in new[]{false,true})
                {
                    SendMessage(hwnd,0x312,HotkeyService.Id,0);
                    var widths=new List<int>();
                    foreach(var size in new[]{Scotch.Core.ClockSizePreset.Small,Scotch.Core.ClockSizePreset.Medium,Scotch.Core.ClockSizePreset.Large})
                    {
                        window.ApplySize(size);
                        var frame=new System.Windows.Threading.DispatcherFrame();
                        var timer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(150)};
                        timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};
                        timer.Start();System.Windows.Threading.Dispatcher.PushFrame(frame);
                        Assert.True(GetWindowRect(hwnd,out var bounds));
                        var area=System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
                        Assert.InRange(bounds.Left,area.Left,area.Right);
                        Assert.InRange(bounds.Top,area.Top,area.Bottom);
                        Assert.InRange(bounds.Right,area.Left,area.Right);
                        Assert.InRange(bounds.Bottom,area.Top,area.Bottom);
                        widths.Add(bounds.Right-bounds.Left);
                        Assert.Equal(enabled,(GetWindowLongPtr(hwnd,-20).ToInt64()&0x20)!=0);
                        Assert.Equal(size,store.Load().Size);
                    }
                    Assert.True(widths[0]<widths[1] && widths[1]<widths[2]);
                    Assert.InRange(Math.Abs(widths[0]-widths[1]*0.75),0,2);
                    Assert.InRange(Math.Abs(widths[2]-widths[1]*1.5),0,2);
                }
            }
            finally
            {
                window?.Close();
                if(original is null)System.IO.File.Delete(path);else System.IO.File.WriteAllText(path,original);
            }
        });
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeRect {public int Left,Top,Right,Bottom;}
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return:System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hwnd,out NativeRect rect);

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

    [Fact] public void Windows_regional_format_change_refreshes_visible_clock_without_restart()
    {
        Sta.Run(()=>
        {
            const uint userDefault=0x400,shortTime=0x79;
            var buffer=new System.Text.StringBuilder(256);
            Assert.True(GetLocaleInfo(userDefault,shortTime,buffer,buffer.Capacity)>0);
            var original=buffer.ToString();
            var window=new ClockWindow();
            try
            {
                window.Show(); // Populate the same culture cache used in a normal running clock.
                Assert.True(SetLocaleInfo(userDefault,shortTime,"HH':'mm' ScotchTest'"));
                var hwnd=new System.Windows.Interop.WindowInteropHelper(window).Handle;
                var name=System.Runtime.InteropServices.Marshal.StringToHGlobalUni("intl");
                try {SendMessage(hwnd,0x1A,0,name);}
                finally {System.Runtime.InteropServices.Marshal.FreeHGlobal(name);}
                var frame=new System.Windows.Threading.DispatcherFrame();
                var timer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(250)};
                timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};
                timer.Start();System.Windows.Threading.Dispatcher.PushFrame(frame);
                Assert.Contains("ScotchTest",((System.Windows.Controls.TextBlock)window.FindName("TimeLabel")).Text);
            }
            finally
            {
                Assert.True(SetLocaleInfo(userDefault,shortTime,original));
                System.Globalization.CultureInfo.CurrentCulture.ClearCachedData();
                window.Close();
            }
        });
    }
    [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int GetLocaleInfo(uint locale,uint type,System.Text.StringBuilder data,int length);
    [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]
    [return:System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetLocaleInfo(uint locale,uint type,string data);

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
