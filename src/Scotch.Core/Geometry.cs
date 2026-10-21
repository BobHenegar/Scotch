namespace Scotch.Core;

public readonly record struct PointD(double X, double Y)
{
    public double DistanceSquared(PointD other) => (X-other.X)*(X-other.X)+(Y-other.Y)*(Y-other.Y);
    public static PointD Lerp(PointD from, PointD to, double amount) => new(from.X+(to.X-from.X)*amount,from.Y+(to.Y-from.Y)*amount);
}
public readonly record struct SizeD(double Width, double Height);
public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Right => X+Width;
    public double Bottom => Y+Height;
    public RectD Inflate(double amount) => new(X-amount,Y-amount,Width+amount*2,Height+amount*2);
    public bool Contains(PointD p) => p.X>=X && p.X<=Right && p.Y>=Y && p.Y<=Bottom;
    public bool Contains(RectD r) => r.X>=X && r.Right<=Right && r.Y>=Y && r.Bottom<=Bottom;
}
public static class Geometry
{
    public static PointD Clamp(PointD point, RectD area, SizeD size) => new(
        Math.Clamp(point.X,area.X,Math.Max(area.X,area.Right-size.Width)),
        Math.Clamp(point.Y,area.Y,Math.Max(area.Y,area.Bottom-size.Height)));
    public static PointD DefaultHome(RectD area, SizeD size) => Clamp(new(area.Right-size.Width-16,area.Y+16),area,size);
    public static bool IsNear(PointD position, SizeD size, PointD pointer) => new RectD(position.X,position.Y,size.Width,size.Height).Inflate(48).Contains(pointer);

    // Moving the window along a segment is equivalent to moving its origin
    // against this pointer-centered obstacle (window size plus avoidance margin).
    public static bool PathIsClear(PointD from, PointD to, PointD pointer, SizeD size)
    {
        var obstacle=new RectD(pointer.X-size.Width-48,pointer.Y-size.Height-48,size.Width+96,size.Height+96);
        double enter=0,exit=1;
        return !Clip(from.X,to.X-from.X,obstacle.X,obstacle.Right,ref enter,ref exit)
            || !Clip(from.Y,to.Y-from.Y,obstacle.Y,obstacle.Bottom,ref enter,ref exit);
    }
    private static bool Clip(double start,double delta,double min,double max,ref double enter,ref double exit)
    {
        if(Math.Abs(delta)<1e-9) return start>=min && start<=max;
        var a=(min-start)/delta;var b=(max-start)/delta;
        if(a>b)(a,b)=(b,a);
        enter=Math.Max(enter,a);exit=Math.Min(exit,b);
        return enter<=exit;
    }
}
