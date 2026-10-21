using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Scotch.Windows;

public sealed class RegionalFormatService
{
    public CultureInfo Culture {get;private set;}=ReadWindowsCulture();

    public void Refresh()
    {
        // CultureData caches user overrides. A new CultureInfo alone keeps the old patterns.
        CultureInfo.CurrentCulture.ClearCachedData();
        Culture=ReadWindowsCulture();
    }
    private static CultureInfo ReadWindowsCulture()
    {
        var name=new StringBuilder(85); // LOCALE_NAME_MAX_LENGTH
        return GetUserDefaultLocaleName(name,name.Capacity)>0
            ?new CultureInfo(name.ToString(),true)
            :new CultureInfo(CultureInfo.InstalledUICulture.Name,true);
    }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]
    private static extern int GetUserDefaultLocaleName(StringBuilder name,int length);
}
