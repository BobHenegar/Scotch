using System.ComponentModel;
using System.Runtime.InteropServices;
using Scotch.Core;

namespace Scotch.Windows;

public sealed class MonitorService
{
    private IReadOnlyList<DisplaySnapshot> displays=[];
    public HomePlacement ResolveHome(ClockSettings settings,SizeD size)
    {displays=ReadDisplays();return DisplayPlacement.Resolve(settings,size,displays);}
    public DisplaySnapshot GetDisplay(string id) => displays.FirstOrDefault(d=>d.Id==id)
        ??displays.First(d=>d.IsPrimary);
    public PointD? GetPointerDip(string monitorId)
    {
        if(!NativeMethods.GetCursorPos(out var point))return null;
        return GetDisplay(monitorId).ToDip(new(point.X,point.Y));
    }
    public PointD GetWindowDip(nint hwnd,string monitorId)
    {
        if(!NativeMethods.GetWindowRect(hwnd,out var rect))throw new Win32Exception();
        return GetDisplay(monitorId).ToDip(new(rect.Left,rect.Top));
    }
    public ClockSettings CaptureHome(nint hwnd,ClockSettings settings,SizeD size)
    {
        displays=ReadDisplays();
        var monitor=NativeMethods.MonitorFromWindow(hwnd,2);
        var info=ReadInfo(monitor);
        var display=GetDisplay(info.Device);
        return settings with {MonitorId=display.Id,Home=Geometry.Clamp(GetWindowDip(hwnd,display.Id),display.WorkAreaDip,size)};
    }
    public void Move(nint hwnd,string monitorId,PointD point)
    {
        var physical=GetDisplay(monitorId).ToPhysical(point);
        // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE; motion must never take focus.
        NativeMethods.SetWindowPos(hwnd,0,(int)Math.Round(physical.X),(int)Math.Round(physical.Y),0,0,0x1|0x4|0x10);
    }
    private static NativeMethods.MonitorInfo ReadInfo(nint monitor)
    {
        var info=new NativeMethods.MonitorInfo {Size=Marshal.SizeOf<NativeMethods.MonitorInfo>(),Device=string.Empty};
        if(!NativeMethods.GetMonitorInfo(monitor,ref info))throw new Win32Exception();
        return info;
    }
    private static IReadOnlyList<DisplaySnapshot> ReadDisplays()
    {
        var result=new List<DisplaySnapshot>();
        // Exceptions must not cross a native callback boundary.
        Exception? failure=null;
        NativeMethods.EnumDisplayMonitors(0,0,(monitor,_,_,_)=>
        {
            try
            {
                var info=ReadInfo(monitor);
                var scale=NativeMethods.GetDpiForMonitor(monitor,0,out var dpi,out _)==0?dpi/96d:1;
                var r=info.Work;
                result.Add(new(info.Device,(info.Flags&1)!=0,new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top),Math.Max(scale,0.5)));
                return true;
            }
            catch(Exception e){failure=e;return false;}
        },0);
        if(failure is not null)throw failure;
        if(result.Count==0)throw new InvalidOperationException("Windows did not report an available display.");
        return result;
    }
}
