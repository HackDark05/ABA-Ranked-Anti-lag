using System.Diagnostics;
using System.IO;
using System.Text;

namespace RegionBlocker
{
    internal static class FirewallManager
    {
        private const string RuleName = "BlockIP";

        internal static string GetRuleStatus()
        {
            string output = RunPS(
                "Get-NetFirewallRule -DisplayName 'BlockIP' -ErrorAction SilentlyContinue" +
                " | Select-Object -ExpandProperty Enabled");
            if (string.IsNullOrWhiteSpace(output)) return "MISSING";
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase)
                ? "ENABLED" : "DISABLED";
        }

        internal static void EnableBlock(IEnumerable<string> ips)
        {
            var list = ips.ToList();
            if (list.Count == 0)
                throw new InvalidOperationException("No IPs configured — press 'Apply to Rule' first.");

            // Always recreate the rule to ensure IPs and Enabled state are correct
            string ipArray = BuildIpArray(list);
            string script = $@"
$name = '{RuleName}'
Remove-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
$ips = {ipArray}
New-NetFirewallRule -DisplayName $name -Direction Outbound -Action Block `
    -RemoteAddress $ips -Profile Any -Enabled True | Out-Null
";
            RunPSFile(script);
        }

        internal static void DisableBlock(IEnumerable<string> ips)
        {
            string script = $@"
$name = '{RuleName}'
$rule = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
if ($rule) {{ Set-NetFirewallRule -DisplayName $name -Enabled False }}
";
            RunPSFile(script);
        }

        internal static void ApplyIPs(IEnumerable<string> ips)
        {
            var list = ips.ToList();
            string ipArray = BuildIpArray(list);
            string script = $@"
$name = '{RuleName}'
Remove-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
if ({(list.Count > 0 ? "1" : "0")}) {{
    $ips = {ipArray}
    New-NetFirewallRule -DisplayName $name -Direction Outbound -Action Block `
        -RemoteAddress $ips -Profile Any -Enabled False | Out-Null
}}
";
            RunPSFile(script);
        }

        private static string BuildIpArray(IEnumerable<string> ips)
        {
            var list = ips.ToList();
            if (list.Count == 0) return "@()";
            var sb = new StringBuilder("@(");
            sb.Append(string.Join(",", list.Select(ip => $"'{ip}'")));
            sb.Append(')');
            return sb.ToString();
        }

        // Write script to a temp .ps1 file and run it — avoids all -Command escape issues
        private static string RunPSFile(string script)
        {
            string tmp = Path.Combine(Path.GetTempPath(), $"rb_{Guid.NewGuid():N}.ps1");
            try
            {
                File.WriteAllText(tmp, script, Encoding.UTF8);
                return RunPS($"-ExecutionPolicy Bypass -File \"{tmp}\"");
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        // Run powershell with raw arguments
        private static string RunPS(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "powershell.exe",
                Arguments              = $"-NoProfile -NonInteractive -WindowStyle Hidden {arguments}",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi)!;
            string output = proc.StandardOutput.ReadToEnd();
            string errors = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            // Throw on PowerShell error so MainWindow can log it
            if (!string.IsNullOrWhiteSpace(errors) && proc.ExitCode != 0)
                throw new Exception(errors.Trim());
            return output;
        }
    }
}
