using AsusFanControl;
using System;
using System.Diagnostics;
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
        private ToggleSwitch toggleFanCurve;
        private ToggleSwitch toggleStartWithWindows;
        private ToggleSwitch toggleAutoRefresh;
        private ToggleSwitch toggleMinimizeToTray;
        private ToggleSwitch toggleTurnOffOnExit;
        private ToggleSwitch toggleSafeSettings;
        private ModernSlider manualSpeedSlider;
        private FanCurveControl fanCurveControl;
        private ComboBox pollingComboBox;

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
            LoadSettingsIntoInterface();
            WireEvents();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            Resize += Form1_Resize;
            initializing = false;
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

        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White,
                Padding = new Padding(22, 0, 22, 0)
            };

            var logo = new Panel
            {
                Location = new Point(22, 17),
                Size = new Size(38, 38),
                BackColor = Blue
            };
            var logoText = CreateLabel("S", 16, FontStyle.Bold, Color.White);
            logoText.Dock = DockStyle.Fill;
            logoText.TextAlign = ContentAlignment.MiddleCenter;
            logo.Controls.Add(logoText);

            var title = CreateLabel("SimpleFanControl", 15, FontStyle.Bold, DarkBlue);
            title.Location = new Point(72, 14);
            title.AutoSize = true;
            var subtitle = CreateLabel("for Asus", 9, FontStyle.Regular, TextSecondary);
            subtitle.Location = new Point(74, 40);
            subtitle.AutoSize = true;

            labelConnectionStatus = CreateLabel("Initializing / Đang khởi tạo", 8, FontStyle.Bold, TextSecondary);
            labelConnectionStatus.AutoSize = false;
            labelConnectionStatus.Size = new Size(180, 32);
            labelConnectionStatus.TextAlign = ContentAlignment.MiddleCenter;
            labelConnectionStatus.BackColor = Color.FromArgb(235, 241, 249);
            labelConnectionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelConnectionStatus.Location = new Point(ClientSize.Width - 202, 20);
            header.Resize += (sender, args) =>
                labelConnectionStatus.Left = header.ClientSize.Width - labelConnectionStatus.Width - 22;

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(labelConnectionStatus);
            return header;
        }

        private Control BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 190,
                BackColor = Color.FromArgb(248, 251, 255),
                Padding = new Padding(14, 22, 14, 18)
            };

            var navigationLabel = CreateLabel("NAVIGATION / ĐIỀU HƯỚNG", 7.5F, FontStyle.Bold, TextSecondary);
            navigationLabel.Dock = DockStyle.Top;
            navigationLabel.Height = 30;

            dashboardNavigationButton = CreateNavigationButton("Fan control / Quạt");
            settingsNavigationButton = CreateNavigationButton("Settings / Cài đặt");
            dashboardNavigationButton.Dock = DockStyle.Top;
            settingsNavigationButton.Dock = DockStyle.Top;

            var version = CreateLabel("Version 2.1 • EN / VI", 8, FontStyle.Regular, TextSecondary);
            version.Dock = DockStyle.Bottom;
            version.Height = 24;
            version.TextAlign = ContentAlignment.MiddleCenter;

            sidebar.Controls.Add(version);
            sidebar.Controls.Add(settingsNavigationButton);
            sidebar.Controls.Add(dashboardNavigationButton);
            sidebar.Controls.Add(navigationLabel);
            return sidebar;
        }

        private Panel BuildDashboardPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackground,
                Padding = new Padding(24)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackground,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 136));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var titlePanel = new Panel { Dock = DockStyle.Fill };
            var title = CreateLabel("Fan control / Điều khiển quạt", 18, FontStyle.Bold, TextPrimary);
            title.Location = new Point(0, 0);
            title.AutoSize = true;
            var subtitle = CreateLabel("Monitor and tune cooling / Theo dõi và tinh chỉnh hệ thống làm mát.", 9, FontStyle.Regular, TextSecondary);
            subtitle.Location = new Point(2, 31);
            subtitle.AutoSize = true;
            titlePanel.Controls.Add(title);
            titlePanel.Controls.Add(subtitle);

            var stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 14)
            };
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            stats.Controls.Add(CreateStatCard("CPU TEMPERATURE / NHIỆT ĐỘ", "°C", out labelCpuTemperature, 0), 0, 0);
            stats.Controls.Add(CreateStatCard("FAN SPEED / TỐC ĐỘ QUẠT", "RPM", out labelFanRpm, 8), 1, 0);
            stats.Controls.Add(CreateStatCard("APPLIED OUTPUT / MỨC ÁP DỤNG", "TARGET", out labelAppliedSpeed, 8), 2, 0);

            var controlCard = BuildControlCard();
            var curveCard = BuildCurveCard();

            layout.Controls.Add(titlePanel, 0, 0);
            layout.Controls.Add(stats, 0, 1);
            layout.Controls.Add(controlCard, 0, 2);
            layout.Controls.Add(curveCard, 0, 3);
            page.Controls.Add(layout);
            return page;
        }

        private Control BuildControlCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 14)
            };

            var masterTitle = CreateLabel("Fan control / Điều khiển quạt", 11, FontStyle.Bold, TextPrimary);
            masterTitle.Location = new Point(18, 15);
            masterTitle.AutoSize = true;
            var masterSubtitle = CreateLabel("Enable manual or curve mode / Bật chế độ thủ công hoặc biểu đồ.", 8, FontStyle.Regular, TextSecondary);
            masterSubtitle.Location = new Point(19, 38);
            masterSubtitle.AutoSize = true;
            toggleFanControl = new ToggleSwitch
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - 64, 18)
            };
            card.Resize += (sender, args) => toggleFanControl.Left = card.ClientSize.Width - 64;

            var modeLabel = CreateLabel("Fan curve / Biểu đồ", 9, FontStyle.Bold, TextPrimary);
            modeLabel.Location = new Point(19, 76);
            modeLabel.AutoSize = true;
            toggleFanCurve = new ToggleSwitch { Location = new Point(158, 72) };

            manualSpeedSlider = new ModernSlider
            {
                Location = new Point(230, 68),
                Size = new Size(390, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            labelManualSpeed = CreateLabel("90%", 11, FontStyle.Bold, Blue);
            labelManualSpeed.AutoSize = false;
            labelManualSpeed.Size = new Size(60, 32);
            labelManualSpeed.Location = new Point(635, 69);
            labelManualSpeed.TextAlign = ContentAlignment.MiddleRight;
            labelManualSpeed.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            card.Resize += (sender, args) =>
            {
                manualSpeedSlider.Width = Math.Max(160, card.ClientSize.Width - 330);
                labelManualSpeed.Left = card.ClientSize.Width - 78;
            };

            card.Controls.Add(masterTitle);
            card.Controls.Add(masterSubtitle);
            card.Controls.Add(toggleFanControl);
            card.Controls.Add(modeLabel);
            card.Controls.Add(toggleFanCurve);
            card.Controls.Add(manualSpeedSlider);
            card.Controls.Add(labelManualSpeed);
            return card;
        }

        private Control BuildCurveCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(500, 280)
            };

            var title = CreateLabel("CPU temperature fan curve / Biểu đồ nhiệt độ CPU", 11, FontStyle.Bold, TextPrimary);
            title.Location = new Point(18, 14);
            title.AutoSize = true;
            var subtitle = CreateLabel("Drag blue points; orange line = current CPU temp / Kéo điểm xanh; vạch cam = nhiệt độ hiện tại.", 8, FontStyle.Regular, TextSecondary);
            subtitle.Location = new Point(19, 37);
            subtitle.AutoSize = true;
            var resetButton = CreateButton("Reset / Đặt lại", false);
            resetButton.Size = new Size(108, 32);
            resetButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            resetButton.Location = new Point(card.Width - 126, 15);

            fanCurveControl = new FanCurveControl
            {
                Location = new Point(16, 66),
                Size = new Size(500, 220),
                Curve = FanCurve.Parse(Properties.Settings.Default.fanCurve)
            };
            labelCurveStatus = CreateLabel("Waiting for data / Đang chờ dữ liệu nhiệt độ.", 8, FontStyle.Regular, TextSecondary);
            labelCurveStatus.AutoSize = false;
            labelCurveStatus.Location = new Point(19, card.Height - 34);
            labelCurveStatus.Size = new Size(card.Width - 38, 20);
            card.Resize += (sender, args) =>
            {
                resetButton.Left = card.ClientSize.Width - 126;
                fanCurveControl.SetBounds(
                    16,
                    66,
                    Math.Max(300, card.ClientSize.Width - 32),
                    Math.Max(160, card.ClientSize.Height - 116));
                labelCurveStatus.SetBounds(
                    19,
                    card.ClientSize.Height - 31,
                    Math.Max(200, card.ClientSize.Width - 38),
                    20);
            };

            resetButton.Click += ResetFanCurve_Click;
            card.Controls.Add(title);
            card.Controls.Add(subtitle);
            card.Controls.Add(resetButton);
            card.Controls.Add(fanCurveControl);
            card.Controls.Add(labelCurveStatus);
            return card;
        }

        private Panel BuildSettingsPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBackground,
                Padding = new Padding(24),
                Visible = false
            };

            var title = CreateLabel("Settings / Cài đặt", 18, FontStyle.Bold, TextPrimary);
            title.Dock = DockStyle.Top;
            title.Height = 34;
            var subtitle = CreateLabel("Startup, polling, safety and preferences / Khởi động, làm mới, an toàn và tùy chọn.", 9, FontStyle.Regular, TextSecondary);
            subtitle.Dock = DockStyle.Top;
            subtitle.Height = 38;

            var settingsList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 4, 8, 0)
            };
            settingsList.SizeChanged += (sender, args) =>
            {
                foreach (Control control in settingsList.Controls)
                    control.Width = Math.Max(400, settingsList.ClientSize.Width - 25);
            };

            toggleStartWithWindows = new ToggleSwitch();
            toggleAutoRefresh = new ToggleSwitch();
            toggleMinimizeToTray = new ToggleSwitch();
            toggleTurnOffOnExit = new ToggleSwitch();
            toggleSafeSettings = new ToggleSwitch();
            pollingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                Width = 170
            };
            pollingComboBox.Items.AddRange(new object[]
            {
                new PollingOption("1 second / 1 giây", 1000),
                new PollingOption("2 seconds / 2 giây", 2000),
                new PollingOption("3 seconds / 3 giây", 3000),
                new PollingOption("5 seconds / 5 giây", 5000),
                new PollingOption("10 seconds / 10 giây", 10000)
            });

            settingsList.Controls.Add(CreateSettingRow(
                "Start with Windows / Khởi động cùng Windows",
                "Launch after sign-in using Scheduled Task / Tự chạy sau khi đăng nhập bằng Scheduled Task.",
                toggleStartWithWindows));
            settingsList.Controls.Add(CreateSettingRow(
                "Polling interval / Chu kỳ cập nhật",
                "Refresh temperature and RPM / Cập nhật nhiệt độ và RPM. Mặc định: 2 giây.",
                pollingComboBox));
            settingsList.Controls.Add(CreateSettingRow(
                "Auto-refresh statistics / Tự làm mới thống kê",
                "Continuously update dashboard data / Liên tục cập nhật dữ liệu trên Dashboard.",
                toggleAutoRefresh));
            settingsList.Controls.Add(CreateSettingRow(
                "Minimize to system tray / Thu nhỏ xuống khay",
                "Close keeps the app running in tray / Bấm X vẫn giữ ứng dụng chạy dưới system tray.",
                toggleMinimizeToTray));
            settingsList.Controls.Add(CreateSettingRow(
                "Reset fan control on exit / Reset quạt khi thoát",
                "Return fans to ASUS firmware / Trả quyền điều khiển quạt về firmware ASUS.",
                toggleTurnOffOnExit));
            settingsList.Controls.Add(CreateSettingRow(
                "Safe output limits / Giới hạn an toàn",
                "Limit commands to 40–99% / Giới hạn lệnh điều khiển trong khoảng 40–99%.",
                toggleSafeSettings));

            var actions = new ModernCard { Height = 82, Width = 700 };
            var refreshButton = CreateButton("Refresh / Làm mới", true);
            refreshButton.Location = new Point(18, 22);
            refreshButton.Size = new Size(118, 38);
            refreshButton.Click += (sender, args) => RefreshAll(true);
            var projectButton = CreateButton("Project / Dự án", false);
            projectButton.Location = new Point(146, 22);
            projectButton.Size = new Size(118, 38);
            projectButton.Click += (sender, args) =>
                Process.Start("https://github.com/Karmel0x/AsusFanControl");
            var exitButton = CreateButton("Exit / Thoát", false);
            exitButton.Size = new Size(132, 38);
            exitButton.Location = new Point(actions.Width - 150, 22);
            exitButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exitButton.Click += (sender, args) => ExitApplication();
            actions.Controls.Add(refreshButton);
            actions.Controls.Add(projectButton);
            actions.Controls.Add(exitButton);
            settingsList.Controls.Add(actions);

            labelSettingsStatus = CreateLabel(string.Empty, 8, FontStyle.Regular, TextSecondary);
            labelSettingsStatus.Dock = DockStyle.Bottom;
            labelSettingsStatus.Height = 26;

            page.Controls.Add(settingsList);
            page.Controls.Add(labelSettingsStatus);
            page.Controls.Add(subtitle);
            page.Controls.Add(title);
            return page;
        }

        private void LoadSettingsIntoInterface()
        {
            var settings = Properties.Settings.Default;
            manualSpeedSlider.Value = Math.Max(0, Math.Min(100, settings.fanSpeed));
            labelManualSpeed.Text = manualSpeedSlider.Value + "%";
            toggleFanCurve.Checked = settings.fanCurveEnabled;
            toggleAutoRefresh.Checked = settings.autoRefreshStats;
            toggleMinimizeToTray.Checked = settings.minimizeToTrayOnClose;
            toggleTurnOffOnExit.Checked = settings.turnOffControlOnExit;
            toggleSafeSettings.Checked = settings.forbidUnsafeSettings;
            fanCurveControl.Curve = FanCurve.Parse(settings.fanCurve);

            var polling = Math.Max(1000, Math.Min(10000, settings.pollingIntervalMs));
            pollingComboBox.SelectedItem = pollingComboBox.Items
                .Cast<PollingOption>()
                .OrderBy(option => Math.Abs(option.Milliseconds - polling))
                .First();

            try
            {
                toggleStartWithWindows.Checked = StartupTaskManager.IsEnabled();
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
            toggleFanCurve.CheckedChanged += ToggleFanCurve_CheckedChanged;
            manualSpeedSlider.ValueChanged += (sender, args) =>
                labelManualSpeed.Text = manualSpeedSlider.Value + "%";
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
            if (Environment.GetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW") == "1")
                return;

            try
            {
                asusControl = new AsusControl();
                ApplyFanSpeed(0);
                SetConnectionStatus("Firmware / Tự động", false);
                labelCurveStatus.Text = "Firmware fan control active / Firmware đang điều khiển quạt.";
                RefreshAll(true);
            }
            catch (Exception exception)
            {
                SetConnectionStatus("Hardware unavailable / Không khả dụng", true);
                labelCurveStatus.Text = exception.Message;
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

            if (!toggleFanControl.Checked)
            {
                ApplyFanSpeed(0);
                SetConnectionStatus("Firmware / Tự động", false);
            }
            else if (toggleFanCurve.Checked)
            {
                RefreshCpuTemperature(true);
                SetConnectionStatus("Curve active / Đang chạy biểu đồ", false);
            }
            else
            {
                ApplyManualSpeed();
                SetConnectionStatus("Manual / Thủ công", false);
            }
        }

        private void ToggleFanCurve_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.fanCurveEnabled = toggleFanCurve.Checked;
            SaveSettings();
            UpdateControlMode();
            RestartTimer();

            if (toggleFanControl.Checked)
            {
                if (toggleFanCurve.Checked)
                    RefreshCpuTemperature(true);
                else
                    ApplyManualSpeed();
            }
        }

        private void ManualSpeedSlider_ValueCommitted(object sender, EventArgs e)
        {
            var value = ApplySafetyLimits(manualSpeedSlider.Value);
            manualSpeedSlider.Value = value;
            Properties.Settings.Default.fanSpeed = value;
            SaveSettings();

            if (toggleFanControl.Checked && !toggleFanCurve.Checked)
                ApplyFanSpeed(value);
        }

        private void FanCurveControl_CurveChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.fanCurve = fanCurveControl.Curve.ToString();
            SaveSettings();

            if (toggleFanControl.Checked && toggleFanCurve.Checked)
                RefreshCpuTemperature(true);
        }

        private void ResetFanCurve_Click(object sender, EventArgs e)
        {
            fanCurveControl.Curve = FanCurve.CreateDefault();
            FanCurveControl_CurveChanged(sender, e);
            labelCurveStatus.Text = "Default curve restored / Đã khôi phục biểu đồ mặc định.";
        }

        private void ToggleStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (initializing)
                return;

            try
            {
                StartupTaskManager.SetEnabled(toggleStartWithWindows.Checked);
                Properties.Settings.Default.startWithWindows = toggleStartWithWindows.Checked;
                SaveSettings();
                labelSettingsStatus.Text = toggleStartWithWindows.Checked
                    ? "Startup task installed / Đã bật khởi động cùng Windows."
                    : "Startup task removed / Đã tắt khởi động cùng Windows.";
            }
            catch (Exception exception)
            {
                initializing = true;
                toggleStartWithWindows.Checked = !toggleStartWithWindows.Checked;
                initializing = false;
                labelSettingsStatus.Text = "Startup setting failed / Cài đặt khởi động thất bại: " + exception.Message;
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
                if (toggleFanCurve.Checked)
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
            labelSettingsStatus.Text = "Polling updated / Đã đổi chu kỳ: " + option + ".";
            RestartTimer();
        }

        private void RestartTimer()
        {
            timer?.Stop();
            timer?.Dispose();
            timer = null;

            if (!toggleAutoRefresh.Checked && !toggleFanCurve.Checked)
                return;

            var interval = Properties.Settings.Default.pollingIntervalMs;
            interval = Math.Max(1000, Math.Min(10000, interval));
            timer = new Timer { Interval = interval };
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

                if (forceStats || toggleAutoRefresh.Checked || toggleFanCurve.Checked)
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
                labelFanRpm.Text = speeds.Count == 0
                    ? "—"
                    : string.Join(" / ", speeds);
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

                if (!applyCurve || !toggleFanCurve.Checked)
                    return;

                var requestedSpeed = ApplySafetyLimits(
                    fanCurveControl.Curve.GetFanSpeed(temperature));
                labelCurveStatus.Text = string.Format(
                    "CPU {0}°C  →  target / mục tiêu {1}%", temperature, requestedSpeed);

                if (toggleFanControl.Checked)
                    ApplyFanSpeed(requestedSpeed);
            }
            catch (Exception exception)
            {
                labelCpuTemperature.Text = "—";
                labelCurveStatus.Text = "Temperature unavailable / Không đọc được nhiệt độ: " + exception.Message;
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
            labelAppliedSpeed.Text = value == 0 ? "AUTO" : value + "%";
            if (fanSpeed == value || asusControl == null)
                return;

            fanSpeed = value;
            asusControl.SetFanSpeeds(value);
        }

        private void UpdateControlMode()
        {
            manualSpeedSlider.Enabled = !toggleFanCurve.Checked;
            manualSpeedSlider.Cursor = manualSpeedSlider.Enabled
                ? Cursors.Hand
                : Cursors.Default;
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
            if (exitRequested || !toggleMinimizeToTray.Checked)
                return;

            e.Cancel = true;
            Hide();
            EnsureTrayIcon();
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(
                2500,
                "SimpleFanControl for Asus",
                "Still running in system tray / Ứng dụng vẫn chạy dưới khay hệ thống.",
                ToolTipIcon.Info);
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
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Open / Mở", (sender, args) => RestoreFromTray()),
                    new MenuItem("Exit / Thoát", (sender, args) => ExitApplication())
                })
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
            labelConnectionStatus.Text = text;
            labelConnectionStatus.ForeColor = error
                ? Color.FromArgb(180, 45, 55)
                : DarkBlue;
            labelConnectionStatus.BackColor = error
                ? Color.FromArgb(255, 235, 238)
                : Color.FromArgb(231, 242, 255);
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private ModernCard CreateStatCard(
            string title,
            string unit,
            out Label valueLabel,
            int leftMargin)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(leftMargin, 0, 0, 0)
            };
            var titleLabel = CreateLabel(title, 8, FontStyle.Bold, TextSecondary);
            titleLabel.Location = new Point(18, 15);
            titleLabel.AutoSize = true;
            valueLabel = CreateLabel("—", 20, FontStyle.Bold, DarkBlue);
            valueLabel.Location = new Point(18, 39);
            valueLabel.AutoSize = true;
            var unitLabel = CreateLabel(unit, 8, FontStyle.Bold, TextSecondary);
            unitLabel.AutoSize = false;
            unitLabel.Size = new Size(62, 20);
            unitLabel.TextAlign = ContentAlignment.MiddleRight;
            unitLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            unitLabel.Location = new Point(card.Width - 80, 48);
            card.Resize += (sender, args) =>
                unitLabel.Left = card.ClientSize.Width - unitLabel.Width - 18;
            card.Controls.Add(titleLabel);
            card.Controls.Add(valueLabel);
            card.Controls.Add(unitLabel);
            return card;
        }

        private ModernCard CreateSettingRow(string title, string description, Control action)
        {
            var row = new ModernCard { Height = 74, Width = 700 };
            var titleLabel = CreateLabel(title, 10, FontStyle.Bold, TextPrimary);
            titleLabel.Location = new Point(18, 14);
            titleLabel.AutoSize = true;
            var descriptionLabel = CreateLabel(description, 8, FontStyle.Regular, TextSecondary);
            descriptionLabel.Location = new Point(19, 39);
            descriptionLabel.AutoSize = true;
            action.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            action.Location = new Point(row.Width - action.Width - 20, (row.Height - action.Height) / 2);
            row.Resize += (sender, args) =>
                action.Left = row.ClientSize.Width - action.Width - 20;
            row.Controls.Add(titleLabel);
            row.Controls.Add(descriptionLabel);
            row.Controls.Add(action);
            return row;
        }

        private Button CreateNavigationButton(string text)
        {
            return new Button
            {
                Text = text,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
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

        private static Label CreateLabel(
            string text,
            float size,
            FontStyle style,
            Color color)
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
