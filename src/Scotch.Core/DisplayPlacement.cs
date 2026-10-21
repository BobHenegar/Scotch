namespace Scotch.Core;

public sealed record DisplaySnapshot(string Id,bool IsPrimary,RectD PhysicalWorkArea,double Scale)
{
    public RectD WorkAreaDip => new(0,0,PhysicalWorkArea.Width/Scale,PhysicalWorkArea.Height/Scale);
    public PointD ToDip(PointD physical) => new((physical.X-PhysicalWorkArea.X)/Scale,(physical.Y-PhysicalWorkArea.Y)/Scale);
    public PointD ToPhysical(PointD dip) => new(PhysicalWorkArea.X+dip.X*Scale,PhysicalWorkArea.Y+dip.Y*Scale);
}
public readonly record struct HomePlacement(string MonitorId,PointD Home,RectD WorkArea);
public static class DisplayPlacement
{
    public static HomePlacement Resolve(ClockSettings settings,SizeD size,IReadOnlyList<DisplaySnapshot> displays)
    {
        if(displays.Count==0)throw new ArgumentException("At least one display is required.",nameof(displays));
        var saved=displays.FirstOrDefault(x=>x.Id==settings.MonitorId);
        var selected=saved??displays.FirstOrDefault(x=>x.IsPrimary)??displays[0];
        var area=selected.WorkAreaDip;
        var home=saved is not null && settings is {IsValid:true,Home:not null}
            ?Geometry.Clamp(settings.Home.Value,area,size):Geometry.DefaultHome(area,size);
        return new(selected.Id,home,area);
    }
}
