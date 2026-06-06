using System.Windows;
using Application = System.Windows.Application;

namespace RegionBlocker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!AdminHelper.IsAdmin())
            {
                AdminHelper.RestartAsAdmin();
                Shutdown();
                return;
            }
            base.OnStartup(e);
        }
    }
}
