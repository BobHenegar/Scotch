namespace Scotch.Core;

public enum ClockSizePreset {Small,Medium,Large}

// Home is relative to the selected monitor's work-area origin, in DIP.
public sealed record ClockSettings(bool ShowDate=false,string? MonitorId=null,PointD? Home=null,ClockSizePreset Size=ClockSizePreset.Medium)
{
    public bool IsValid => Enum.IsDefined(Size)
        && (MonitorId is null || MonitorId.Length<=256)
        && (Home is null || (double.IsFinite(Home.Value.X) && double.IsFinite(Home.Value.Y)
            && Math.Abs(Home.Value.X)<=1_000_000 && Math.Abs(Home.Value.Y)<=1_000_000));
}
