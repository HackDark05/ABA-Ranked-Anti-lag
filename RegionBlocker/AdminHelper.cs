using System.Diagnostics;
using System.Security.Principal;

namespace RegionBlocker
{
    internal static class AdminHelper
    {
        internal static bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static void RestartAsAdmin()
        {
            var psi = new ProcessStartInfo
            {
                FileName        = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName!,
                UseShellExecute = true,
                Verb            = "runas"
            };
            try { Process.Start(psi); } catch { }
        }
    }
}
