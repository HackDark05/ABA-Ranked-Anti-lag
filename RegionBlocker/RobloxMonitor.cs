using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RegionBlocker
{
    public class SamplePoint
    {
        public double RelX  { get; set; }
        public double RelY  { get; set; }
        public string Label { get; set; } = "";

        public (int x, int y) ToPixel(int w, int h)
            => ((int)(RelX * w), (int)(RelY * h));
    }

    public class RobloxMonitor : IDisposable
    {
        private System.Timers.Timer? _timer;
        private bool _triggered;

        private const int FAST_INTERVAL_MS = 1000;
        private const int SLOW_INTERVAL_MS = 5000;

        private readonly object _pointsLock = new();
        private List<SamplePoint> _samplePoints;

        public IReadOnlyList<SamplePoint> SamplePoints
        {
            get { lock (_pointsLock) return _samplePoints.AsReadOnly(); }
        }

        public void SetSamplePoints(List<SamplePoint> pts)
        {
            lock (_pointsLock) _samplePoints = pts;
        }

        // Default points matching the target positions from the reference screenshot
        public static List<SamplePoint> DefaultPoints() => new()
        {
            new SamplePoint { RelX = 0.035, RelY = 0.143, Label = "1" },
            new SamplePoint { RelX = 0.977, RelY = 0.142, Label = "2" },
            new SamplePoint { RelX = 0.032, RelY = 0.939, Label = "3" },
            new SamplePoint { RelX = 0.973, RelY = 0.936, Label = "4" },
            new SamplePoint { RelX = 0.502, RelY = 0.958, Label = "5" },
        };

        public int BlackThreshold { get; set; } = 4;

        public bool IsRunning { get; private set; }
        public event Action<MonitorStatus>? StatusChanged;
        public event Action? BlackScreenDetected;

        public RobloxMonitor()
        {
            // Load saved points from config; fall back to defaults if none saved
            var saved = ConfigManager.LoadPoints();
            if (saved.Count > 0)
                _samplePoints = saved.Select(d => new SamplePoint { RelX = d.RelX, RelY = d.RelY, Label = d.Label }).ToList();
            else
                _samplePoints = DefaultPoints();
        }

        public void Start()
        {
            _triggered = false;
            IsRunning  = true;
            _timer     = new System.Timers.Timer(SLOW_INTERVAL_MS);
            _timer.Elapsed  += (_, _) => Check();
            _timer.AutoReset = false;
            _timer.Start();
        }

        public void Stop()
        {
            IsRunning  = false;
            _triggered = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        public void ResetTrigger() => _triggered = false;

        private void ScheduleNext(bool robloxFound)
        {
            if (_timer == null || !IsRunning) return;
            _timer.Interval = robloxFound ? FAST_INTERVAL_MS : SLOW_INTERVAL_MS;
            _timer.Start();
        }

        private void Check()
        {
            var status = new MonitorStatus();

            var proc = Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
            if (proc == null)
            {
                status.RobloxState = RobloxState.NotRunning;
                StatusChanged?.Invoke(status);
                ScheduleNext(false);
                return;
            }

            if (proc.MainWindowHandle == IntPtr.Zero)
            {
                status.RobloxState = RobloxState.Loading;
                StatusChanged?.Invoke(status);
                ScheduleNext(false);
                return;
            }

            bool isMinimized = IsIconic(proc.MainWindowHandle);
            status.RobloxState = RobloxState.Running;
            status.IsMinimized = isMinimized;

            bool black = CaptureAndSample(proc.MainWindowHandle, status);

            StatusChanged?.Invoke(status);

            if (black && !_triggered)
            {
                _triggered = true;
                BlackScreenDetected?.Invoke();
            }

            ScheduleNext(true);
        }

        public bool CaptureAndSample(IntPtr hwnd, MonitorStatus status)
        {
            try
            {
                if (!GetClientRect(hwnd, out RECT cr)) return false;
                int w = cr.Right  - cr.Left;
                int h = cr.Bottom - cr.Top;
                if (w <= 0 || h <= 0) return false;

                var bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    bool ok = PrintWindow(hwnd, hdc, 2);
                    g.ReleaseHdc(hdc);
                    if (!ok) { bmp.Dispose(); return false; }
                }

                List<SamplePoint> pts;
                lock (_pointsLock) pts = new List<SamplePoint>(_samplePoints);

                int blackCount = 0;
                var colors = new Color[pts.Count];

                for (int i = 0; i < pts.Count; i++)
                {
                    var (px, py) = pts[i].ToPixel(w, h);
                    px = Math.Clamp(px, 0, w - 1);
                    py = Math.Clamp(py, 0, h - 1);
                    colors[i] = bmp.GetPixel(px, py);
                    if (colors[i].R == 0 && colors[i].G == 0 && colors[i].B == 0)
                        blackCount++;
                }

                status.SampleColors   = colors;
                status.BlackCount     = blackCount;
                status.IsBlackScreen  = blackCount >= BlackThreshold;
                status.CapturedBitmap = bmp;

                return status.IsBlackScreen;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public void Dispose() => Stop();
    }

    public class MonitorStatus
    {
        public RobloxState RobloxState   { get; set; } = RobloxState.NotRunning;
        public bool        IsBlackScreen { get; set; }
        public bool        IsMinimized   { get; set; }
        public int         BlackCount    { get; set; }
        public Color[]     SampleColors  { get; set; } = Array.Empty<Color>();
        public Bitmap?     CapturedBitmap { get; set; }
    }

    public enum RobloxState { NotRunning, Loading, Running }
}
