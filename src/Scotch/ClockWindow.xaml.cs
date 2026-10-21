using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Scotch.Core;
using Geometry=Scotch.Core.Geometry;
using Scotch.Windows;

namespace Scotch;

public partial class ClockWindow : Window
{
    private readonly SettingsStore store=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Scotch","settings.json"));
    private readonly MonitorService monitors=new();
    private readonly StartupService startup=new();
    private readonly HotkeyService hotkey=new();
    private readonly ThemeService theme=new();
    private readonly RegionalFormatService regionalFormat=new();
    private readonly Stopwatch elapsed=Stopwatch.StartNew();
    private readonly DispatcherTimer timer;
    private ClockSettings settings;
    private MovementController? movement;
    private HomePlacement placement;
    private TrayService? tray;
    private HwndSource? source;
    private nint hwnd;
    private bool closed,dragging,placementQueued;
    private string lastClockText=string.Empty;
    private long lastSecond=-1;
    private readonly uint taskbarCreated=RegisterWindowMessage("TaskbarCreated");

    public ClockWindow()
    {
        settings=store.Load();
        InitializeComponent();
        timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(33)};
        timer.Tick+=OnTick;
        hotkey.ToggleRequested+=ToggleAvoidance;
        theme.Changed+=OnThemeChanged;
        SourceInitialized+=OnSourceInitialized;
        SizeChanged+=(_,_)=>QueuePlacement(false);
        Closed+=OnClosed;
        UpdateClock();ApplyTheme();
        Measure(new System.Windows.Size(double.PositiveInfinity,double.PositiveInfinity));
    }
    private SizeD ClockSize => new(ActualWidth>0?ActualWidth:Math.Max(DesiredSize.Width,150),ActualHeight>0?ActualHeight:Math.Max(DesiredSize.Height,52));
    private void OnSourceInitialized(object? sender,EventArgs e)
    {
        hwnd=new WindowInteropHelper(this).Handle;
        source=HwndSource.FromHwnd(hwnd);source?.AddHook(WindowMessage);
        tray=new TrayService(ToggleAvoidance,ToggleDate,ResetPosition,ToggleStartup,Close);
        tray.Menu.Opening+=(_,_)=>RefreshControls();
        RefreshPlacement(true);ApplyTheme();RefreshControls();
        if(!hotkey.TryRegister(hwnd,out var error))
            Dispatcher.BeginInvoke(new Action(()=>tray?.Notify(error!)),DispatcherPriority.ApplicationIdle);
        elapsed.Restart();timer.Start();
    }
    private void OnTick(object? sender,EventArgs e)
    {
        var dt=elapsed.Elapsed;elapsed.Restart();
        var second=DateTime.Now.Ticks/TimeSpan.TicksPerSecond;
        if(second!=lastSecond){lastSecond=second;UpdateClock();}
        if(movement is not {Enabled:true} || dragging || hwnd==0)return;
        var pointer=monitors.GetPointerDip(placement.MonitorId);
        if(pointer is null)return; // Secure desktop/unavailable pointer: hold position.
        var before=movement.Position;
        var next=movement.Tick(dt,pointer.Value,ClockSize,placement.WorkArea,SystemParameters.ClientAreaAnimation);
        if(next!=before)monitors.Move(hwnd,placement.MonitorId,next);
    }
    private void UpdateClock()
    {
        var text=ClockText.Format(DateTime.Now,regionalFormat.Culture,settings.ShowDate);
        if(text.Time==lastClockText && DateLabel.Text==text.Date)return;
        lastClockText=text.Time;TimeLabel.Text=text.Time;DateLabel.Text=text.Date;
        DateLabel.Visibility=settings.ShowDate?Visibility.Visible:Visibility.Collapsed;
    }
    private void RefreshPlacement(bool resetCurrent)
    {
        if(hwnd==0 || dragging || closed)return;
        var previous=movement;
        var resolved=monitors.ResolveHome(settings,ClockSize);
        var position=!resetCurrent && previous is not null && placement.MonitorId==resolved.MonitorId
            ?Geometry.Clamp(previous.Position,resolved.WorkArea,ClockSize):resolved.Home;
        placement=resolved;
        movement=new(resolved.Home,position);
        movement.SetEnabled(previous?.Enabled??true);
        monitors.Move(hwnd,placement.MonitorId,position);
    }
    private void QueuePlacement(bool resetCurrent)
    {
        if(hwnd==0 || closed || dragging)return;
        // Display and size messages arrive in bursts; apply after WPF finishes DPI/layout work.
        resetPlacementRequested|=resetCurrent;
        if(placementQueued)return;
        placementQueued=true;
        Dispatcher.BeginInvoke(new Action(()=>
        {
            placementQueued=false;
            var reset=resetPlacementRequested;resetPlacementRequested=false;
            try {RefreshPlacement(reset);}
            catch(Exception error) when(error is Win32Exception or InvalidOperationException)
            {tray?.Notify("Unable to refresh display position: "+error.Message);}
        }),DispatcherPriority.Loaded);
    }
    private bool resetPlacementRequested;
    private void ToggleAvoidance()
    {
        if(movement is null)return;
        movement.SetEnabled(!movement.Enabled);
        elapsed.Restart();timer.Interval=TimeSpan.FromMilliseconds(movement.Enabled?33:1000);
        RefreshControls();
    }
    private void ToggleDate(){settings=settings with {ShowDate=!settings.ShowDate};Save();UpdateClock();RefreshControls();}
    private void ResetPosition()
    {
        settings=settings with {MonitorId=null,Home=null};Save();
        RefreshPlacement(true);RefreshControls();
    }
    private void ToggleStartup()
    {
        if(!startup.TrySetEnabled(!startup.IsEnabled,out var error))tray?.Notify("Could not change launch at sign-in: "+error);
        RefreshControls();
    }
    private void Save(){if(!store.TrySave(settings,out var error))tray?.Notify("Could not save preferences: "+error);}
    private void RefreshControls()
    {
        var enabled=movement?.Enabled??true;
        if(hwnd!=0)WindowInteraction.SetAvoidance(hwnd,enabled);
        tray?.Refresh(enabled,settings.ShowDate,startup.IsEnabled);
        var palette=theme.Read();var accent=new SolidColorBrush(palette.Accent);
        StatusDot.Fill=enabled?accent:Brushes.Transparent;StatusDot.Stroke=accent;
        ToolTip=enabled?"Avoidance on · Ctrl+Alt+C to pause":"Avoidance paused · Drag to set home · Ctrl+Alt+C to resume";
        System.Windows.Automation.AutomationProperties.SetName(this,"Scotch clock. "+(enabled?"Avoidance on.":"Avoidance paused."));
    }
    private void OnThemeChanged()
    {
        if(closed || Dispatcher.HasShutdownStarted)return;
        Dispatcher.BeginInvoke(new Action(()=>{if(!closed)ApplyTheme();}));
    }
    private void ApplyTheme()
    {
        var palette=theme.Read();
        Face.Background=new SolidColorBrush(palette.Background);Face.BorderBrush=new SolidColorBrush(palette.Accent);
        TimeLabel.Foreground=new SolidColorBrush(palette.Foreground);DateLabel.Foreground=TimeLabel.Foreground;
        tray?.ApplyPalette(palette);RefreshControls();
    }
    private void OnDrag(object sender,MouseButtonEventArgs e)
    {
        if(movement is not {Enabled:false} || e.ChangedButton!=MouseButton.Left)return;
        dragging=true;
        try
        {
            DragMove();
            settings=monitors.CaptureHome(hwnd,settings,ClockSize);Save();
        }
        catch(InvalidOperationException) { }
        finally {dragging=false;RefreshPlacement(true);}
        e.Handled=true;
    }
    private void OnContextMenu(object sender,MouseButtonEventArgs e)
    {if(movement is {Enabled:false}){tray?.ShowAtPointer();e.Handled=true;}}
    private nint WindowMessage(nint window,int message,nint wParam,nint lParam,ref bool handled)
    {
        if(hotkey.ProcessMessage((uint)message,wParam)){handled=true;return 0;}
        if(taskbarCreated!=0 && (uint)message==taskbarCreated){tray?.RestoreIcon();return 0;}
        switch(message)
        {
            case 0x21 when movement is {Enabled:true}: handled=true;return 3; // WM_MOUSEACTIVATE: MA_NOACTIVATE
            case 0x1A: // WM_SETTINGCHANGE: theme, regional format, work area, animations
                regionalFormat.Refresh();lastClockText=string.Empty;lastSecond=-1;
                theme.NotifyChanged();QueuePlacement(false);break;
            case 0x31A: case 0x320: theme.NotifyChanged();break; // theme/accent
            case 0x7E: QueuePlacement(true);break; // WM_DISPLAYCHANGE
            case 0x2E0: QueuePlacement(false);break; // WM_DPICHANGED; WPF applies suggested bounds first
        }
        return 0;
    }
    private void OnClosed(object? sender,EventArgs e)
    {
        closed=true;timer.Stop();timer.Tick-=OnTick;
        source?.RemoveHook(WindowMessage);hotkey.Dispose();theme.Dispose();tray?.Dispose();
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
}
