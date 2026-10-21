using System.Security.Principal;
using System.Windows;

namespace Scotch;

public partial class App : System.Windows.Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance=new Mutex(true,@"Local\Scotch-"+WindowsIdentity.GetCurrent().User?.Value,out var created);
        if(!created){Shutdown();return;}
        try
        {
            var clock=new ClockWindow();MainWindow=clock;
            clock.Closed+=(_,_)=>Shutdown();
            clock.Show();
        }
        catch(Exception error)
        {System.Windows.MessageBox.Show("Scotch could not start. "+error.Message,"Scotch",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);}
    }
    protected override void OnExit(ExitEventArgs e)
    {instance?.Dispose();base.OnExit(e);}
}
