using Scotch.Core;

namespace Scotch.Core.Tests;

public class DisplayPlacementTests
{
    [Fact] public void Missing_monitor_falls_back_to_primary_default_home()
    {
        DisplaySnapshot[] displays=[new("one",true,new(0,0,1920,1080),1)];
        var home=DisplayPlacement.Resolve(new(false,"removed",new(0,0)),new(160,60),displays);
        Assert.Equal("one",home.MonitorId);
        Assert.Equal(new PointD(1744,16),home.Home);
    }
    [Theory] [InlineData(1)] [InlineData(1.5)] [InlineData(2)]
    public void Physical_coordinates_map_to_monitor_relative_DIP(double scale)
    {
        var display=new DisplaySnapshot("two",false,new(-1920,-200,1920*scale,1080*scale),scale);
        Assert.Equal(new PointD(48,48),display.ToDip(new(-1920+48*scale,-200+48*scale)));
        Assert.Equal(new PointD(-1920+160*scale,-200+60*scale),display.ToPhysical(new(160,60)));
        var home=DisplayPlacement.Resolve(new(false,"two",null),new(160,60),[display]);
        Assert.Equal(new PointD(1744,16),home.Home);
    }
    [Fact] public void Saved_home_and_larger_clock_are_clamped()
    {
        DisplaySnapshot[] displays=[new("one",true,new(-1920,0,1920,1080),1)];
        var home=DisplayPlacement.Resolve(new(false,"one",new(1800,1000)),new(300,100),displays);
        Assert.Equal(new PointD(1620,980),home.Home);
    }
}
