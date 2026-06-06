using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Cursors = System.Windows.Input.Cursors;

namespace RegionBlocker
{
    internal class PointViewModel : INotifyPropertyChanged
    {
        private double _relX, _relY;
        private System.Drawing.Color _color = System.Drawing.Color.Gray;
        private bool _isBlack;

        public string Label { get; set; } = "";

        public double RelX { get => _relX; set { _relX = Math.Clamp(value, 0, 1); OnChanged(); OnChanged(nameof(RelXPct)); } }
        public double RelY { get => _relY; set { _relY = Math.Clamp(value, 0, 1); OnChanged(); OnChanged(nameof(RelYPct)); } }

        public string RelXPct
        {
            get => (_relX * 100).ToString("F1");
            set { if (double.TryParse(value, out double v)) RelX = v / 100.0; }
        }
        public string RelYPct
        {
            get => (_relY * 100).ToString("F1");
            set { if (double.TryParse(value, out double v)) RelY = v / 100.0; }
        }

        public System.Drawing.Color SampledColor { get => _color; set { _color = value; OnChanged(); OnChanged(nameof(SwatchBrush)); OnChanged(nameof(IsBlackText)); OnChanged(nameof(IsBlackFore)); } }
        public bool IsBlack { get => _isBlack; set { _isBlack = value; OnChanged(); OnChanged(nameof(IsBlackText)); OnChanged(nameof(IsBlackFore)); } }

        public System.Windows.Media.Brush SwatchBrush =>
            new SolidColorBrush(System.Windows.Media.Color.FromRgb(_color.R, _color.G, _color.B));
        public string IsBlackText => _isBlack ? "●" : "○";
        public System.Windows.Media.Brush IsBlackFore =>
            new SolidColorBrush(_isBlack ? System.Windows.Media.Color.FromRgb(255, 80, 80)
                                         : System.Windows.Media.Color.FromRgb(80, 200, 80));

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public partial class PreviewWindow : Window
    {
        private readonly RobloxMonitor _monitor;
        private System.Timers.Timer? _refreshTimer;
        private int _refreshMs = 1000;

        private readonly List<PointViewModel> _points = new();
        private int _threshold = 4;

        private readonly List<Ellipse>   _markers      = new();
        private readonly List<TextBlock> _markerLabels = new();

        private int _dragIndex = -1;

        private double _imgW, _imgH, _imgOffX, _imgOffY;

        private Bitmap? _currentBitmap;
        private readonly object _bitmapLock = new();

        public PreviewWindow(RobloxMonitor monitor)
        {
            InitializeComponent();
            _monitor = monitor;

            foreach (var sp in monitor.SamplePoints)
                _points.Add(new PointViewModel { Label = sp.Label, RelX = sp.RelX, RelY = sp.RelY });

            sliderThreshold.Value = _threshold;
            RefreshPointList();

            previewCanvas.SizeChanged += (_, _) => RedrawMarkers();

            StartRefreshTimer(_refreshMs);
        }

        // ── Timer ────────────────────────────────────────────────────────────
        private void StartRefreshTimer(int ms)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            if (ms <= 0) return;
            _refreshTimer = new System.Timers.Timer(ms) { AutoReset = true };
            _refreshTimer.Elapsed += (_, _) => DoCapture();
            _refreshTimer.Start();
        }

        private void DoCapture()
        {
            try
            {
                var proc = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta").FirstOrDefault();
                if (proc == null || proc.MainWindowHandle == IntPtr.Zero)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = "  —  Roblox not open";
                        txtOverlay.Visibility = Visibility.Visible;
                        imgPreview.Source = null;
                        foreach (var p in _points) { p.SampledColor = System.Drawing.Color.DimGray; p.IsBlack = false; }
                    });
                    return;
                }

                var tempPts = _points.Select(p => new SamplePoint { RelX = p.RelX, RelY = p.RelY, Label = p.Label }).ToList();
                _monitor.SetSamplePoints(tempPts);

                var status = new MonitorStatus();
                _monitor.CaptureAndSample(proc.MainWindowHandle, status);

                Bitmap? bmp = status.CapturedBitmap;
                if (bmp == null) return;

