using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RegionBlocker
{
    internal static class FirewallManager
    {
        private const string RuleName = "BlockIP";

        private static readonly Regex EnabledRegex = new(
            @"Enabled:\s*(Yes|No)", RegexOptions.IgnoreCase);

        internal static List<string> GetRuleIPs()
        {
            string output = RunNetsh($"advfirewall firewall show rule name=\"{RuleName}\"");
            if (string.IsNullOrWhiteSpace(output) || output.Contains("No rules match"))
                return new List<string>();

            var result = new List<string>();
            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("RemoteIP:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = trimmed.Substring("RemoteIP:".Length).Trim();
                foreach (var entry in value.Split(','))
                {
                    var ip = entry.Trim();
                    if (!string.IsNullOrEmpty(ip) && ip != "Any")
                        result.Add(ip);
                }
            }
            return result;
        }

        internal static string GetRuleStatus()
        {
            string output = RunNetsh($"advfirewall firewall show rule name=\"{RuleName}\"");
            if (string.IsNullOrWhiteSpace(output) || output.Contains("No rules match"))
                return "MISSING";
            var match = EnabledRegex.Match(output);
            if (!match.Success) return "DISABLED";
            return match.Groups[1].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                ? "ENABLED" : "DISABLED";
        }

        internal static void EnableBlock(IEnumerable<string> ips)
        {
            var list = ips.ToList();
            if (list.Count == 0)
                throw new InvalidOperationException("No IPs configured — press 'Apply to Rule' first.");

            RunNetsh($"advfirewall firewall delete rule name=\"{RuleName}\"");
            CreateRule(list, enabled: true);
        }

        internal static void DisableBlock(IEnumerable<string> ips)
        {
            RunNetsh($"advfirewall firewall set rule name=\"{RuleName}\" new enable=no");
        }

        internal static void ApplyIPs(IEnumerable<string> ips)
        {
            var list = ips.ToList();
            RunNetsh($"advfirewall firewall delete rule name=\"{RuleName}\"");
            if (list.Count > 0)
                CreateRule(list, enabled: false);
        }

        private static void CreateRule(List<string> ips, bool enabled)
        {
            string remoteip = string.Join(",", ips);
            string enabledStr = enabled ? "yes" : "no";
            RunNetsh($"advfirewall firewall add rule name=\"{RuleName}\" dir=out action=block remoteip=\"{remoteip}\" enable={enabledStr} profile=any");
        }

        private static string RunNetsh(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "netsh.exe",
                Arguments              = arguments,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi)!;
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }
    }
}
