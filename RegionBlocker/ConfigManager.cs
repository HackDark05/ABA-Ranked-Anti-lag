using System.IO;
using System.Text.Json;

namespace RegionBlocker
{
    internal static class ConfigManager
    {
        private static readonly string ConfigDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RegionBlocker");
        private static readonly string ConfigFile = Path.Combine(ConfigDir, "iplist.json");
        private static readonly string PointsFile = Path.Combine(ConfigDir, "points.json");
        internal static readonly string LogFile   = Path.Combine(ConfigDir, "trigger.log");

        private static readonly List<string> DefaultIPs = new()
        {
            "3.15.187.0/24","13.222.104.0/24","18.116.63.0/24","18.117.179.0/24",
            "18.232.35.0/24","18.232.154.0/24","34.234.84.0/24","35.88.193.0/24",
            "44.207.5.0/24","52.202.123.0/24","54.159.153.0/24","54.193.123.0/24",
            "98.93.158.0/24","100.53.91.0/24","128.116.1.0/24","128.116.4.0/24",
            "128.116.5.0/24","128.116.21.0/24","128.116.22.0/24","128.116.31.0/24",
            "128.116.32.0/24","128.116.33.0/24","128.116.44.0/24","128.116.45.0/24",
            "128.116.48.0/24","128.116.51.0/24","128.116.53.0/24","128.116.56.0/24",
            "128.116.63.0/24","128.116.86.0/24","128.116.95.0/24","128.116.99.0/24",
            "128.116.101.0/24","128.116.102.0/24","128.116.104.0/24","128.116.115.0/24",
            "128.116.116.0/24","128.116.117.0/24","128.116.119.0/24","128.116.122.0/24",
            "128.116.123.0/24","128.116.124.0/24","128.116.127.0/24","209.206.42.0/24",
            "209.206.43.0/24"
        };

        // ── IP list ────────────────────────────────────────────────────────────
        internal static List<string> LoadIPs()
        {
            EnsureDir();
            if (!File.Exists(ConfigFile))
            {
                SaveIPs(DefaultIPs);
                return new List<string>(DefaultIPs);
            }
            try
            {
                string json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        internal static void SaveIPs(IEnumerable<string> ips)
        {
            EnsureDir();
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(ips.ToList(), new JsonSerializerOptions { WriteIndented = true }));
        }

        // ── Sample points ──────────────────────────────────────────────────────
        internal static List<SamplePointData> LoadPoints()
        {
            EnsureDir();
            if (!File.Exists(PointsFile)) return new List<SamplePointData>();
            try
            {
                string json = File.ReadAllText(PointsFile);
                return JsonSerializer.Deserialize<List<SamplePointData>>(json) ?? new List<SamplePointData>();
            }
            catch { return new List<SamplePointData>(); }
        }

        internal static void SavePoints(IEnumerable<SamplePoint> points)
        {
            EnsureDir();
            var data = points.Select(p => new SamplePointData { RelX = p.RelX, RelY = p.RelY, Label = p.Label }).ToList();
            File.WriteAllText(PointsFile, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ── Log ────────────────────────────────────────────────────────────────
        internal static void WriteLog(string message)
        {
            EnsureDir();
            string ts   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"[{ts}] {message}";
            File.AppendAllText(LogFile, line + Environment.NewLine);
        }

        internal static string GetLastLog()
        {
            if (!File.Exists(LogFile)) return "-";
            var lines = File.ReadAllLines(LogFile);
            return lines.Length > 0 ? lines[^1] : "-";
        }

        private static void EnsureDir()
        {
            if (!Directory.Exists(ConfigDir))
                Directory.CreateDirectory(ConfigDir);
        }
    }

    // Serialization DTO for sample points
    internal class SamplePointData
    {
        public double RelX  { get; set; }
        public double RelY  { get; set; }
        public string Label { get; set; } = "";
    }
}
