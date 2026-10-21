using Scotch.Core;

namespace Scotch.Core.Tests;

public class MovementControllerTests
{
    private static readonly SizeD Size = new(160, 60);
    private static readonly RectD Area = new(0, 0, 1920, 1080);
    private static readonly PointD Far = new(100, 900);
    private static PointD Tick(MovementController c, PointD pointer, int frames = 90, RectD? area = null, bool animations = true)
    {
        var result = c.Position;
        for (var i = 0; i < frames; i++) result = c.Tick(TimeSpan.FromSeconds(1d / 30), pointer, Size, area ?? Area, animations);
        return result;
    }

    [Fact] public void Default_home_respects_padding() => Assert.Equal(new PointD(1744, 16), Geometry.DefaultHome(Area, Size));
    [Fact] public void Corner_yield_is_bounded_and_clear()
    {
        var c = new MovementController(new(1744,16), new(1744,16));
        var pointer = new PointD(1820, 30);
        var result = Tick(c, pointer);
        Assert.True(Area.Contains(new RectD(result.X,result.Y,Size.Width,Size.Height)));
        Assert.False(new RectD(result.X,result.Y,Size.Width,Size.Height).Inflate(48).Contains(pointer));
    }
    [Fact] public void Disabling_freezes_and_enabling_resumes()
    {
        var home = new PointD(1744,16);
        var c = new MovementController(home,home);
        c.SetEnabled(false);
        Assert.Equal(home,Tick(c,home));
        c.SetEnabled(true);
        Assert.NotEqual(home,Tick(c,home));
    }
    [Fact] public void Safe_target_stays_stable_with_pointer_parked_at_home()
    {
        var home = new PointD(1744,16);
        var c = new MovementController(home,home);
        var safe = Tick(c, home);
        Assert.Equal(safe,Tick(c,home,300));
    }
    [Fact] public void Return_waits_a_second_then_finishes_in_two()
    {
        var home = new PointD(1744,16);
        var c = new MovementController(home,home);
        var safe = Tick(c, home);
        Assert.Equal(safe,Tick(c,Far,29));
        Assert.NotEqual(safe,Tick(c,Far,16));
        Assert.Equal(home,Tick(c,Far,65));
    }
    [Fact] public void Pointer_on_return_path_interrupts_return()
    {
        var home = new PointD(1744,16);
        var c = new MovementController(home,new(1200,400));
        Tick(c,Far,35);
        var pointer = new PointD(c.Position.X+80,c.Position.Y+30);
        var result = Tick(c,pointer);
        Assert.False(new RectD(result.X,result.Y,160,60).Inflate(48).Contains(pointer));
    }
    [Fact] public void Negative_origin_and_tiny_work_area_do_not_escape_or_loop()
    {
        var area = new RectD(-1920,-200,1920,1080);
        var home = Geometry.DefaultHome(area,Size);
        var c = new MovementController(home,home);
        var result = Tick(c,home,90,area);
        Assert.True(area.Contains(new RectD(result.X,result.Y,160,60)));
        var tiny = new RectD(-10,5,40,20);
        Assert.Equal(new PointD(-10,5),Tick(c,new(-5,10),90,tiny));
    }
    [Fact] public void Long_resume_interval_does_not_teleport_during_return()
    {
        var c = new MovementController(new(1744,16),new(1000,400));
        var before=c.Position;
        var after=c.Tick(TimeSpan.FromHours(2),Far,Size,Area,true);
        Assert.Equal(before,after);
    }
    [Fact] public void Disabled_animations_yield_immediately_but_keep_return_delay()
    {
        var home = new PointD(1744,16);
        var c = new MovementController(home,home);
        var safe=Tick(c,home,1,animations:false);
        Assert.NotEqual(home,safe);
        Assert.Equal(safe,Tick(c,Far,29,animations:false));
        Assert.Equal(home,Tick(c,Far,3,animations:false));
    }
}