                var colors = status.SampleColors;
                BitmapSource? bmpSrc = null;

                lock (_bitmapLock)
                {
                    _currentBitmap?.Dispose();
                    _currentBitmap = bmp;

                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();
                    bmpSrc = bi;
                }

                Dispatcher.Invoke(() =>
                {
                    bool isMin = status.IsMinimized;
                    txtOverlay.Visibility = Visibility.Collapsed;
                    txtStatus.Text = isMin ? "  —  MINIMIZED (still capturing)" : "  —  RUNNING";
                    txtStatus.Foreground = new SolidColorBrush(isMin
                        ? System.Windows.Media.Color.FromRgb(255, 170, 0)
                        : System.Windows.Media.Color.FromRgb(80, 220, 120));

                    imgPreview.Source = bmpSrc;

                    for (int i = 0; i < _points.Count && i < colors.Length; i++)
                    {
                        _points[i].SampledColor = colors[i];
                        _points[i].IsBlack      = (colors[i].R == 0 && colors[i].G == 0 && colors[i].B == 0);
                    }

                    Dispatcher.InvokeAsync(RedrawMarkers, System.Windows.Threading.DispatcherPriority.Render);
                });
            }
            catch { }
        }

        // ── Point list ───────────────────────────────────────────────────────
        private void RefreshPointList()
        {
            pointsList.ItemsSource = null;
            pointsList.ItemsSource = _points;
            RebuildMarkers();
        }

        private void RebuildMarkers()
        {
            foreach (var e in _markers)      previewCanvas.Children.Remove(e);
            foreach (var t in _markerLabels) previewCanvas.Children.Remove(t);
            _markers.Clear();
            _markerLabels.Clear();

            for (int i = 0; i < _points.Count; i++)
            {
                int idx = i;

                var el = new Ellipse
                {
                    Width = 18, Height = 18,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = System.Windows.Media.Brushes.Cyan,
                    StrokeThickness = 2,
                    Cursor = Cursors.SizeAll,
                    Tag = idx
                };
                el.MouseLeftButtonDown += Marker_MouseDown;
                previewCanvas.Children.Add(el);
                _markers.Add(el);

                var tb = new TextBlock
                {
                    Text = _points[i].Label,
                    Foreground = System.Windows.Media.Brushes.Yellow,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    IsHitTestVisible = false
                };
                previewCanvas.Children.Add(tb);
                _markerLabels.Add(tb);
            }

            RedrawMarkers();
        }

        private void RedrawMarkers()
        {
            if (imgPreview.Source == null) return;

            double cw = previewCanvas.ActualWidth;
            double ch = previewCanvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            double srcW = imgPreview.Source.Width;
            double srcH = imgPreview.Source.Height;
            if (srcW <= 0 || srcH <= 0) return;

            double scaleX = cw / srcW;
            double scaleY = ch / srcH;
            double scale  = Math.Min(scaleX, scaleY);
            _imgW   = srcW * scale;
            _imgH   = srcH * scale;
            _imgOffX = (cw - _imgW) / 2;
            _imgOffY = (ch - _imgH) / 2;

            imgPreview.Width  = _imgW;
            imgPreview.Height = _imgH;
            Canvas.SetLeft(imgPreview, _imgOffX);
            Canvas.SetTop(imgPreview,  _imgOffY);

            for (int i = 0; i < _points.Count && i < _markers.Count; i++)
            {
                double cx = _imgOffX + _points[i].RelX * _imgW;
                double cy = _imgOffY + _points[i].RelY * _imgH;
                double r  = 9;

                Canvas.SetLeft(_markers[i], cx - r);
                Canvas.SetTop(_markers[i],  cy - r);

                _markers[i].Stroke = _points[i].IsBlack
                    ? System.Windows.Media.Brushes.Red
                    : System.Windows.Media.Brushes.Cyan;

                Canvas.SetLeft(_markerLabels[i], cx + r - 2);
                Canvas.SetTop(_markerLabels[i],  cy - r - 1);
            }
        }

        // ── Drag ─────────────────────────────────────────────────────────────
        private void Marker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse el && el.Tag is int idx)
            {
                _dragIndex = idx;
                previewCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_dragIndex >= 0) return;
            var pos = e.GetPosition(previewCanvas);
            if (_imgW <= 0 || _imgH <= 0) return;

            double relX = (pos.X - _imgOffX) / _imgW;
            double relY = (pos.Y - _imgOffY) / _imgH;
            if (relX < 0 || relX > 1 || relY < 0 || relY > 1) return;

            foreach (var m in _markers)
            {
                double cx = Canvas.GetLeft(m) + 9;
                double cy = Canvas.GetTop(m)  + 9;
                double dx = pos.X - cx, dy = pos.Y - cy;
                if (Math.Sqrt(dx * dx + dy * dy) < 14) return;
            }

            AddPointAt(relX, relY);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragIndex >= 0)
            {
                _dragIndex = -1;
                previewCanvas.ReleaseMouseCapture();
                RefreshPointList();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragIndex < 0 || e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(previewCanvas);

            if (_imgW <= 0 || _imgH <= 0) return;
            double relX = Math.Clamp((pos.X - _imgOffX) / _imgW, 0, 1);
            double relY = Math.Clamp((pos.Y - _imgOffY) / _imgH, 0, 1);

            _points[_dragIndex].RelX = relX;
            _points[_dragIndex].RelY = relY;
            RedrawMarkers();
            pointsList.ItemsSource = null;
            pointsList.ItemsSource = _points;
        }

        // ── Buttons ───────────────────────────────────────────────────────────
        private void AddPointAt(double relX, double relY)
        {
            int idx = _points.Count + 1;
            _points.Add(new PointViewModel { Label = idx.ToString(), RelX = relX, RelY = relY });
            sliderThreshold.Maximum = _points.Count;
            RebuildMarkers();
            RefreshPointList();
        }

        private void BtnAddPoint_Click(object sender, RoutedEventArgs e)
            => AddPointAt(0.5, 0.5);

        private void BtnRemoveLast_Click(object sender, RoutedEventArgs e)
        {
            if (_points.Count == 0) return;
            _points.RemoveAt(_points.Count - 1);
            sliderThreshold.Maximum = Math.Max(1, _points.Count);
            if (_threshold > _points.Count) _threshold = _points.Count;
            RebuildMarkers();
            RefreshPointList();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _points.Clear();
            foreach (var sp in RobloxMonitor.DefaultPoints())
                _points.Add(new PointViewModel { Label = sp.Label, RelX = sp.RelX, RelY = sp.RelY });
            _threshold = 4;
            sliderThreshold.Value   = 4;
            sliderThreshold.Maximum = _points.Count;

            // Apply and save the reset points
            var newPts = _points.Select(p => new SamplePoint { RelX = p.RelX, RelY = p.RelY, Label = p.Label }).ToList();
            _monitor.SetSamplePoints(newPts);
            _monitor.BlackThreshold = _threshold;
            ConfigManager.SavePoints(newPts);

            RebuildMarkers();
            RefreshPointList();

            txtHint.Text = $"Reset to defaults and saved ({newPts.Count} points)";
            txtHint.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 220, 120));
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var newPts = _points.Select(p => new SamplePoint
            {
                RelX  = p.RelX,
                RelY  = p.RelY,
                Label = p.Label
            }).ToList();

            _monitor.SetSamplePoints(newPts);
            _monitor.BlackThreshold = _threshold;

            // Persist to disk so positions survive app restarts
            ConfigManager.SavePoints(newPts);

            txtHint.Text = $"Applied and saved {newPts.Count} points, threshold = {_threshold}";
            txtHint.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 220, 120));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void CboRefresh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboRefresh.SelectedItem is ComboBoxItem item && item.Tag is string tagStr
                && int.TryParse(tagStr, out int ms))
            {
                _refreshMs = ms;
                StartRefreshTimer(ms);
            }
        }

        private void SliderThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _threshold = (int)e.NewValue;
            if (txtThreshold != null) txtThreshold.Text = _threshold.ToString();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            lock (_bitmapLock) { _currentBitmap?.Dispose(); _currentBitmap = null; }
            base.OnClosed(e);
        }
    }
}
