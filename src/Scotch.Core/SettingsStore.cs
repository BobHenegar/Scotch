using System.Security;
using System.Text.Json;

namespace Scotch.Core;

public sealed class SettingsStore(string path)
{
    public ClockSettings Load()
    {
        try
        {
            var settings=JsonSerializer.Deserialize<ClockSettings>(File.ReadAllText(path));
            return settings is {IsValid:true}?settings:new();
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException or SecurityException)
        {return new();}
    }
    public bool TrySave(ClockSettings settings,out string? error)
    {
        error=null;
        if(!settings.IsValid){error="Saved preferences are invalid.";return false;}
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(stream,settings);stream.Flush(true);}
            File.Move(temp,path,true);
            return true;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException or SecurityException)
        {error=e.Message;return false;}
        finally
        {
            try {if(File.Exists(temp))File.Delete(temp);}
            catch(Exception e) when(e is IOException or UnauthorizedAccessException or SecurityException) { }
        }
    }
}
