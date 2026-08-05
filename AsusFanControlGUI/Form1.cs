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

        // Language & Localization
        private string currentLanguage = "VI";
        private Button langEnButton;
        private Button langViButton;

        private Label sidebarNavLabel;
        private Label sidebarVersionLabel;
        private Label dashboardTitleLabel;
        private Label dashboardSubtitleLabel;
        private Label statCpuTempTitleLabel;
        private Label statFanRpmTitleLabel;
        private Label statAppliedSpeedTitleLabel;
        private Label controlTitleLabel;
        private Label controlSubtitleLabel;
        private Label modeLabel;
        private Label curveTitleLabel;
        private Label curveSubtitleLabel;
        private Button curveResetButton;

        private Label settingsTitleLabel;
        private Label settingsSubtitleLabel;
        private Label settingStartWithWinTitleLabel;
        private Label settingStartWithWinDescLabel;
        private Label settingPollingTitleLabel;
        private Label settingPollingDescLabel;
        private Label settingAutoRefreshTitleLabel;
        private Label settingAutoRefreshDescLabel;
        private Label settingMinToTrayTitleLabel;
        private Label settingMinToTrayDescLabel;
        private Label settingTurnOffExitTitleLabel;
        private Label settingTurnOffExitDescLabel;
        private Label settingSafeLimitsTitleLabel;
        private Label settingSafeLimitsDescLabel;
        private Button settingsRefreshButton;
        private Button settingsProjectButton;
        private Button settingsExitButton;

        // State trackers for localized dynamic messages
        private enum ConnectionState { Initializing, FirmwareAuto, Manual, CurveActive, HardwareUnavailable }
        private ConnectionState currentConnectionState = ConnectionState.Initializing;

        private enum CurveStatusState { FirmwareActive, Waiting, RestoredDefault, TargetInfo, TempUnavailable }
        private CurveStatusState currentCurveState = CurveStatusState.FirmwareActive;
        private int currentCurveTemp;
        private int currentCurveTargetSpeed;
        private string currentCurveError = string.Empty;

        private enum SettingsStatusState { None, TaskInstalled, TaskRemoved, TaskFailed, PollingUpdated }
        private SettingsStatusState currentSettingsStatus = SettingsStatusState.None;
        private string currentSettingsParam = string.Empty;

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

            // EN / VI Language Switcher right next to App Name
            var langSwitchPanel = new Panel
            {
                Location = new Point(235, 20),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(240, 244, 250)
            };

            langEnButton = new Button
            {
                Text = "EN",
                Size = new Size(42, 26),
                Location = new Point(3, 3),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            langViButton = new Button
            {
                Text = "VI",
                Size = new Size(42, 26),
                Location = new Point(47, 3),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            langEnButton.Click += (sender, args) => SetLanguage("EN");
            langViButton.Click += (sender, args) => SetLanguage("VI");

            langSwitchPanel.Controls.Add(langEnButton);
            langSwitchPanel.Controls.Add(langViButton);

            labelConnectionStatus = CreateLabel(string.Empty, 8, FontStyle.Bold, TextSecondary);
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
            header.Controls.Add(langSwitchPanel);
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

            sidebarNavLabel = CreateLabel("NAVIGATION", 7.5F, FontStyle.Bold, TextSecondary);
            sidebarNavLabel.Dock = DockStyle.Top;
            sidebarNavLabel.Height = 30;

            dashboardNavigationButton = CreateNavigationButton("Fan control");
            settingsNavigationButton = CreateNavigationButton("Settings");
            dashboardNavigationButton.Dock = DockStyle.Top;
            settingsNavigationButton.Dock = DockStyle.Top;

            sidebarVersionLabel = CreateLabel("Version 2.1", 8, FontStyle.Regular, TextSecondary);
            sidebarVersionLabel.Dock = DockStyle.Bottom;
            sidebarVersionLabel.Height = 24;
            sidebarVersionLabel.TextAlign = ContentAlignment.MiddleCenter;

            sidebar.Controls.Add(sidebarVersionLabel);
            sidebar.Controls.Add(settingsNavigationButton);
            sidebar.Controls.Add(dashboardNavigationButton);
            sidebar.Controls.Add(sidebarNavLabel);
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
            dashboardTitleLabel = CreateLabel("Fan control", 18, FontStyle.Bold, TextPrimary);
            dashboardTitleLabel.Location = new Point(0, 0);
            dashboardTitleLabel.AutoSize = true;
            dashboardSubtitleLabel = CreateLabel("Monitor and tune cooling performance.", 9, FontStyle.Regular, TextSecondary);
            dashboardSubtitleLabel.Location = new Point(2, 31);
            dashboardSubtitleLabel.AutoSize = true;
            titlePanel.Controls.Add(dashboardTitleLabel);
            titlePanel.Controls.Add(dashboardSubtitleLabel);

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
            statCpuTempTitleLabel = CreateLabel("CPU TEMPERATURE", 8, FontStyle.Bold, TextSecondary);
            labelCpuTemperature = CreateLabel("—", 20, FontStyle.Bold, DarkBlue);
            stats.Controls.Add(CreateStatCard(statCpuTempTitleLabel, labelCpuTemperature, "°C", 0), 0, 0);

            statFanRpmTitleLabel = CreateLabel("FAN SPEED", 8, FontStyle.Bold, TextSecondary);
            labelFanRpm = CreateLabel("—", 20, FontStyle.Bold, DarkBlue);
            stats.Controls.Add(CreateStatCard(statFanRpmTitleLabel, labelFanRpm, "RPM", 8), 1, 0);

            statAppliedSpeedTitleLabel = CreateLabel("APPLIED OUTPUT", 8, FontStyle.Bold, TextSecondary);
            labelAppliedSpeed = CreateLabel("—", 20, FontStyle.Bold, DarkBlue);
            stats.Controls.Add(CreateStatCard(statAppliedSpeedTitleLabel, labelAppliedSpeed, "TARGET", 8), 2, 0);

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

            controlTitleLabel = CreateLabel("Fan control", 11, FontStyle.Bold, TextPrimary);
            controlTitleLabel.Location = new Point(18, 15);
            controlTitleLabel.AutoSize = true;
            controlSubtitleLabel = CreateLabel("Enable manual or curve mode.", 8, FontStyle.Regular, TextSecondary);
            controlSubtitleLabel.Location = new Point(19, 38);
            controlSubtitleLabel.AutoSize = true;
            toggleFanControl = new ToggleSwitch
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - 64, 18)
            };
            card.Resize += (sender, args) => toggleFanControl.Left = card.ClientSize.Width - 64;

            modeLabel = CreateLabel("Fan curve", 9, FontStyle.Bold, TextPrimary);
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

            card.Controls.Add(controlTitleLabel);
            card.Controls.Add(controlSubtitleLabel);
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

            curveTitleLabel = CreateLabel("CPU temperature fan curve", 11, FontStyle.Bold, TextPrimary);
            curveTitleLabel.Location = new Point(18, 14);
            curveTitleLabel.AutoSize = true;
            curveSubtitleLabel = CreateLabel("Drag blue points; orange line = current CPU temp.", 8, FontStyle.Regular, TextSecondary);
            curveSubtitleLabel.Location = new Point(19, 37);
            curveSubtitleLabel.AutoSize = true;
            curveResetButton = CreateButton("Reset", false);
            curveResetButton.Size = new Size(108, 32);
            curveResetButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            curveResetButton.Location = new Point(card.Width - 126, 15);

            fanCurveControl = new FanCurveControl
            {
                Location = new Point(16, 66),
                Size = new Size(500, 220),
                Curve = FanCurve.Parse(Properties.Settings.Default.fanCurve)
            };
            labelCurveStatus = CreateLabel(string.Empty, 8, FontStyle.Regular, TextSecondary);
            labelCurveStatus.AutoSize = false;
            labelCurveStatus.Location = new Point(19, card.Height - 34);
            labelCurveStatus.Size = new Size(card.Width - 38, 20);
            card.Resize += (sender, args) =>
            {
                curveResetButton.Left = card.ClientSize.Width - 126;
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

            curveResetButton.Click += ResetFanCurve_Click;
            card.Controls.Add(curveTitleLabel);
            card.Controls.Add(curveSubtitleLabel);
            card.Controls.Add(curveResetButton);
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

            settingsTitleLabel = CreateLabel("Settings", 18, FontStyle.Bold, TextPrimary);
            settingsTitleLabel.Dock = DockStyle.Top;
            settingsTitleLabel.Height = 34;
            settingsSubtitleLabel = CreateLabel("Startup, polling, safety and preferences.", 9, FontStyle.Regular, TextSecondary);
            settingsSubtitleLabel.Dock = DockStyle.Top;
            settingsSubtitleLabel.Height = 38;

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

            settingStartWithWinTitleLabel = CreateLabel("Start with Windows", 10, FontStyle.Bold, TextPrimary);
            settingStartWithWinDescLabel = CreateLabel("Launch after sign-in using Scheduled Task.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingStartWithWinTitleLabel,
                settingStartWithWinDescLabel,
                toggleStartWithWindows));

            settingPollingTitleLabel = CreateLabel("Polling interval", 10, FontStyle.Bold, TextPrimary);
            settingPollingDescLabel = CreateLabel("Refresh temperature and RPM. Default: 2s.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingPollingTitleLabel,
                settingPollingDescLabel,
                pollingComboBox));

            settingAutoRefreshTitleLabel = CreateLabel("Auto-refresh statistics", 10, FontStyle.Bold, TextPrimary);
            settingAutoRefreshDescLabel = CreateLabel("Continuously update dashboard data.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingAutoRefreshTitleLabel,
                settingAutoRefreshDescLabel,
                toggleAutoRefresh));

            settingMinToTrayTitleLabel = CreateLabel("Minimize to system tray", 10, FontStyle.Bold, TextPrimary);
            settingMinToTrayDescLabel = CreateLabel("Close keeps the app running in tray.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingMinToTrayTitleLabel,
                settingMinToTrayDescLabel,
                toggleMinimizeToTray));

            settingTurnOffExitTitleLabel = CreateLabel("Reset fan control on exit", 10, FontStyle.Bold, TextPrimary);
            settingTurnOffExitDescLabel = CreateLabel("Return fans to ASUS firmware.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingTurnOffExitTitleLabel,
                settingTurnOffExitDescLabel,
                toggleTurnOffOnExit));

            settingSafeLimitsTitleLabel = CreateLabel("Safe output limits", 10, FontStyle.Bold, TextPrimary);
            settingSafeLimitsDescLabel = CreateLabel("Limit commands to 40–99%.", 8, FontStyle.Regular, TextSecondary);
            settingsList.Controls.Add(CreateSettingRow(
                settingSafeLimitsTitleLabel,
                settingSafeLimitsDescLabel,
                toggleSafeSettings));

            var actions = new ModernCard { Height = 82, Width = 700 };
            settingsRefreshButton = CreateButton("Refresh", true);
            settingsRefreshButton.Location = new Point(18, 22);
            settingsRefreshButton.Size = new Size(118, 38);
            settingsRefreshButton.Click += (sender, args) => RefreshAll(true);

            settingsProjectButton = CreateButton("Project", false);
            settingsProjectButton.Location = new Point(146, 22);
            settingsProjectButton.Size = new Size(118, 38);
            settingsProjectButton.Click += (sender, args) =>
                Process.Start("https://github.com/Karmel0x/AsusFanControl");

            settingsExitButton = CreateButton("Exit", false);
            settingsExitButton.Size = new Size(132, 38);
            settingsExitButton.Location = new Point(actions.Width - 150, 22);
            settingsExitButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            settingsExitButton.Click += (sender, args) => ExitApplication();

            actions.Controls.Add(settingsRefreshButton);
            actions.Controls.Add(settingsProjectButton);
            actions.Controls.Add(settingsExitButton);
            settingsList.Controls.Add(actions);

            labelSettingsStatus = CreateLabel(string.Empty, 8, FontStyle.Regular, TextSecondary);
            labelSettingsStatus.Dock = DockStyle.Bottom;
            labelSettingsStatus.Height = 26;

            page.Controls.Add(settingsList);
            page.Controls.Add(labelSettingsStatus);
            page.Controls.Add(settingsSubtitleLabel);
            page.Controls.Add(settingsTitleLabel);
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

            currentLanguage = string.IsNullOrEmpty(settings.language) ? "VI" : settings.language;
            ApplyLanguage();

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

        private void SetLanguage(string lang)
        {
            if (currentLanguage == lang)
                return;

            currentLanguage = lang;
            Properties.Settings.Default.language = lang;
            SaveSettings();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool isEn = currentLanguage == "EN";

            // Header language buttons
            langEnButton.BackColor = isEn ? Blue : Color.Transparent;
            langEnButton.ForeColor = isEn ? Color.White : TextSecondary;
            langViButton.BackColor = !isEn ? Blue : Color.Transparent;
            langViButton.ForeColor = !isEn ? Color.White : TextSecondary;

            // Sidebar
            if (sidebarNavLabel != null)
                sidebarNavLabel.Text = isEn ? "NAVIGATION" : "ĐIỀU HƯỚNG";
            if (dashboardNavigationButton != null)
                dashboardNavigationButton.Text = isEn ? "Fan control" : "Điều khiển quạt";
            if (settingsNavigationButton != null)
                settingsNavigationButton.Text = isEn ? "Settings" : "Cài đặt";
            if (sidebarVersionLabel != null)
                sidebarVersionLabel.Text = isEn ? "Version 2.1" : "Phiên bản 2.1";

            // Dashboard
            if (dashboardTitleLabel != null)
                dashboardTitleLabel.Text = isEn ? "Fan control" : "Điều khiển quạt";
            if (dashboardSubtitleLabel != null)
                dashboardSubtitleLabel.Text = isEn ? "Monitor and tune cooling performance." : "Theo dõi và tinh chỉnh hệ thống làm mát.";

            // Stat Cards
            if (statCpuTempTitleLabel != null)
                statCpuTempTitleLabel.Text = isEn ? "CPU TEMPERATURE" : "NHIỆT ĐỘ CPU";
            if (statFanRpmTitleLabel != null)
                statFanRpmTitleLabel.Text = isEn ? "FAN SPEED" : "TỐC ĐỘ QUẠT";
            if (statAppliedSpeedTitleLabel != null)
                statAppliedSpeedTitleLabel.Text = isEn ? "APPLIED OUTPUT" : "MỨC ÁP DỤNG";

            // Control Card
            if (controlTitleLabel != null)
                controlTitleLabel.Text = isEn ? "Fan control" : "Điều khiển quạt";
            if (controlSubtitleLabel != null)
                controlSubtitleLabel.Text = isEn ? "Enable manual or curve mode." : "Bật chế độ thủ công hoặc biểu đồ.";
            if (modeLabel != null)
                modeLabel.Text = isEn ? "Fan curve" : "Biểu đồ quạt";

            // Curve Card
            if (curveTitleLabel != null)
                curveTitleLabel.Text = isEn ? "CPU temperature fan curve" : "Biểu đồ nhiệt độ CPU";
            if (curveSubtitleLabel != null)
                curveSubtitleLabel.Text = isEn ? "Drag blue points; orange line = current CPU temp." : "Kéo điểm xanh; vạch cam = nhiệt độ hiện tại.";
            if (curveResetButton != null)
                curveResetButton.Text = isEn ? "Reset" : "Đặt lại";

            // Settings Page
            if (settingsTitleLabel != null)
                settingsTitleLabel.Text = isEn ? "Settings" : "Cài đặt";
            if (settingsSubtitleLabel != null)
                settingsSubtitleLabel.Text = isEn ? "Startup, polling, safety and preferences." : "Khởi động, làm mới, an toàn và tùy chọn.";

            if (settingStartWithWinTitleLabel != null)
                settingStartWithWinTitleLabel.Text = isEn ? "Start with Windows" : "Khởi động cùng Windows";
            if (settingStartWithWinDescLabel != null)
                settingStartWithWinDescLabel.Text = isEn ? "Launch after sign-in using Scheduled Task." : "Tự chạy sau khi đăng nhập bằng Scheduled Task.";

            if (settingPollingTitleLabel != null)
                settingPollingTitleLabel.Text = isEn ? "Polling interval" : "Chu kỳ cập nhật";
            if (settingPollingDescLabel != null)
                settingPollingDescLabel.Text = isEn ? "Refresh temperature and RPM. Default: 2s." : "Cập nhật nhiệt độ và RPM. Mặc định: 2 giây.";

            if (settingAutoRefreshTitleLabel != null)
                settingAutoRefreshTitleLabel.Text = isEn ? "Auto-refresh statistics" : "Tự làm mới thống kê";
            if (settingAutoRefreshDescLabel != null)
                settingAutoRefreshDescLabel.Text = isEn ? "Continuously update dashboard data." : "Liên tục cập nhật dữ liệu trên Dashboard.";

            if (settingMinToTrayTitleLabel != null)
                settingMinToTrayTitleLabel.Text = isEn ? "Minimize to system tray" : "Thu nhỏ xuống khay";
            if (settingMinToTrayDescLabel != null)
                settingMinToTrayDescLabel.Text = isEn ? "Close keeps the app running in tray." : "Bấm X vẫn giữ ứng dụng chạy dưới system tray.";

            if (settingTurnOffExitTitleLabel != null)
                settingTurnOffExitTitleLabel.Text = isEn ? "Reset fan control on exit" : "Reset quạt khi thoát";
            if (settingTurnOffExitDescLabel != null)
                settingTurnOffExitDescLabel.Text = isEn ? "Return fans to ASUS firmware." : "Trả quyền điều khiển quạt về firmware ASUS.";

            if (settingSafeLimitsTitleLabel != null)
                settingSafeLimitsTitleLabel.Text = isEn ? "Safe output limits" : "Giới hạn an toàn";
            if (settingSafeLimitsDescLabel != null)
                settingSafeLimitsDescLabel.Text = isEn ? "Limit commands to 40–99%." : "Giới hạn lệnh điều khiển trong khoảng 40–99%.";

            if (settingsRefreshButton != null)
                settingsRefreshButton.Text = isEn ? "Refresh" : "Làm mới";
            if (settingsProjectButton != null)
                settingsProjectButton.Text = isEn ? "Project" : "Dự án";
            if (settingsExitButton != null)
                settingsExitButton.Text = isEn ? "Exit" : "Thoát";

            UpdatePollingComboBoxItems();
            UpdateConnectionStatusDisplay();
            UpdateCurveStatusDisplay();
            UpdateSettingsStatusDisplay();
            UpdateTrayContextMenu();
        }

        private void UpdatePollingComboBoxItems()
        {
            if (pollingComboBox == null) return;
            bool isEn = currentLanguage == "EN";
            int selectedMs = (pollingComboBox.SelectedItem as PollingOption)?.Milliseconds ?? Properties.Settings.Default.pollingIntervalMs;

            pollingComboBox.BeginUpdate();
            pollingComboBox.Items.Clear();
            pollingComboBox.Items.AddRange(new object[]
            {
                new PollingOption(isEn ? "1 second" : "1 giây", 1000),
                new PollingOption(isEn ? "2 seconds" : "2 giây", 2000),
                new PollingOption(isEn ? "3 seconds" : "3 giây", 3000),
                new PollingOption(isEn ? "5 seconds" : "5 giây", 5000),
                new PollingOption(isEn ? "10 seconds" : "10 giây", 10000)
            });

            pollingComboBox.SelectedItem = pollingComboBox.Items
                .Cast<PollingOption>()
                .OrderBy(option => Math.Abs(option.Milliseconds - selectedMs))
                .First();
            pollingComboBox.EndUpdate();
        }

        private void SetConnectionStatusState(ConnectionState state)
        {
            currentConnectionState = state;
            UpdateConnectionStatusDisplay();
        }

        private void UpdateConnectionStatusDisplay()
        {
            if (labelConnectionStatus == null) return;
            bool isEn = currentLanguage == "EN";
            bool isError = currentConnectionState == ConnectionState.HardwareUnavailable;
            string text = string.Empty;

            switch (currentConnectionState)
            {
                case ConnectionState.Initializing:
                    text = isEn ? "Initializing" : "Đang khởi tạo";
                    break;
                case ConnectionState.FirmwareAuto:
                    text = isEn ? "Firmware / Auto" : "Firmware / Tự động";
                    break;
                case ConnectionState.Manual:
                    text = isEn ? "Manual" : "Thủ công";
                    break;
                case ConnectionState.CurveActive:
                    text = isEn ? "Curve Active" : "Đang chạy biểu đồ";
                    break;
                case ConnectionState.HardwareUnavailable:
                    text = isEn ? "Hardware Unavailable" : "Không khả dụng";
                    break;
            }

            labelConnectionStatus.Text = text;
            labelConnectionStatus.ForeColor = isError ? Color.FromArgb(180, 45, 55) : DarkBlue;
            labelConnectionStatus.BackColor = isError ? Color.FromArgb(255, 235, 238) : Color.FromArgb(231, 242, 255);
        }

        private void SetCurveStatusState(CurveStatusState state, int temp = 0, int target = 0, string errorMsg = "")
        {
            currentCurveState = state;
            currentCurveTemp = temp;
            currentCurveTargetSpeed = target;
            currentCurveError = errorMsg;
            UpdateCurveStatusDisplay();
        }

        private void UpdateCurveStatusDisplay()
        {
            if (labelCurveStatus == null) return;
            bool isEn = currentLanguage == "EN";

            switch (currentCurveState)
            {
                case CurveStatusState.FirmwareActive:
                    labelCurveStatus.Text = isEn ? "Firmware fan control active." : "Firmware đang điều khiển quạt.";
                    break;
                case CurveStatusState.Waiting:
                    labelCurveStatus.Text = isEn ? "Waiting for temperature data." : "Đang chờ dữ liệu nhiệt độ.";
                    break;
                case CurveStatusState.RestoredDefault:
                    labelCurveStatus.Text = isEn ? "Default curve restored." : "Đã khôi phục biểu đồ mặc định.";
                    break;
                case CurveStatusState.TargetInfo:
                    labelCurveStatus.Text = isEn
                        ? string.Format("CPU {0}°C  →  target {1}%", currentCurveTemp, currentCurveTargetSpeed)
                        : string.Format("CPU {0}°C  →  mục tiêu {1}%", currentCurveTemp, currentCurveTargetSpeed);
                    break;
                case CurveStatusState.TempUnavailable:
                    labelCurveStatus.Text = isEn
                        ? "Temperature unavailable: " + currentCurveError
                        : "Không đọc được nhiệt độ: " + currentCurveError;
                    break;
            }
        }

        private void SetSettingsStatusState(SettingsStatusState state, string param = "")
        {
            currentSettingsStatus = state;
            currentSettingsParam = param;
            UpdateSettingsStatusDisplay();
        }

        private void UpdateSettingsStatusDisplay()
        {
            if (labelSettingsStatus == null) return;
            bool isEn = currentLanguage == "EN";

            switch (currentSettingsStatus)
            {
                case SettingsStatusState.None:
                    labelSettingsStatus.Text = string.Empty;
                    break;
                case SettingsStatusState.TaskInstalled:
                    labelSettingsStatus.Text = isEn ? "Startup task installed." : "Đã bật khởi động cùng Windows.";
                    break;
                case SettingsStatusState.TaskRemoved:
                    labelSettingsStatus.Text = isEn ? "Startup task removed." : "Đã tắt khởi động cùng Windows.";
                    break;
                case SettingsStatusState.TaskFailed:
                    labelSettingsStatus.Text = isEn
                        ? "Startup setting failed: " + currentSettingsParam
                        : "Cài đặt khởi động thất bại: " + currentSettingsParam;
                    break;
                case SettingsStatusState.PollingUpdated:
                    labelSettingsStatus.Text = isEn
                        ? "Polling updated: " + currentSettingsParam + "."
                        : "Đã đổi chu kỳ: " + currentSettingsParam + ".";
                    break;
            }
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
                SetConnectionStatusState(ConnectionState.FirmwareAuto);
                SetCurveStatusState(CurveStatusState.FirmwareActive);
                RefreshAll(true);
            }
            catch (Exception exception)
            {
                SetConnectionStatusState(ConnectionState.HardwareUnavailable);
                SetCurveStatusState(CurveStatusState.TempUnavailable, 0, 0, exception.Message);
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
                SetConnectionStatusState(ConnectionState.FirmwareAuto);
            }
            else if (toggleFanCurve.Checked)
            {
                RefreshCpuTemperature(true);
                SetConnectionStatusState(ConnectionState.CurveActive);
            }
            else
            {
                ApplyManualSpeed();
                SetConnectionStatusState(ConnectionState.Manual);
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
            SetCurveStatusState(CurveStatusState.RestoredDefault);
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
                SetSettingsStatusState(toggleStartWithWindows.Checked
                    ? SettingsStatusState.TaskInstalled
                    : SettingsStatusState.TaskRemoved);
            }
            catch (Exception exception)
            {
                initializing = true;
                toggleStartWithWindows.Checked = !toggleStartWithWindows.Checked;
                initializing = false;
                SetSettingsStatusState(SettingsStatusState.TaskFailed, exception.Message);
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
            SetSettingsStatusState(SettingsStatusState.PollingUpdated, option.ToString());
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
                SetCurveStatusState(CurveStatusState.TempUnavailable, 0, 0, exception.Message);
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
                SetCurveStatusState(CurveStatusState.TargetInfo, temperature, requestedSpeed);

                if (toggleFanControl.Checked)
                    ApplyFanSpeed(requestedSpeed);
            }
            catch (Exception exception)
            {
                labelCpuTemperature.Text = "—";
                SetCurveStatusState(CurveStatusState.TempUnavailable, 0, 0, exception.Message);
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
            bool isEn = currentLanguage == "EN";
            trayIcon.ShowBalloonTip(
                2500,
                "SimpleFanControl for Asus",
                isEn ? "Still running in system tray." : "Ứng dụng vẫn chạy dưới khay hệ thống.",
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
                Text = "SimpleFanControl for Asus"
            };
            UpdateTrayContextMenu();
            trayIcon.DoubleClick += (sender, args) => RestoreFromTray();
        }

        private void UpdateTrayContextMenu()
        {
            if (trayIcon == null) return;
            bool isEn = currentLanguage == "EN";
            trayIcon.ContextMenu = new ContextMenu(new[]
            {
                new MenuItem(isEn ? "Open" : "Mở", (sender, args) => RestoreFromTray()),
                new MenuItem(isEn ? "Exit" : "Thoát", (sender, args) => ExitApplication())
            });
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

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private ModernCard CreateStatCard(
            Label titleLabel,
            Label valueLabel,
            string unit,
            int leftMargin)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(leftMargin, 0, 0, 0)
            };
            titleLabel.Location = new Point(18, 15);
            titleLabel.AutoSize = true;
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

        private ModernCard CreateSettingRow(
            Label titleLabel,
            Label descriptionLabel,
            Control action)
        {
            var row = new ModernCard { Height = 74, Width = 700 };
            titleLabel.Location = new Point(18, 14);
            titleLabel.AutoSize = true;
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
