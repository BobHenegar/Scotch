namespace Scotch.Core;

public sealed class MovementController(PointD home, PointD position)
{
    public PointD Position {get;private set;}=position;
    public PointD Home {get;private set;}=home;
    public bool Enabled {get;private set;}=true;
    private PointD? target;
    private PointD from;
    private double progress;
    private double clearTime;
    private bool returning;

    public void SetEnabled(bool enabled){Enabled=enabled;target=null;clearTime=0;returning=false;}
    public void SetHome(PointD home){Home=home;target=null;clearTime=0;returning=false;}
    public void Reset(PointD home){SetHome(home);Position=home;}

    public PointD Tick(TimeSpan elapsed,PointD pointer,SizeD size,RectD workArea,bool animationsEnabled)
    {
        if(!Enabled)return Position;
        Position=Geometry.Clamp(Position,workArea,size);
        Home=Geometry.Clamp(Home,workArea,size);
        var dt=Math.Clamp(elapsed.TotalSeconds,0,0.1);
        var near=Geometry.IsNear(Position,size,pointer);
        if(near)
        {
            clearTime=0;
            if(returning || target is null || Geometry.IsNear(target.Value,size,pointer))
                Start(FindSafe(pointer,size,workArea),false);
        }
        else if(target is not null && !returning)
        {
            if(Geometry.IsNear(target.Value,size,pointer)) Start(FindSafe(pointer,size,workArea),false);
        }
        else
        {
            clearTime=Geometry.PathIsClear(Position,Home,pointer,size)?clearTime+dt:0;
            if(returning && !Geometry.PathIsClear(Position,Home,pointer,size))
            {target=null;returning=false;clearTime=0;}
            if(target is null && clearTime>=1 && Position!=Home && Geometry.PathIsClear(Position,Home,pointer,size)) Start(Home,true);
        }
        if(target is not null)
        {
            progress+=dt/(returning?2:0.2);
            if(!animationsEnabled || progress>=1)
            {Position=target.Value;target=null;returning=false;}
            else
            {
                var t=progress*progress*(3-2*progress);
                Position=PointD.Lerp(from,target.Value,t);
            }
        }
        return Position;
    }
    private void Start(PointD destination,bool isReturn)
    {from=Position;target=destination;returning=isReturn;progress=0;}

    private PointD FindSafe(PointD pointer,SizeD size,RectD area)
    {
        PointD[] candidates=[
            new(pointer.X-size.Width-49,Position.Y),new(pointer.X+49,Position.Y),
            new(Position.X,pointer.Y-size.Height-49),new(Position.X,pointer.Y+49),
            new(area.X,area.Y),new(area.Right-size.Width,area.Y),
            new(area.X,area.Bottom-size.Height),new(area.Right-size.Width,area.Bottom-size.Height)];
        var bounded=candidates.Select(p=>Geometry.Clamp(p,area,size)).Distinct().ToArray();
        var safe=bounded.Where(p=>!Geometry.IsNear(p,size,pointer)).OrderBy(p=>p.DistanceSquared(Position)).ToArray();
        if(safe.Length>0)return safe[0];
        // No position can provide full clearance on a very small work area.
        return bounded.OrderByDescending(p=>DistanceFromWindow(p,pointer,size)).First();
    }
    private static double DistanceFromWindow(PointD origin,PointD pointer,SizeD size)
    {
        var closest=new PointD(Math.Clamp(pointer.X,origin.X,origin.X+size.Width),Math.Clamp(pointer.Y,origin.Y,origin.Y+size.Height));
        return closest.DistanceSquared(pointer);
    }
}
