using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Application = System.Windows.Application;
using WinForms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace RegionBlocker
{
    public partial class MainWindow : Window
    {
        private readonly RobloxMonitor _monitor = new();
        private List<string> _ips = new();
        private int _triggerCount;
        private bool _minimizeNotified;

        private static readonly Regex CidrRegex = new(@"^\d{1,3}(\.\d{1,3}){3}(/(\d{1,2}|(\d{1,3}\.){3}\d{1,3}))?$", RegexOptions.Compiled);

        private static string NormalizeCidr(string ip)
        {
            int slash = ip.IndexOf('/');
            if (slash < 0) return ip;
            string addr = ip[..slash];
            string mask = ip[(slash + 1)..];
            if (!mask.Contains('.')) return ip;
            if (!System.Net.IPAddress.TryParse(mask, out var maskAddr)) return ip;
            byte[] bytes = maskAddr.GetAddressBytes();
            int bits = 0;
            foreach (byte b in bytes)
                for (int i = 7; i >= 0; i--)
                    if ((b & (1 << i)) != 0) bits++; else goto done;
            done:
            return $"{addr}/{bits}";
        }

        public MainWindow()
        {
            InitializeComponent();
            _ips = ConfigManager.LoadIPs();
            RefreshIPList();
            RefreshFirewallStatus();
            RefreshLastLog();

            InitTrayIcon();

            _monitor.StatusChanged       += OnMonitorStatus;
            _monitor.BlackScreenDetected += OnBlackScreen;

            Closed += (_, _) => { _monitor.Dispose(); _trayIcon?.Dispose(); };
        }

        // ── Monitor events ────────────────────────────────────────────────────

        private void OnMonitorStatus(MonitorStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                switch (status.RobloxState)
                {
                    case RobloxState.NotRunning:
                        SetLabel(txtRobloxState, "NOT OPEN", "#FF4444");
                        SetLabel(txtScreenState,  "-",        "#333355");
                        SetAllDots(System.Drawing.Color.FromArgb(17, 17, 34));
                        txtBlackCount.Text = "";
                        break;

                    case RobloxState.Loading:
                        SetLabel(txtRobloxState, "LOADING", "#FFAA00");
                        SetLabel(txtScreenState,  "-",       "#333355");
                        SetAllDots(System.Drawing.Color.FromArgb(17, 17, 34));
                        txtBlackCount.Text = "";
                        break;

                    case RobloxState.Running:
                        SetLabel(txtRobloxState, "RUNNING", "#44FF88");

                        if (status.IsMinimized)
                        {
                            SetLabel(txtScreenState, "MINIMIZED", "#FFAA00");
                            txtBlackCount.Text = status.IsBlackScreen
                                ? $"(minimized) {status.BlackCount}/5 black"
                                : "(minimized)";
                            ShowMinimizeWarning();
                        }
                        else
                        {
                            ResetMinimizeNotify();
                            UpdateDots(status.SampleColors);
                            if (status.IsBlackScreen)
                            {
                                SetLabel(txtScreenState, "BLACK", "#FF4444");
                                txtBlackCount.Text = $"{status.BlackCount}/5 black";
                            }
                            else
                            {
                                SetLabel(txtScreenState, "NORMAL", "#44FF88");
                                txtBlackCount.Text = status.BlackCount > 0 ? $"{status.BlackCount}/5 black" : "";
                            }
                        }
                        break;
                }
            });
        }

        private void OnBlackScreen()
        {
            _triggerCount++;
            Dispatcher.Invoke(() =>
            {
                txtTriggerCount.Text = $"{_triggerCount}x";

                // Change reset button to orange/alert to indicate trigger is active
                SetResetTriggerButtonState(active: true);

                if (_ips.Count == 0)
                {
                    Log("BLACK SCREEN detected — no IPs configured. Press 'Apply to Rule' first.");
                    return;
                }

                Log($"BLACK SCREEN detected — enabling firewall ({_ips.Count} IPs)...");

                try
                {
                    FirewallManager.EnableBlock(_ips);
                    ConfigManager.WriteLog("TRIGGERED: Black screen detected, block rule enabled.");
                    RefreshFirewallStatus();
                    RefreshLastLog();
                    Log($"Block rule ENABLED. (trigger #{_triggerCount})");
                }
                catch (Exception ex)
                {
                    Log($"Trigger error: {ex.Message}");
                }
            });
        }

        // ── Monitor buttons ───────────────────────────────────────────────────

        private void BtnStartMonitor_Click(object sender, RoutedEventArgs e)
        {
            _monitor.Start();
            btnStartMonitor.IsEnabled = false;
            btnStopMonitor.IsEnabled  = true;
            Log("Monitor started.");
        }

        private void BtnStopMonitor_Click(object sender, RoutedEventArgs e)
        {
            _monitor.Stop();
            btnStartMonitor.IsEnabled = true;
            btnStopMonitor.IsEnabled  = false;
            SetLabel(txtRobloxState, "-", "#333355");
            SetLabel(txtScreenState, "-", "#333355");
            SetAllDots(System.Drawing.Color.FromArgb(17, 17, 34));
            txtBlackCount.Text = "";
            Log("Monitor stopped.");
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            var pw = new PreviewWindow(_monitor) { Owner = this };
            pw.Show();
        }

        private void BtnResetTrigger_Click(object sender, RoutedEventArgs e)
        {
            // Reset trigger gate
            _monitor.ResetTrigger();
            SetResetTriggerButtonState(active: false);

            // Also disable the firewall rule so the next session can trigger immediately
            try
            {
                FirewallManager.DisableBlock(_ips);
                ConfigManager.WriteLog("Reset: trigger cleared and block rule disabled.");
                RefreshFirewallStatus();
                RefreshLastLog();
                Log("Trigger reset — block rule disabled. Ready for next detection.");
            }
            catch (Exception ex)
            {
                Log($"Trigger reset (firewall error: {ex.Message})");
            }
        }

        // Changes the Reset Trigger button color to indicate whether a trigger is active
        private void SetResetTriggerButtonState(bool active)
        {
            if (active)
            {
                // Orange/alert: trigger fired, waiting for manual reset
                btnResetTrigger.Background  = new SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 50, 0));
                btnResetTrigger.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 100, 0));
                btnResetTrigger.Foreground  = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 160, 40));
                btnResetTrigger.Content     = "⚠ RESET TRIGGER";
            }
            else
            {
                // Neutral: idle state
                btnResetTrigger.Background  = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 46));
                btnResetTrigger.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 58, 92));
                btnResetTrigger.Foreground  = new SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 192, 224));
                btnResetTrigger.Content     = "~ RESET TRIGGER";
            }
        }

        // ── Firewall buttons ──────────────────────────────────────────────────

        private void BtnEnableBlock_Click(object sender, RoutedEventArgs e)
        {
            RunFirewallOp(() =>
            {
                FirewallManager.EnableBlock(_ips);
                ConfigManager.WriteLog("Manual: Block rule ENABLED.");
                Log("Block rule ENABLED.");
            });
        }

        private void BtnDisableBlock_Click(object sender, RoutedEventArgs e)
        {
            RunFirewallOp(() =>
            {
                FirewallManager.DisableBlock(_ips);
                ConfigManager.WriteLog("Manual: Block rule DISABLED.");
                Log("Block rule DISABLED.");
            });
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshFirewallStatus();
            RefreshLastLog();
            Log("Refreshed.");
        }

        private void RunFirewallOp(Action op)
        {
            try
            {
                op();
                RefreshFirewallStatus();
            }
            catch (Exception ex) { Log($"Error: {ex.Message}"); }
        }

        // ── IP list buttons ───────────────────────────────────────────────────

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => AddCurrentIP();
        private void TxtNewIP_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) AddCurrentIP();
        }

        private void AddCurrentIP()
        {
            string val = NormalizeCidr(txtNewIP.Text.Trim());
            if (!CidrRegex.IsMatch(val)) { Log("Invalid format. Use x.x.x.x or x.x.x.x/24 or x.x.x.x/255.255.255.0"); return; }
            if (_ips.Contains(val)) { Log($"Already in list: {val}"); return; }
            _ips.Add(val);
            ConfigManager.SaveIPs(_ips);
            listIPs.Items.Add(val);
            UpdateIPCount();
            txtNewIP.Clear();
            Log($"Added: {val}");
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selected = listIPs.SelectedItems.Cast<string>().ToList();
            if (selected.Count == 0) { Log("Select IPs to remove."); return; }
            foreach (var s in selected) _ips.Remove(s);
            ConfigManager.SaveIPs(_ips);
            RefreshIPList();
            Log($"Removed {selected.Count} IP(s).");
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_ips.Count == 0) { Log("No IPs to apply."); return; }
            try
            {
                FirewallManager.ApplyIPs(_ips);
                RefreshFirewallStatus();
                Log($"Applied {_ips.Count} IPs to firewall rule.");
            }
            catch (Exception ex) { Log($"Error: {ex.Message}"); }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files|*.*" };
            if (dlg.ShowDialog() != true) return;
            var lines = File.ReadAllLines(dlg.FileName)
                            .Select(l => NormalizeCidr(l.Trim()))
                            .Where(l => CidrRegex.IsMatch(l));
            int added = 0;
            foreach (var line in lines)
            {
                if (!_ips.Contains(line)) { _ips.Add(line); added++; }
            }
            ConfigManager.SaveIPs(_ips);
            RefreshIPList();
            Log($"Imported {added} new IP(s).");
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = "BlockIP-list.txt" };
            if (dlg.ShowDialog() != true) return;
            File.WriteAllLines(dlg.FileName, _ips);
            Log($"Exported {_ips.Count} IPs to file.");
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void RefreshFirewallStatus()
        {
            string status = FirewallManager.GetRuleStatus();
            Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case "ENABLED":  SetLabel(txtRuleStatus, "ENABLED",  "#FF4444"); break;
                    case "DISABLED": SetLabel(txtRuleStatus, "DISABLED", "#44FF88"); break;
                    default:         SetLabel(txtRuleStatus, "NO RULE",  "#FFAA00"); break;
                }
            });
        }

        private void RefreshLastLog()
        {
            string last = ConfigManager.GetLastLog();
            Dispatcher.Invoke(() => txtLog.Text = last);
        }

        private void RefreshIPList()
        {
            listIPs.Items.Clear();
            foreach (var ip in _ips) listIPs.Items.Add(ip);
            UpdateIPCount();
        }

        private void UpdateIPCount() { }

        private void SetLabel(System.Windows.Controls.TextBlock tb, string text, string hexColor)
        {
            tb.Text       = text;
            tb.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
        }

        private void Log(string msg)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss");
            txtLog.Text = $"[{ts}] {msg}";
        }

        private void ShowMinimizeWarning()
        {
            if (_minimizeNotified) return;
            _minimizeNotified = true;
            _trayIcon?.ShowBalloonTip(4000, "RegionBlocker",
                "Roblox is minimized — still monitoring for black screen.", WinForms.ToolTipIcon.Warning);
        }

        private void ResetMinimizeNotify() => _minimizeNotified = false;

        // ── System tray ───────────────────────────────────────────────────────

        private WinForms.NotifyIcon? _trayIcon;

        private void InitTrayIcon()
        {
            _trayIcon = new WinForms.NotifyIcon
            {
                Icon    = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text    = "RegionBlocker"
            };
        }

        private void UpdateDots(System.Drawing.Color[] colors)
        {
            var dots = new[] { dot1, dot2, dot3, dot4, dot5 };
            for (int i = 0; i < dots.Length && i < colors.Length; i++)
            {
                var c = colors[i];
                dots[i].Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
            }
        }

        private void SetAllDots(System.Drawing.Color c)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
            dot1.Background = dot2.Background = dot3.Background =
            dot4.Background = dot5.Background = brush;
        }
    }
}
