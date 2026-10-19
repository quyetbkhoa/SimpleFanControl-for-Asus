using AsusFanControl;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    public partial class Form1 : Form
    {
        private static readonly Color Blue = Color.FromArgb(21, 112, 239);
        private static readonly Color DarkBlue = Color.FromArgb(14, 56, 112);
        private static readonly Color TextPrimary = Color.FromArgb(28, 39, 55);
        private static readonly Color TextSecondary = Color.FromArgb(103, 116, 135);
        private static readonly Color PageBackground = Color.FromArgb(245, 248, 252);
        private AsusControl asusControl;
        private int fanSpeed = -1;
        private Timer timer;
        private NotifyIcon trayIcon;
        private bool exitRequested;
        private bool initializing = true;
        private bool refreshing;
        private Panel dashboardPage;
        private Panel settingsPage;
        private Button dashboardNavigationButton;
        private Button settingsNavigationButton;
        private Label labelCpuTemperature;
        private Label labelFanRpm;
        private Label labelAppliedSpeed;
        private Label labelConnectionStatus;
        private Label labelCurveStatus;
        private Label labelManualSpeed;
        private Label labelSettingsStatus;
        private ToggleSwitch toggleFanControl;
        private ModeSelector modeSelector;
        private readonly Localizer strings = new Localizer(Properties.Settings.Default.language == "en" ? UiLanguage.English : UiLanguage.Vietnamese);
        private readonly Dictionary<Control, string> translations = new Dictionary<Control, string>();
        private LanguageButton languageButton;
        private string connectionKey = "initializing";
        private bool preview;
        private readonly bool startInTray;
        private Label modeTitle;
        private Label modeDescription;
        private Label controlNote;
        private ToggleSwitch toggleStartWithWindows;
        private ToggleSwitch toggleAutoRefresh;
        private ToggleSwitch toggleMinimizeToTray;
        private ToggleSwitch toggleTurnOffOnExit;
        private ToggleSwitch toggleSafeSettings;
        private ModernSlider manualSpeedSlider;
        private FanCurveControl fanCurveControl;
        private ComboBox pollingComboBox;
        public Form1() : this(false) { }

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


        private void BuildInterface()
        {
            var header = BuildHeader();
            var sidebar = BuildSidebar();
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackground
            };
            dashboardPage = BuildDashboardPage();
            settingsPage = BuildSettingsPage();
            contentHost.Controls.Add(settingsPage);
            contentHost.Controls.Add(dashboardPage);
            Controls.Add(contentHost);
            Controls.Add(sidebar);
            Controls.Add(header);
            ShowPage(dashboardPage);
        }

        private Label LocalLabel(string key, float size = 9, bool bold = false, Color? color = null)
        {
            var label = CreateLabel(strings[key], size, bold ? FontStyle.Bold : FontStyle.Regular, color ?? TextPrimary);
            label.AutoSize = true;
            translations.Add(label, key);
            return label;
        }

        private Button LocalButton(string key, bool primary = false)
        {
            var button = CreateButton(strings[key], primary);
            translations.Add(button, key);
            return button;
        }

        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White
            };
            var logo = CreateLabel("S", 21, FontStyle.Bold, Color.White);
            logo.BackColor = Blue;
            logo.SetBounds(24, 18, 42, 42);
            logo.TextAlign = ContentAlignment.MiddleCenter;
            var title = CreateLabel("SimpleFanControl", 17, FontStyle.Bold, DarkBlue);
            title.AutoSize = true;
            title.Location = new Point(80, 15);
            var subtitle = CreateLabel("for Asus", 9, FontStyle.Regular, TextSecondary);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(82, 47);
            languageButton = new LanguageButton
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            labelConnectionStatus = CreateLabel("", 9, FontStyle.Bold, DarkBlue);
            labelConnectionStatus.TextAlign = ContentAlignment.MiddleCenter;
            header.Controls.AddRange(new Control[] { logo, title, subtitle, labelConnectionStatus, languageButton });
            header.Resize += (o, e) =>
            {
                var scale = header.DeviceDpi / 96f;
                languageButton.SetBounds(header.Width - (int)(104 * scale), (int)(21 * scale), (int)(80 * scale), (int)(38 * scale));
                labelConnectionStatus.SetBounds(languageButton.Left - (int)(220 * scale), (int)(25 * scale), (int)(200 * scale), (int)(30 * scale));
            };
            languageButton.Click += (o, e) =>
            {
                strings.Language = strings.Language == UiLanguage.Vietnamese ? UiLanguage.English : UiLanguage.Vietnamese;
                if (!preview)
                {
                    Properties.Settings.Default.language = strings.Language == UiLanguage.English ? "en" : "vi";
                    SaveSettings();
                }

                ApplyLanguage();
            };
            return header;
        }

        private Control BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 192,
                BackColor = Color.White,
                Padding = new Padding(16, 24, 16, 20)
            };
            dashboardNavigationButton = CreateNavigationButton("");
            settingsNavigationButton = CreateNavigationButton("");
            translations.Add(dashboardNavigationButton, "navControl");
            translations.Add(settingsNavigationButton, "navSettings");
            dashboardNavigationButton.Dock = DockStyle.Top;
            settingsNavigationButton.Dock = DockStyle.Top;
            var version = CreateLabel("SimpleFanControl\nv2.2.1", 9, FontStyle.Regular, TextSecondary);
            version.Dock = DockStyle.Bottom;
            version.Height = 48;
            sidebar.Controls.AddRange(new Control[] { version, settingsNavigationButton, dashboardNavigationButton });
            return sidebar;
        }

        private Panel BuildDashboardPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 24, 20),
                BackColor = PageBackground
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new Panel
            {
                Dock = DockStyle.Fill
            };
            var title = LocalLabel("dashboardTitle", 23, true);
            var subtitle = LocalLabel("dashboardSubtitle", 9, false, TextSecondary);
            subtitle.Location = new Point(2, 43);
            heading.Controls.AddRange(new Control[] { title, subtitle });
            var stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 18)
            };
            for (int i = 0; i < 3; i++)
                stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            stats.Controls.Add(Stat("cpuTemperature", "°C", out labelCpuTemperature), 0, 0);
            stats.Controls.Add(Stat("fanSpeed", "RPM", out labelFanRpm), 1, 0);
            stats.Controls.Add(Stat("appliedOutput", "", out labelAppliedSpeed), 2, 0);
            labelAppliedSpeed.Font = new Font("Segoe UI", 21, FontStyle.Bold);
            labelFanRpm.Font = new Font("Segoe UI", 21, FontStyle.Bold);
            var work = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            work.Controls.Add(BuildControlCard(), 0, 0);
            work.Controls.Add(BuildCurveCard(), 1, 0);
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(stats, 0, 1);
            layout.Controls.Add(work, 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private ModernCard Stat(string key, string unit, out Label value)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(20, 15, 20, 10)
            };
            var title = LocalLabel(key, 9, false, TextSecondary);
            title.Dock = DockStyle.Top;
            value = CreateLabel("—", 27, FontStyle.Bold, DarkBlue);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.MiddleLeft;
            var units = CreateLabel(unit, 9, FontStyle.Bold, TextSecondary);
            units.Dock = DockStyle.Right;
            units.Width = 50;
            units.TextAlign = ContentAlignment.MiddleRight;
            card.Controls.AddRange(new Control[] { value, units, title });
            return card;
        }

        private Control BuildControlCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(20),
                AutoScroll = true
            };
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 410,
                ColumnCount = 1,
                RowCount = 8
            };
            foreach (var height in new[]
            {
                48,
                66,
                54,
                40,
                66,
                40,
                50
            }

            )
                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = new Panel
            {
                Dock = DockStyle.Fill
            };
            var title = LocalLabel("coolingControl", 10, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            toggleFanControl = new ToggleSwitch
            {
                Dock = DockStyle.Right
            };
            top.Controls.AddRange(new Control[] { title, toggleFanControl });
            var hint = LocalLabel("coolingControlDescription", 9, false, TextSecondary);
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            modeSelector = new ModeSelector
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            var speedTitle = LocalLabel("manualTitle", 10, true);
            speedTitle.Dock = DockStyle.Bottom;
            var description = LocalLabel("manualDescription", 9, false, TextSecondary);
            description.AutoSize = false;
            description.Dock = DockStyle.Fill;
            modeTitle = speedTitle;
            modeDescription = description;
            manualSpeedSlider = new ModernSlider
            {
                Dock = DockStyle.Fill
            };
            labelManualSpeed = CreateLabel("90%", 24, FontStyle.Bold, Blue);
            labelManualSpeed.Dock = DockStyle.Fill;
            var note = LocalLabel("firmwareActive", 9, false, TextSecondary);
            note.AutoSize = false;
            note.Dock = DockStyle.Fill;
            controlNote = note;
            stack.Controls.Add(top, 0, 0);
            stack.Controls.Add(hint, 0, 1);
            stack.Controls.Add(modeSelector, 0, 2);
            stack.Controls.Add(speedTitle, 0, 3);
            stack.Controls.Add(description, 0, 4);
            stack.Controls.Add(manualSpeedSlider, 0, 5);
            stack.Controls.Add(labelManualSpeed, 0, 6);
            stack.Controls.Add(note, 0, 7);
            card.Controls.Add(stack);
            return card;
        }

        private Control BuildCurveCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 18, 20, 16)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            var head = new Panel
            {
                Dock = DockStyle.Fill
            };
            var title = LocalLabel("curveTitle", 11, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            var reset = LocalButton("resetCurve");
            reset.Dock = DockStyle.Right;
            reset.Width = 80;
            reset.Click += ResetFanCurve_Click;
            head.Controls.AddRange(new Control[] { title, reset });
            var hint = LocalLabel("curveSubtitle", 9, false, TextSecondary);
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            fanCurveControl = new FanCurveControl
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            labelCurveStatus = CreateLabel("", 9, FontStyle.Regular, TextSecondary);
            labelCurveStatus.Dock = DockStyle.Fill;
            labelCurveStatus.TextAlign = ContentAlignment.BottomLeft;
            layout.Controls.Add(head, 0, 0);
            layout.Controls.Add(hint, 0, 1);
            layout.Controls.Add(fanCurveControl, 0, 2);
            layout.Controls.Add(labelCurveStatus, 0, 3);
            card.Controls.Add(layout);
            return card;
        }

        private Panel BuildSettingsPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 24, 20),
                BackColor = PageBackground,
                Visible = false
            };
            var title = LocalLabel("settingsTitle", 23, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Top;
            title.Height = 46;
            var subtitle = LocalLabel("settingsSubtitle", 9, false, TextSecondary);
            subtitle.AutoSize = false;
            subtitle.Dock = DockStyle.Top;
            subtitle.Height = 40;
            var list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 0, 12, 0)
            };
            list.SizeChanged += (o, e) =>
            {
                foreach (Control row in list.Controls)
                    row.Width = Math.Max(300, list.ClientSize.Width - 32);
            };
            toggleStartWithWindows = new ToggleSwitch();
            toggleAutoRefresh = new ToggleSwitch();
            toggleMinimizeToTray = new ToggleSwitch();
            toggleTurnOffOnExit = new ToggleSwitch();
            toggleSafeSettings = new ToggleSwitch();
            pollingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 135,
                Font = new Font("Segoe UI", 10)
            };
            AddSetting(list, "startWindows", "startWindowsDescription", toggleStartWithWindows);
            AddSetting(list, "polling", "pollingDescription", pollingComboBox);
            AddSetting(list, "autoRefresh", "autoRefreshDescription", toggleAutoRefresh);
            AddSetting(list, "minimizeTray", "minimizeTrayDescription", toggleMinimizeToTray);
            AddSetting(list, "resetOnExit", "resetOnExitDescription", toggleTurnOffOnExit);
            AddSetting(list, "safeLimits", "safeLimitsDescription", toggleSafeSettings);
            var actions = new FlowLayoutPanel
            {
                Height = 48,
                Width = 650
            };
            var refresh = LocalButton("refreshNow", true);
            refresh.Size = new Size(155, 38);
            refresh.Click += (o, e) => RefreshAll(true);
            var exit = LocalButton("exit");
            exit.Size = new Size(155, 38);
            exit.Click += (o, e) => ExitApplication();
            actions.Controls.AddRange(new Control[] { refresh, exit });
            list.Controls.Add(actions);
            labelSettingsStatus = CreateLabel("", 9, FontStyle.Regular, TextSecondary);
            labelSettingsStatus.Dock = DockStyle.Bottom;
            labelSettingsStatus.Height = 28;
            page.Controls.AddRange(new Control[] { list, labelSettingsStatus, subtitle, title });
            return page;
        }

        private void AddSetting(FlowLayoutPanel list, string title, string hint, Control action)
        {
            var row = new ModernCard
            {
                Width = 750,
                Height = 86,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(20, 13, 20, 12)
            };
            var actionHost = new Panel
            {
                Dock = DockStyle.Right,
                Width = action.Width + 18
            };
            action.Location = new Point(10, 12);
            actionHost.Controls.Add(action);
            var copy = new Panel
            {
                Dock = DockStyle.Fill
            };
            var name = LocalLabel(title, 10, true);
            name.AutoSize = false;
            name.Dock = DockStyle.Top;
            name.Height = 26;
            var description = LocalLabel(hint, 9, false, TextSecondary);
            description.AutoSize = false;
            description.Dock = DockStyle.Fill;
            copy.Controls.AddRange(new Control[] { description, name });
            row.Controls.AddRange(new Control[] { copy, actionHost });
            list.Controls.Add(row);
        }

        private void ApplyLanguage()
        {
            foreach (var pair in translations)
                pair.Key.Text = strings[pair.Value];
            languageButton.Language = strings.Language;
            languageButton.AccessibleName = strings.Language == UiLanguage.Vietnamese ? "Switch to English" : "Chuyển sang tiếng Việt";
            modeSelector.ManualText = strings["manual"];
            modeSelector.CurveText = strings["curve"];
            modeSelector.Invalidate();
            fanCurveControl.TemperatureAxisTitle = strings["temperatureAxis"];
            fanCurveControl.SpeedAxisTitle = strings["speedAxis"];
            fanCurveControl.Invalidate();
            var wasInitializing = initializing;
            initializing = true;
            var milliseconds = (pollingComboBox.SelectedItem as PollingOption)?.Milliseconds ?? Properties.Settings.Default.pollingIntervalMs;
            pollingComboBox.Items.Clear();
            foreach (var interval in new[]
            {
                1000,
                2000,
                3000,
                5000,
                10000
            }

            )
                pollingComboBox.Items.Add(new PollingOption(strings.Format("seconds", interval / 1000), interval));
            pollingComboBox.SelectedItem = pollingComboBox.Items.Cast<PollingOption>().OrderBy(p => Math.Abs(p.Milliseconds - milliseconds)).First();
            initializing = wasInitializing;
            SetConnectionStatus(connectionKey, connectionKey == "unavailable");
            labelCurveStatus.Text = strings[toggleFanControl.Checked ? (modeSelector.CurveSelected ? "curveActive" : "manualActive") : "firmwareActive"];
            if (fanSpeed <= 0)
                labelAppliedSpeed.Text = strings["auto"];
            if (trayIcon != null)
            {
                trayIcon.ContextMenu.MenuItems[0].Text = strings["open"];
                trayIcon.ContextMenu.MenuItems[1].Text = strings["exit"];
            }

            UpdateControlMode();
        }

        private void LoadSettingsIntoInterface()
        {
            var settings = Properties.Settings.Default;
            manualSpeedSlider.Value = Math.Max(0, Math.Min(100, settings.fanSpeed));
            labelManualSpeed.Text = manualSpeedSlider.Value + "%";
            modeSelector.CurveSelected = settings.fanCurveEnabled;
            toggleAutoRefresh.Checked = settings.autoRefreshStats;
            toggleMinimizeToTray.Checked = settings.minimizeToTrayOnClose;
            toggleTurnOffOnExit.Checked = settings.turnOffControlOnExit;
            toggleSafeSettings.Checked = settings.forbidUnsafeSettings;
            fanCurveControl.Curve = FanCurve.Parse(settings.fanCurve);
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
            toggleFanControl.CheckedChanged += ToggleFanControl_CheckedChanged;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            if (preview)
            {
                labelCpuTemperature.Text = "61";
                labelFanRpm.Text = "2400";
                fanCurveControl.CurrentTemperature = 61;
                SetConnectionStatus("firmware", false);
                return;
            }

            try
            {
                asusControl = new AsusControl();
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
            if (Properties.Settings.Default.turnOffControlOnExit && asusControl != null)
                asusControl.SetFanSpeeds(0);
        }

        private void ToggleFanControl_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;
            UpdateControlMode();
            if (!toggleFanControl.Checked)
            {
                ApplyFanSpeed(0);
                SetConnectionStatus("firmware", false);
            }
            else if (modeSelector.CurveSelected)
            {
                RefreshCpuTemperature(true);
                SetConnectionStatus("curveActive", false);
            }
            else
            {
                ApplyManualSpeed();
                SetConnectionStatus("manualActive", false);
            }
        }

        private void ToggleFanCurve_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.fanCurveEnabled = modeSelector.CurveSelected;
            SaveSettings();
            UpdateControlMode();
            RestartTimer();
            if (toggleFanControl.Checked)
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
            if (toggleFanControl.Checked && !modeSelector.CurveSelected)
                ApplyFanSpeed(value);
        }

        private void FanCurveControl_CurveChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.fanCurve = fanCurveControl.Curve.ToString();
            SaveSettings();
            if (toggleFanControl.Checked && modeSelector.CurveSelected)
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
            if (toggleFanControl.Checked)
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
            if (!toggleAutoRefresh.Checked && !modeSelector.CurveSelected)
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

        private void RefreshAll(bool forceStats)
        {
            if (refreshing || asusControl == null)
                return;
            refreshing = true;
            try
            {
                if (forceStats || toggleAutoRefresh.Checked)
                    RefreshFanSpeeds();
                if (forceStats || toggleAutoRefresh.Checked || modeSelector.CurveSelected)
                    RefreshCpuTemperature(true);
            }
            finally
            {
                refreshing = false;
            }
        }

        private void RefreshFanSpeeds()
        {
            try
            {
                var speeds = asusControl.GetFanSpeeds();
                labelFanRpm.Text = speeds.Count == 0 ? "—" : string.Join(" / ", speeds);
            }
            catch (Exception exception)
            {
                labelFanRpm.Text = "N/A";
                labelCurveStatus.Text = exception.Message;
            }
        }

        private void RefreshCpuTemperature(bool applyCurve)
        {
            try
            {
                var temperature = asusControl.Thermal_Read_Cpu_Temperature();
                labelCpuTemperature.Text = temperature.ToString();
                fanCurveControl.CurrentTemperature = temperature;
                if (!applyCurve || !modeSelector.CurveSelected)
                    return;
                var requestedSpeed = ApplySafetyLimits(fanCurveControl.Curve.GetFanSpeed(temperature));
                labelCurveStatus.Text = string.Format(strings["curveTarget"], temperature, requestedSpeed);
                if (toggleFanControl.Checked)
                    ApplyFanSpeed(requestedSpeed);
            }
            catch (Exception exception)
            {
                labelCpuTemperature.Text = "—";
                labelCurveStatus.Text = strings.Format("temperatureUnavailable", exception.Message);
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
            labelAppliedSpeed.Text = value == 0 ? strings["auto"] : value + "%";
            if (fanSpeed == value || asusControl == null)
                return;
            fanSpeed = value;
            asusControl.SetFanSpeeds(value);
        }

        private void UpdateControlMode()
        {
            modeTitle.Text = strings[modeSelector.CurveSelected ? "curveModeTitle" : "manualTitle"];
            modeDescription.Text = strings[modeSelector.CurveSelected ? "curveModeDescription" : "manualDescription"];
            controlNote.Text = strings[toggleFanControl.Checked ? (modeSelector.CurveSelected ? "curveActive" : "manualActive") : "firmwareActive"];
            manualSpeedSlider.Visible = !modeSelector.CurveSelected;
            labelManualSpeed.Visible = !modeSelector.CurveSelected;
            manualSpeedSlider.Enabled = !modeSelector.CurveSelected;
            manualSpeedSlider.Cursor = manualSpeedSlider.Enabled ? Cursors.Hand : Cursors.Default;
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
                return;
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
            labelConnectionStatus.Text = strings[text];
            labelConnectionStatus.ForeColor = error ? Color.FromArgb(180, 45, 55) : DarkBlue;
            labelConnectionStatus.BackColor = error ? Color.FromArgb(255, 235, 238) : Color.FromArgb(231, 242, 255);
        }

        private static void SaveSettings()
        {
            if (Environment.GetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW") != "1")
                Properties.Settings.Default.Save();
        }

        private Button CreateNavigationButton(string text)
        {
            return new Button
            {
                Text = text,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0
                },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 6)
            };
        }

        private static void StyleNavigationButton(Button button, bool active)
        {
            button.BackColor = active ? Color.FromArgb(226, 239, 255) : Color.Transparent;
            button.ForeColor = active ? Blue : TextSecondary;
        }

        private static Button CreateButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = primary ? Blue : Color.White,
                ForeColor = primary ? Color.White : DarkBlue,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = primary ? Blue : Color.FromArgb(198, 211, 227);
            button.FlatAppearance.BorderSize = 1;
            return button;
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
