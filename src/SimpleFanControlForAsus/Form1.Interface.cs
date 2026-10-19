using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    public partial class Form1
    {
        private Panel modeContent;
        private Control curvePage, manualPage, offPage;
        private Label controlError;
        private ToggleSwitch smoothingToggle;
        private NumericUpDown rampUpInput, rampDownInput;
        private static TableLayoutPanel Grid(int columns, int rows)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = rows,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = Color.Transparent
            };
            if (rows == 1)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return grid;
        }

        private Label LocalLabel(string key, float size = 9.5f, bool bold = false, Color? color = null)
        {
            var label = CreateLabel(strings[key], size, bold ? FontStyle.Bold : FontStyle.Regular, color ?? UiPalette.Text);
            label.AutoSize = true;
            translations.Add(label, key);
            return label;
        }

        private Button LocalButton(string key, bool primary = false)
        {
            var button = new FluentButton
            {
                Text = strings[key],
                Primary = primary
            };
            translations.Add(button, key);
            return button;
        }

        private void BuildInterface()
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiPalette.Page
            };
            dashboardPage = BuildDashboardPage();
            settingsPage = BuildSettingsPage();
            host.Controls.AddRange(new Control[] { settingsPage, dashboardPage });
            var header = BuildHeader();
            var scale = CurrentAutoScaleDimensions.Width / 96f;
            if (Math.Abs(scale - 1) > .01f)
            {
                host.Scale(new SizeF(scale, scale));
                header.Scale(new SizeF(scale, scale));
            }

            Controls.Add(host);
            Controls.Add(header);
            ShowPage(dashboardPage);
        }

        private Control BuildHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = UiPalette.Surface,
                Padding = new Padding(20, 6, 20, 6)
            };
            var row = Grid(2, 1);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Margin = Padding.Empty
            };
            dashboardNavigationButton = new FluentButton
            {
                Navigation = true,
                Glyph = UiGlyph.Dashboard,
                Width = 160,
                Height = 36,
                Margin = new Padding(0, 0, 8, 0)
            };
            settingsNavigationButton = new FluentButton
            {
                Navigation = true,
                Glyph = UiGlyph.Settings,
                Width = 120,
                Height = 36,
                Margin = Padding.Empty
            };
            translations.Add(dashboardNavigationButton, "navControl");
            translations.Add(settingsNavigationButton, "navSettings");
            nav.Controls.AddRange(new Control[] { dashboardNavigationButton, settingsNavigationButton });
            languageButton = new LanguageButton
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            languageButton.Click += (sender, args) =>
            {
                strings.Language = strings.Language == UiLanguage.Vietnamese ? UiLanguage.English : UiLanguage.Vietnamese;
                if (!preview)
                {
                    Properties.Settings.Default.language = strings.Language == UiLanguage.English ? "en" : "vi";
                    SaveSettings();
                }

                ApplyLanguage();
            };
            row.Controls.Add(nav, 0, 0);
            row.Controls.Add(languageButton, 1, 0);
            header.Controls.Add(row);
            return header;
        }

        private Control Heading(string titleKey, string subtitleKey)
        {
            var heading = Grid(1, 2);
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            heading.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            var title = LocalLabel(titleKey, 18, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            var subtitle = LocalLabel(subtitleKey, 9.5f, false, UiPalette.Muted);
            subtitle.AutoSize = false;
            subtitle.Dock = DockStyle.Fill;
            heading.Controls.Add(title, 0, 0);
            heading.Controls.Add(subtitle, 0, 1);
            return heading;
        }

        private Panel BuildDashboardPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 16),
                BackColor = UiPalette.Page
            };
            var layout = Grid(1, 3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var stats = Grid(3, 1);
            for (int i = 0; i < 3; i++)
                stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            stats.Controls.Add(Stat("cpuTemperature", "°C", UiGlyph.Temperature, out labelCpuTemperature), 0, 0);
            stats.Controls.Add(Stat("fanSpeed", "RPM", UiGlyph.Fan, out labelFanRpm), 1, 0);
            stats.Controls.Add(Stat("appliedOutput", "", UiGlyph.Power, out labelAppliedSpeed), 2, 0);
            labelAppliedSpeed.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            labelFanRpm.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            modeContent = new Panel
            {
                Dock = DockStyle.Fill
            };
            curvePage = BuildCurveCard();
            manualPage = BuildManualPage();
            offPage = BuildOffPage();
            modeContent.Controls.AddRange(new[] { curvePage, manualPage, offPage });
            layout.Controls.Add(stats, 0, 0);
            layout.Controls.Add(BuildControlCard(), 0, 1);
            layout.Controls.Add(modeContent, 0, 2);
            controlError = CreateLabel("", 9, FontStyle.Regular, UiPalette.Danger);
            controlError.Dock = DockStyle.Bottom;
            controlError.Height = 32;
            controlError.Visible = false;
            page.Controls.Add(layout);
            page.Controls.Add(controlError);
            return page;
        }

        private Control BuildOffPage()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            var title = LocalLabel("offTitle", 16, true);
            title.Dock = DockStyle.Top;
            title.AutoSize = false;
            title.Height = 42;
            var hint = LocalLabel("offDescription", 10, false, UiPalette.Muted);
            hint.Dock = DockStyle.Top;
            hint.AutoSize = false;
            hint.Height = 72;
            card.Controls.Add(hint);
            card.Controls.Add(title);
            return card;
        }

        private Control BuildManualPage()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };
            var layout = Grid(1, 4);
            foreach (int height in new[]
            {
                40,
                56,
                64
            }

            )
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LocalLabel("manualTitle", 14, true);
            title.Dock = DockStyle.Fill;
            labelManualSpeed = CreateLabel("90%", 28, FontStyle.Bold, UiPalette.PrimaryDark);
            labelManualSpeed.Dock = DockStyle.Fill;
            manualSpeedSlider = new ModernSlider
            {
                Dock = DockStyle.Fill,
                AccessibleName = strings["manualTitle"]
            };
            var hint = LocalLabel("manualDescription", 10, false, UiPalette.Muted);
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(labelManualSpeed, 0, 1);
            layout.Controls.Add(manualSpeedSlider, 0, 2);
            layout.Controls.Add(hint, 0, 3);
            card.Controls.Add(layout);
            return card;
        }

        private ModernCard Stat(string key, string unit, UiGlyph glyph, out Label value)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 8, 10),
                Padding = new Padding(14, 8, 14, 8)
            };
            var row = Grid(2, 1);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            var body = Grid(1, 2);
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var label = LocalLabel(key, 9, false, UiPalette.Muted);
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            var metric = Grid(2, 1);
            metric.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            metric.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, unit.Length == 0 ? 0 : 40));
            value = CreateLabel("—", 24, FontStyle.Bold, UiPalette.Text);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.MiddleLeft;
            var units = CreateLabel(unit, 9, FontStyle.Regular, UiPalette.Muted);
            units.Dock = DockStyle.Fill;
            units.TextAlign = ContentAlignment.MiddleLeft;
            metric.Controls.Add(value, 0, 0);
            metric.Controls.Add(units, 1, 0);
            body.Controls.Add(label, 0, 0);
            body.Controls.Add(metric, 0, 1);
            row.Controls.Add(body, 0, 0);
            row.Controls.Add(new IconView { Dock = DockStyle.Fill, Glyph = glyph, BackColor = Color.White, ForeColor = UiPalette.Primary, AccessibleName = strings[key] }, 1, 0);
            card.Controls.Add(row);
            return card;
        }

        private Control BuildControlCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 8, 14, 8),
                Margin = new Padding(0, 0, 8, 12)
            };
            var row = Grid(2, 1);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450));
            var title = LocalLabel("coolingControl", 11, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            modeSelector = new ModeSelector
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                AccessibleName = strings["modeLabel"]
            };
            row.Controls.Add(title, 0, 0);
            row.Controls.Add(modeSelector, 1, 0);
            card.Controls.Add(row);
            return card;
        }

        private Control BuildCurveCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(0, 0, 12, 0)
            };
            var layout = Grid(1, 4);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            var top = Grid(3, 1);
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            var title = LocalLabel("curveTitle", 10.5f, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            var reset = LocalButton("resetCurve");
            reset.Dock = DockStyle.Fill;
            reset.Click += ResetFanCurve_Click;
            top.Controls.Add(title, 0, 0);
            var add = LocalButton("addPoint");
            add.Dock = DockStyle.Fill;
            add.Margin = new Padding(0, 0, 8, 0);
            add.Click += (sender, args) =>
            {
                var menu = new ContextMenu(new[] { new MenuItem(strings["addStart"], (a, b) => fanCurveControl.AddPoint(true)), new MenuItem(strings["addEnd"], (a, b) => fanCurveControl.AddPoint(false)) });
                menu.MenuItems[0].Enabled = fanCurveControl.Curve.Points[0].Temperature > 0;
                menu.MenuItems[1].Enabled = fanCurveControl.Curve.Points[fanCurveControl.Curve.Points.Count - 1].Temperature < 110;
                menu.Collapse += (a, b) => menu.Dispose();
                menu.Show(add, new Point(0, add.Height));
            };
            top.Controls.Add(add, 1, 0);
            top.Controls.Add(reset, 2, 0);
            var hint = LocalLabel("curveSubtitle", 8.5f, false, UiPalette.Muted);
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            fanCurveControl = new FanCurveControl
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TabIndex = 0
            };
            labelCurveStatus = CreateLabel("", 9, FontStyle.Regular, UiPalette.Muted);
            labelCurveStatus.Dock = DockStyle.Fill;
            labelCurveStatus.TextAlign = ContentAlignment.BottomLeft;
            layout.Controls.Add(top, 0, 0);
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
                Padding = new Padding(20, 12, 20, 16),
                BackColor = UiPalette.Page,
                Visible = false
            };
            var layout = Grid(1, 3);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            var groups = Grid(2, 1);
            groups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            groups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            toggleStartWithWindows = new ToggleSwitch();
            toggleAutoRefresh = new ToggleSwitch();
            toggleMinimizeToTray = new ToggleSwitch();
            toggleTurnOffOnExit = new ToggleSwitch();
            toggleSafeSettings = new ToggleSwitch();
            pollingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 112,
                Font = new Font("Segoe UI", 9.5f)
            };
            var general = SettingsGroup("general", out var generalRows);
            var behavior = SettingsGroup("behavior", out var behaviorRows);
            general.Margin = new Padding(0, 0, 12, 12);
            behavior.Margin = new Padding(0, 0, 0, 12);
            AddSetting(generalRows, "startWindows", "startWindowsDescription", toggleStartWithWindows);
            AddSetting(generalRows, "polling", "pollingDescription", pollingComboBox);
            AddSetting(generalRows, "autoRefresh", "autoRefreshDescription", toggleAutoRefresh);
            AddSetting(behaviorRows, "minimizeTray", "minimizeTrayDescription", toggleMinimizeToTray);
            AddSetting(behaviorRows, "resetOnExit", "resetOnExitDescription", toggleTurnOffOnExit);
            AddSetting(behaviorRows, "safeLimits", "safeLimitsDescription", toggleSafeSettings);
            groups.Controls.Add(general, 0, 0);
            groups.Controls.Add(behavior, 1, 0);
            var actions = Grid(3, 1);
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164));
            labelSettingsStatus = CreateLabel("SimpleFanControl for Asus · 2.4.1", 9, FontStyle.Regular, UiPalette.Muted);
            labelSettingsStatus.Dock = DockStyle.Fill;
            labelSettingsStatus.TextAlign = ContentAlignment.MiddleLeft;
            var refresh = LocalButton("refreshNow");
            refresh.Dock = DockStyle.Fill;
            refresh.Click += (sender, args) => RefreshAll(true);
            var exit = LocalButton("exit");
            exit.Dock = DockStyle.Fill;
            exit.Margin = new Padding(12, 0, 0, 0);
            exit.Click += (sender, args) => ExitApplication();
            actions.Controls.Add(labelSettingsStatus, 0, 0);
            actions.Controls.Add(refresh, 1, 0);
            actions.Controls.Add(exit, 2, 0);
            layout.Controls.Add(Heading("settingsTitle", "settingsSubtitle"), 0, 0);
            var scroll = new Panel
            {
                Name = "settingsScroll",
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            var sections = Grid(1, 2);
            sections.Dock = DockStyle.Top;
            sections.Height = 560;
            sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
            sections.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            sections.Controls.Add(groups, 0, 0);
            sections.Controls.Add(BuildSmoothingCard(), 0, 1);
            scroll.Controls.Add(sections);
            layout.Controls.Add(scroll, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            page.Controls.Add(layout);
            return page;
        }

        private ModernCard SettingsGroup(string key, out FlowLayoutPanel rows)
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14)
            };
            var title = LocalLabel(key, 11, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Top;
            title.Height = 36;
            rows = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Margin = Padding.Empty
            };
            var list = rows;
            rows.SizeChanged += (sender, args) =>
            {
                foreach (Control row in list.Controls)
                    row.Width = Math.Max(200, list.ClientSize.Width - 18);
            };
            card.Controls.Add(rows);
            card.Controls.Add(title);
            return card;
        }

        private void AddSetting(FlowLayoutPanel list, string key, string hint, Control action)
        {
            var row = Grid(2, 1);
            row.Dock = DockStyle.None;
            row.Size = new Size(400, 82);
            row.Margin = new Padding(0, 0, 0, 8);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, action.Width + 8));
            var copy = Grid(1, 2);
            copy.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            copy.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LocalLabel(key, 9.5f, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            var description = LocalLabel(hint, 9, false, UiPalette.Muted);
            description.AutoSize = false;
            description.Dock = DockStyle.Fill;
            copy.Controls.Add(title, 0, 0);
            copy.Controls.Add(description, 0, 1);
            action.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            action.Margin = new Padding(4, 6, 0, 0);
            action.AccessibleName = strings[key];
            row.Controls.Add(copy, 0, 0);
            row.Controls.Add(action, 1, 0);
            list.Controls.Add(row);
        }

        private Control BuildSmoothingCard()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16)
            };
            var layout = Grid(2, 3);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LocalLabel("smoothingTitle", 11, true);
            title.Dock = DockStyle.Fill;
            smoothingToggle = new ToggleSwitch
            {
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            var hint = LocalLabel("smoothingDescription", 9, false, UiPalette.Muted);
            hint.AutoSize = false;
            hint.Dock = DockStyle.Fill;
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(smoothingToggle, 1, 0);
            layout.Controls.Add(hint, 0, 1);
            layout.SetColumnSpan(hint, 2);
            rampUpInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Width = 100
            };
            rampDownInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Width = 100
            };
            for (int j = 0; j < 2; j++)
            {
                var row = Grid(2, 1);
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
                var label = LocalLabel(j == 0 ? "rampUp" : "rampDown", 9.5f, true);
                label.AutoSize = false;
                label.Dock = DockStyle.Fill;
                row.Controls.Add(label, 0, 0);
                row.Controls.Add(j == 0 ? rampUpInput : rampDownInput, 1, 0);
                layout.Controls.Add(row, j, 2);
            }

            EventHandler changed = (sender, args) =>
            {
                if (initializing)
                    return;
                Properties.Settings.Default.smoothingEnabled = smoothingToggle.Checked;
                Properties.Settings.Default.rampUpPerSecond = (int)rampUpInput.Value;
                Properties.Settings.Default.rampDownPerSecond = (int)rampDownInput.Value;
                SaveSettings();
            };
            smoothingToggle.CheckedChanged += changed;
            rampUpInput.ValueChanged += changed;
            rampDownInput.ValueChanged += changed;
            card.Controls.Add(layout);
            return card;
        }

        private void ApplyLanguage()
        {
            foreach (var pair in translations)
                pair.Key.Text = strings[pair.Value];
            languageButton.Language = strings.Language;
            languageButton.AccessibleName = strings.Language == UiLanguage.Vietnamese ? "Switch to English" : "Chuyển sang tiếng Việt";
            modeSelector.OffText = strings["off"];
            modeSelector.ManualText = strings["manual"];
            modeSelector.CurveText = strings["curve"];
            modeSelector.Invalidate();
            fanCurveControl.TemperatureAxisTitle = strings["temperatureAxis"];
            fanCurveControl.SpeedAxisTitle = strings["speedAxis"];
            fanCurveControl.AccessibleName = strings["curveTitle"];
            fanCurveControl.Invalidate();
            modeSelector.AccessibleName = strings["modeLabel"];
            smoothingToggle.AccessibleName = strings["smoothingTitle"];
            rampUpInput.AccessibleName = strings["rampUp"];
            rampDownInput.AccessibleName = strings["rampDown"];
            manualSpeedSlider.AccessibleName = strings["manualTitle"];
            toggleStartWithWindows.AccessibleName = strings["startWindows"];
            toggleAutoRefresh.AccessibleName = strings["autoRefresh"];
            toggleMinimizeToTray.AccessibleName = strings["minimizeTray"];
            toggleTurnOffOnExit.AccessibleName = strings["resetOnExit"];
            toggleSafeSettings.AccessibleName = strings["safeLimits"];
            pollingComboBox.AccessibleName = strings["polling"];
            var previous = initializing;
            initializing = true;
            var milliseconds = (pollingComboBox.SelectedItem as PollingOption)?.Milliseconds ?? Properties.Settings.Default.pollingIntervalMs;
            pollingComboBox.Items.Clear();
            foreach (int interval in new[]
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
            initializing = previous;
            SetConnectionStatus(connectionKey, connectionKey == "unavailable");
            labelCurveStatus.Text = strings[modeSelector.SelectedMode == FanControlMode.Off ? "firmwareActive" : modeSelector.CurveSelected ? "curveActive" : "manualActive"];
            if (fanSpeed <= 0)
                labelAppliedSpeed.Text = strings["auto"];
            if (trayIcon != null)
            {
                trayIcon.ContextMenu.MenuItems[0].Text = strings["open"];
                trayIcon.ContextMenu.MenuItems[1].Text = strings["exit"];
            }

            UpdateControlMode();
        }
    }
}
