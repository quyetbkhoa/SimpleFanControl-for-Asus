using AsusFanControl;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    public partial class Form1 : Form
    {
        private static readonly Color Blue = UiPalette.Primary;
        private static readonly Color DarkBlue = UiPalette.PrimaryDark;
        private static readonly Color TextPrimary = UiPalette.Text;
        private static readonly Color TextSecondary = UiPalette.Muted;
        private static readonly Color PageBackground = UiPalette.Page;
        private AsusControl asusControl;
        private int fanSpeed = -1;
        private Timer timer;
        private NotifyIcon trayIcon;
        private bool exitRequested;
        private bool initializing = true;
        private bool refreshing;
        private readonly object hardwareGate = new object ();
        private readonly FanSpeedRamp speedRamp = new FanSpeedRamp();
        private FanCurve committedCurve = FanCurve.CreateDefault();
        private readonly Stopwatch rampClock = Stopwatch.StartNew();
        private Timer controlTimer;
        private double lastRampTime, lastTemperature;
        private volatile int desiredFanSpeed, outputRevision;
        private volatile bool closingHardware;
        private bool writingOutput;
        private bool IsControlEnabled
        {
            get
            {
                return modeSelector.SelectedMode != FanControlMode.Off;
            }
        }

        private Panel dashboardPage;
        private Panel settingsPage;
        private Button dashboardNavigationButton;
        private Button settingsNavigationButton;
        private Label labelCpuTemperature;
        private Label labelFanRpm;
        private Label labelAppliedSpeed;
        private Label labelCurveStatus;
        private Label labelManualSpeed;
        private Label labelSettingsStatus;
        private ModeSelector modeSelector;
        private readonly Localizer strings = new Localizer(Properties.Settings.Default.language == "en" ? UiLanguage.English : UiLanguage.Vietnamese);
        private readonly Dictionary<Control, string> translations = new Dictionary<Control, string>();
        private LanguageButton languageButton;
        private string connectionKey = "initializing";
        private bool preview;
        private readonly bool startInTray;
        private ToggleSwitch toggleStartWithWindows;
        private ToggleSwitch toggleAutoRefresh;
        private ToggleSwitch toggleMinimizeToTray;
        private ToggleSwitch toggleTurnOffOnExit;
        private ToggleSwitch toggleSafeSettings;
        private ModernSlider manualSpeedSlider;
        private FanCurveControl fanCurveControl;
        private ComboBox pollingComboBox;
        public Form1() : this(false)
        {
        }

        public Form1(bool startInTray)
        {
            this.startInTray = startInTray;
            InitializeComponent();
            if (startInTray)
            {
                Opacity = 0;
                ShowInTaskbar = false;
                Shown += (sender, args) =>
                {
                    Hide();
                    EnsureTrayIcon();
                    trayIcon.Visible = true;
                    Opacity = 1;
                    ShowInTaskbar = true;
                };
            }

            preview = Environment.GetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW") == "1";
            DoubleBuffered = true;
            BuildInterface();
            LoadSettingsIntoInterface();
            WireEvents();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            Resize += Form1_Resize;
            initializing = false;
            ApplyLanguage();
        }

        private void LoadSettingsIntoInterface()
        {
            var settings = Properties.Settings.Default;
            manualSpeedSlider.Value = Math.Max(0, Math.Min(100, settings.fanSpeed));
            labelManualSpeed.Text = manualSpeedSlider.Value + "%";
            modeSelector.SelectedMode = FanControlMode.Off;
            smoothingToggle.Checked = settings.smoothingEnabled;
            rampUpInput.Value = Math.Max(1, Math.Min(100, settings.rampUpPerSecond));
            rampDownInput.Value = Math.Max(1, Math.Min(100, settings.rampDownPerSecond));
            toggleAutoRefresh.Checked = settings.autoRefreshStats;
            toggleMinimizeToTray.Checked = settings.minimizeToTrayOnClose;
            toggleTurnOffOnExit.Checked = settings.turnOffControlOnExit;
            toggleSafeSettings.Checked = settings.forbidUnsafeSettings;
            fanCurveControl.Curve = FanCurve.Parse(settings.fanCurve);
            committedCurve = FanCurve.Parse(settings.fanCurve);
            ApplyLanguage();
            var polling = Math.Max(1000, Math.Min(10000, settings.pollingIntervalMs));
            pollingComboBox.SelectedItem = pollingComboBox.Items.Cast<PollingOption>().OrderBy(option => Math.Abs(option.Milliseconds - polling)).First();
            try
            {
                toggleStartWithWindows.Checked = !preview && StartupTaskManager.IsEnabled();
            }
            catch
            {
                toggleStartWithWindows.Checked = settings.startWithWindows;
            }

            UpdateControlMode();
        }

        private void WireEvents()
        {
            dashboardNavigationButton.Click += (sender, args) => ShowPage(dashboardPage);
            settingsNavigationButton.Click += (sender, args) => ShowPage(settingsPage);
            modeSelector.SelectionChanged += ToggleFanCurve_CheckedChanged;
            manualSpeedSlider.ValueChanged += (sender, args) => labelManualSpeed.Text = manualSpeedSlider.Value + "%";
            manualSpeedSlider.ValueCommitted += ManualSpeedSlider_ValueCommitted;
            fanCurveControl.CurveChanged += FanCurveControl_CurveChanged;
            toggleStartWithWindows.CheckedChanged += ToggleStartWithWindows_CheckedChanged;
            toggleAutoRefresh.CheckedChanged += ToggleAutoRefresh_CheckedChanged;
            toggleMinimizeToTray.CheckedChanged += ToggleMinimizeToTray_CheckedChanged;
            toggleTurnOffOnExit.CheckedChanged += ToggleTurnOffOnExit_CheckedChanged;
            toggleSafeSettings.CheckedChanged += ToggleSafeSettings_CheckedChanged;
            pollingComboBox.SelectedIndexChanged += PollingComboBox_SelectedIndexChanged;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            if (preview)
            {
                labelCpuTemperature.Text = "61";
                labelFanRpm.Text = "2400";
                fanCurveControl.CurrentTemperature = 61;
                lastTemperature = 61;
                StartControlTimer();
                SetConnectionStatus("firmware", false);
                return;
            }

            try
            {
                asusControl = await Task.Run(() =>
                {
                    lock (hardwareGate)
                        return new AsusControl();
                });
                if (IsDisposed)
                    return;
                StartControlTimer();
                if (modeSelector.SelectedMode == FanControlMode.Manual)
                    ApplyManualSpeed();
                else
                    ApplyFanSpeed(0);
                SetConnectionStatus("firmware", false);
                labelCurveStatus.Text = strings["firmwareActive"];
                RefreshAll(true);
            }
            catch (Exception exception)
            {
                SetConnectionStatus("unavailable", true);
                labelCurveStatus.Text = exception.Message;
            }

            // Upgrade an already-enabled legacy batch task to the direct EXE launch.
            if (toggleStartWithWindows.Checked)
            {
                try
                {
                    if (!StartupTaskManager.IsCurrentConfiguration())
                        StartupTaskManager.SetEnabled(true);
                }
                catch (Exception exception)
                {
                    labelSettingsStatus.Text = strings.Format("startupFailed", exception.Message);
                }
            }

            RestartTimer();
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            closingHardware = true;
            if (Properties.Settings.Default.turnOffControlOnExit && asusControl != null)
                lock (hardwareGate)
                    asusControl.SetFanSpeeds(0);
        }

        private void ToggleFanCurve_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            Properties.Settings.Default.fanCurveEnabled = modeSelector.CurveSelected;
            SaveSettings();
            UpdateControlMode();
            RestartTimer();
            if (!IsControlEnabled)
            {
                ApplyFanSpeed(0);
                SetConnectionStatus("firmware", false);
            }
            else
            {
                if (modeSelector.CurveSelected)
                    RefreshCpuTemperature(true);
                else
                    ApplyManualSpeed();
                SetConnectionStatus(modeSelector.CurveSelected ? "curveActive" : "manualActive", false);
            }
        }

        private void ManualSpeedSlider_ValueCommitted(object sender, EventArgs e)
        {
            var value = ApplySafetyLimits(manualSpeedSlider.Value);
            manualSpeedSlider.Value = value;
            Properties.Settings.Default.fanSpeed = value;
            SaveSettings();
            if (IsControlEnabled && !modeSelector.CurveSelected)
                ApplyFanSpeed(value);
        }

        private void FanCurveControl_CurveChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.fanCurve = fanCurveControl.Curve.ToString();
            committedCurve = FanCurve.Parse(Properties.Settings.Default.fanCurve);
            SaveSettings();
            if (IsControlEnabled && modeSelector.CurveSelected)
                RefreshCpuTemperature(true);
        }

        private void ResetFanCurve_Click(object sender, EventArgs e)
        {
            fanCurveControl.Curve = FanCurve.CreateDefault();
            FanCurveControl_CurveChanged(sender, e);
            labelCurveStatus.Text = strings["defaultCurveRestored"];
        }

        private void ToggleStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            try
            {
                if (preview)
                    return;
                StartupTaskManager.SetEnabled(toggleStartWithWindows.Checked);
                Properties.Settings.Default.startWithWindows = toggleStartWithWindows.Checked;
                SaveSettings();
                labelSettingsStatus.Text = toggleStartWithWindows.Checked ? strings["startupInstalled"] : strings["startupRemoved"];
            }
            catch (Exception exception)
            {
                initializing = true;
                toggleStartWithWindows.Checked = !toggleStartWithWindows.Checked;
                initializing = false;
                labelSettingsStatus.Text = strings.Format("startupFailed", exception.Message);
            }
        }

        private void ToggleAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            Properties.Settings.Default.autoRefreshStats = toggleAutoRefresh.Checked;
            SaveSettings();
            RestartTimer();
        }

        private void ToggleMinimizeToTray_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            Properties.Settings.Default.minimizeToTrayOnClose = toggleMinimizeToTray.Checked;
            SaveSettings();
        }

        private void ToggleTurnOffOnExit_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            Properties.Settings.Default.turnOffControlOnExit = toggleTurnOffOnExit.Checked;
            SaveSettings();
        }

        private void ToggleSafeSettings_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            Properties.Settings.Default.forbidUnsafeSettings = toggleSafeSettings.Checked;
            SaveSettings();
            if (IsControlEnabled)
            {
                if (modeSelector.CurveSelected)
                    RefreshCpuTemperature(true);
                else
                    ApplyManualSpeed();
            }
        }

        private void PollingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing || !(pollingComboBox.SelectedItem is PollingOption option))
                return;
            Properties.Settings.Default.pollingIntervalMs = option.Milliseconds;
            SaveSettings();
            labelSettingsStatus.Text = strings.Format("pollingUpdated", option);
            RestartTimer();
        }

        private void RestartTimer()
        {
            timer?.Stop();
            timer?.Dispose();
            timer = null;
            if (!toggleAutoRefresh.Checked && !IsControlEnabled)
                return;
            var interval = Properties.Settings.Default.pollingIntervalMs;
            interval = Math.Max(1000, Math.Min(10000, interval));
            timer = new Timer
            {
                Interval = interval
            };
            timer.Tick += (sender, args) => RefreshAll(false);
            timer.Start();
        }

        private async void RefreshAll(bool forceStats)
        {
            if (refreshing || asusControl == null || closingHardware || preview)
                return;
            refreshing = true;
            try
            {
                bool readRpm = forceStats || toggleAutoRefresh.Checked;
                var result = await Task.Run(() =>
                {
                    lock (hardwareGate)
                    {
                        if (closingHardware)
                            return null;
                        string rpm = readRpm ? string.Join(" / ", asusControl.GetFanSpeeds()) : null;
                        double temperature = asusControl.Thermal_Read_Cpu_Temperature();
                        return Tuple.Create(rpm, temperature);
                    }
                });
                if (IsDisposed || closingHardware || result == null)
                    return;
                if (result.Item1 != null)
                    labelFanRpm.Text = string.IsNullOrEmpty(result.Item1) ? "—" : result.Item1;
                lastTemperature = result.Item2;
                if (forceStats || toggleAutoRefresh.Checked || modeSelector.CurveSelected)
                    labelCpuTemperature.Text = lastTemperature.ToString("0");
                fanCurveControl.CurrentTemperature = lastTemperature;
                ApplyCurrentCurve();
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                    labelCurveStatus.Text = strings.Format("temperatureUnavailable", exception.Message);
            }
            finally
            {
                refreshing = false;
            }
        }

        private void ApplyCurrentCurve()
        {
            if (!modeSelector.CurveSelected)
                return;
            var activeCurve = fanCurveControl.IsDragging ? committedCurve : fanCurveControl.Curve;
            int requested = ApplySafetyLimits(activeCurve.GetFanSpeed(lastTemperature));
            labelCurveStatus.Text = strings.Format("curveTarget", lastTemperature, requested);
            if (IsControlEnabled)
                ApplyFanSpeed(requested);
        }

        private void RefreshCpuTemperature(bool applyCurve)
        {
            if (preview)
            {
                ApplyCurrentCurve();
                return;
            }

            RefreshAll(true);
        }

        private void StartControlTimer()
        {
            controlTimer?.Stop();
            controlTimer?.Dispose();
            lastRampTime = rampClock.Elapsed.TotalSeconds;
            controlTimer = new Timer
            {
                Interval = 100
            };
            controlTimer.Tick += (sender, args) =>
            {
                var pending = ApplyPendingOutputAsync();
            };
            controlTimer.Start();
        }

        private async Task ApplyPendingOutputAsync()
        {
            if (writingOutput || closingHardware || (!preview && asusControl == null))
                return;
            double now = rampClock.Elapsed.TotalSeconds;
            double elapsed = now - lastRampTime;
            lastRampTime = now;
            int next = speedRamp.Next(desiredFanSpeed, elapsed, (double)rampUpInput.Value, (double)rampDownInput.Value, smoothingToggle.Checked, lastTemperature >= 90);
            if (next == fanSpeed)
                return;
            int revision = outputRevision;
            writingOutput = true;
            try
            {
                bool applied = preview || await Task.Run(() =>
                {
                    lock (hardwareGate)
                    {
                        if (closingHardware || revision != outputRevision)
                            return false;
                        asusControl.SetFanSpeeds(next);
                        return true;
                    }
                });
                if (applied && !IsDisposed)
                {
                    fanSpeed = next;
                    labelAppliedSpeed.Text = next == 0 ? strings["auto"] : next + "%";
                }
                else if (!applied && fanSpeed >= 0)
                    speedRamp.Reset(fanSpeed);
            }
            catch (Exception exception)
            {
                if (!IsDisposed)
                {
                    SetConnectionStatus("unavailable", true);
                    labelCurveStatus.Text = exception.Message;
                }
            }
            finally
            {
                writingOutput = false;
            }
        }

        private void ApplyManualSpeed()
        {
            var value = ApplySafetyLimits(manualSpeedSlider.Value);
            manualSpeedSlider.Value = value;
            ApplyFanSpeed(value);
        }

        private int ApplySafetyLimits(int value)
        {
            if (!toggleSafeSettings.Checked || value == 0)
                return Math.Max(0, Math.Min(100, value));
            return Math.Max(40, Math.Min(99, value));
        }

        private void ApplyFanSpeed(int value)
        {
            if (desiredFanSpeed != value)
            {
                desiredFanSpeed = value;
                outputRevision++;
            }

            if (value == 0)
                speedRamp.Reset(0);
            var pending = ApplyPendingOutputAsync();
        }

        private void UpdateControlMode()
        {
            bool curve = modeSelector.SelectedMode == FanControlMode.Curve;
            bool manual = modeSelector.SelectedMode == FanControlMode.Manual;
            curvePage.Visible = curve;
            manualPage.Visible = manual;
            offPage.Visible = !curve && !manual;
            var active = curve ? curvePage : manual ? manualPage : offPage;
            active.BringToFront();
            manualSpeedSlider.Enabled = manual;
            manualSpeedSlider.Invalidate();
        }

        private void ShowPage(Panel page)
        {
            dashboardPage.Visible = page == dashboardPage;
            settingsPage.Visible = page == settingsPage;
            page.BringToFront();
            StyleNavigationButton(dashboardNavigationButton, page == dashboardPage);
            StyleNavigationButton(settingsNavigationButton, page == settingsPage);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (exitRequested || e.CloseReason != CloseReason.UserClosing || !toggleMinimizeToTray.Checked)
            {
                closingHardware = true;
                controlTimer?.Stop();
                timer?.Stop();
                return;
            }

            e.Cancel = true;
            Hide();
            EnsureTrayIcon();
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(2500, "SimpleFanControl for Asus", strings["trayMessage"], ToolTipIcon.Info);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized || !toggleMinimizeToTray.Checked)
                return;
            Hide();
            EnsureTrayIcon();
            trayIcon.Visible = true;
        }

        private void EnsureTrayIcon()
        {
            if (trayIcon != null)
                return;
            trayIcon = new NotifyIcon
            {
                Icon = Icon,
                Text = "SimpleFanControl for Asus",
                ContextMenu = new ContextMenu(new[] { new MenuItem(strings["open"], (sender, args) => RestoreFromTray()), new MenuItem(strings["exit"], (sender, args) => ExitApplication()) })
            };
            trayIcon.DoubleClick += (sender, args) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            trayIcon.Visible = false;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            exitRequested = true;
            if (trayIcon != null)
                trayIcon.Visible = false;
            Close();
            Application.Exit();
        }

        private void SetConnectionStatus(string text, bool error)
        {
            connectionKey = text;
            if (controlError != null)
            {
                controlError.Visible = error;
                controlError.Text = error ? strings[text] : "";
            }

            if (error)
                labelCurveStatus.Text = strings[text];
        }

        private static void SaveSettings()
        {
            if (Environment.GetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW") != "1")
                Properties.Settings.Default.Save();
        }

        private static void StyleNavigationButton(Button button, bool active)
        {
            var navigation = button as FluentButton;
            if (navigation != null)
            {
                navigation.IsSelected = active;
                navigation.Invalidate();
            }
        }

        private static Label CreateLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, style),
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        private sealed class PollingOption
        {
            public PollingOption(string label, int milliseconds)
            {
                Label = label;
                Milliseconds = milliseconds;
            }

            public string Label { get; }
            public int Milliseconds { get; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}
