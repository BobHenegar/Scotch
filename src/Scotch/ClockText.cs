using System.Globalization;

namespace Scotch;

public static class ClockText
{
    public static (string Time,string Date) Format(DateTime time,CultureInfo culture,bool showDate)
        => (time.ToString("t",culture),showDate?time.ToString("ddd, MMM d",culture):string.Empty);
}
