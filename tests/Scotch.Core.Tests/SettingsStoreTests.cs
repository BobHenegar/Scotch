using Scotch.Core;

namespace Scotch.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string folder=Path.Combine(Path.GetTempPath(),"ScotchTests",Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(folder,"settings.json");
    [Fact] public void Size_roundtrips_and_older_settings_default_to_medium()
    {
        var store=new SettingsStore(FilePath);
        foreach(var size in new[]{ClockSizePreset.Small,ClockSizePreset.Medium,ClockSizePreset.Large})
        {
            Assert.True(store.TrySave(new ClockSettings(Size:size),out var error),error);
            Assert.Equal(size,store.Load().Size);
        }
        File.WriteAllText(FilePath,"{\"ShowDate\":true}");
        Assert.Equal(ClockSizePreset.Medium,store.Load().Size);
        Assert.False(store.TrySave(new ClockSettings(Size:(ClockSizePreset)99),out _));
    }
    [Fact] public void Missing_and_corrupt_settings_use_defaults()
    {
        var store=new SettingsStore(FilePath);
        Assert.Equal(new ClockSettings(),store.Load());
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath,"{broken");
        Assert.Equal(new ClockSettings(),store.Load());
    }
    [Fact] public void Date_and_home_roundtrip()
    {
        var store=new SettingsStore(FilePath);
        var settings=new ClockSettings(true,"DISPLAY2",new(20,30));
        Assert.True(store.TrySave(settings,out var error),error);
        Assert.Equal(settings,store.Load());
    }
    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(1e100)]
    public void Invalid_home_is_rejected(double coordinate)
    {
        var store=new SettingsStore(FilePath);
        Assert.True(store.TrySave(new(true,"DISPLAY1",new(20,30)),out _));
        Assert.False(store.TrySave(new(false,"DISPLAY1",new(coordinate,0)),out _));
        Assert.Equal(new PointD(20,30),store.Load().Home);
    }
    [Fact] public void Failed_write_preserves_previous_file()
    {
        var store=new SettingsStore(FilePath);
        var original=new ClockSettings(true,"DISPLAY1",new(20,30));
        Assert.True(store.TrySave(original,out _));
        using(var held=new FileStream(FilePath,FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            Assert.False(store.TrySave(new(),out var error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
        Assert.Equal(original,store.Load());
    }
    [Fact] public void Null_json_and_extreme_loaded_coordinates_use_defaults()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath,"null");
        Assert.Equal(new ClockSettings(),new SettingsStore(FilePath).Load());
        File.WriteAllText(FilePath,"{\"ShowDate\":true,\"MonitorId\":\"a\",\"Home\":{\"X\":1e100,\"Y\":0}}");
        Assert.Equal(new ClockSettings(),new SettingsStore(FilePath).Load());
    }
    public void Dispose(){if(Directory.Exists(folder)) Directory.Delete(folder,true);}
}
