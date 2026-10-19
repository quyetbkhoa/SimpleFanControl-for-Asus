using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    public partial class Form1
    {
        private ComboBox pointPicker;
        private NumericUpDown pointTemperature;
        private NumericUpDown pointSpeed;
        private bool updatingPointEditor;
        private Button applyPointButton;
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
                Height = 88,
                BackColor = UiPalette.Surface,
                Padding = new Padding(20, 8, 20, 0)
            };
            var layout = Grid(1, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = Grid(3, 1);
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            var brand = Grid(2, 1);
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            brand.Controls.Add(new IconView { Glyph = UiGlyph.Fan, ForeColor = UiPalette.Primary, BackColor = UiPalette.Surface, Dock = DockStyle.Fill, AccessibleName = "SimpleFanControl" }, 0, 0);
            var name = CreateLabel("SimpleFanControl for Asus", 13, FontStyle.Bold, UiPalette.Text);
            name.Dock = DockStyle.Fill;
            name.TextAlign = ContentAlignment.MiddleLeft;
            name.AutoEllipsis = true;
            brand.Controls.Add(name, 1, 0);
            labelConnectionStatus = CreateLabel("", 9, FontStyle.Regular, UiPalette.Muted);
            labelConnectionStatus.Dock = DockStyle.Fill;
            labelConnectionStatus.Margin = new Padding(8, 10, 16, 10);
            labelConnectionStatus.TextAlign = ContentAlignment.MiddleCenter;
            languageButton = new LanguageButton
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 7, 0, 7)
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
            top.Controls.Add(brand, 0, 0);
            top.Controls.Add(labelConnectionStatus, 1, 0);
            top.Controls.Add(languageButton, 2, 0);
            var navigation = new FlowLayoutPanel
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
                Height = 34,
                Margin = new Padding(0, 2, 8, 0)
            };
            settingsNavigationButton = new FluentButton
            {
                Navigation = true,
                Glyph = UiGlyph.Settings,
                Width = 120,
                Height = 34,
                Margin = new Padding(0, 2, 8, 0)
            };
            translations.Add(dashboardNavigationButton, "navControl");
            translations.Add(settingsNavigationButton, "navSettings");
            navigation.Controls.AddRange(new Control[] { dashboardNavigationButton, settingsNavigationButton });
            layout.Controls.Add(top, 0, 0);
            layout.Controls.Add(navigation, 0, 1);
            header.Controls.Add(layout);
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
            var layout = Grid(1, 4);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var stats = Grid(3, 1);
            for (int i = 0; i < 3; i++)
                stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
            stats.Controls.Add(Stat("cpuTemperature", "°C", UiGlyph.Temperature, out labelCpuTemperature), 0, 0);
            stats.Controls.Add(Stat("fanSpeed", "RPM", UiGlyph.Fan, out labelFanRpm), 1, 0);
            stats.Controls.Add(Stat("appliedOutput", "", UiGlyph.Power, out labelAppliedSpeed), 2, 0);
            labelAppliedSpeed.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            labelFanRpm.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            var work = Grid(2, 1);
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224));
            work.Controls.Add(BuildCurveCard(), 0, 0);
            work.Controls.Add(BuildPointEditor(), 1, 0);
            layout.Controls.Add(Heading("dashboardTitle", "dashboardSubtitle"), 0, 0);
            layout.Controls.Add(stats, 0, 1);
            layout.Controls.Add(BuildControlCard(), 0, 2);
            layout.Controls.Add(work, 0, 3);
            page.Controls.Add(layout);
            return page;
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
                Margin = new Padding(0, 0, 8, 10),
                Padding = new Padding(14, 10, 14, 10)
            };
            var layout = Grid(3, 1);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 224));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            var master = Grid(2, 2);
            master.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            master.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            master.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            master.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LocalLabel("coolingControl", 10, true);
            title.Dock = DockStyle.Fill;
            title.AutoSize = false;
            toggleFanControl = new ToggleSwitch
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AccessibleName = strings["coolingControl"]
            };
            controlNote = LocalLabel("firmwareActive", 8.5f, false, UiPalette.Muted);
            controlNote.AutoSize = false;
            controlNote.Dock = DockStyle.Fill;
            master.Controls.Add(title, 0, 0);
            master.Controls.Add(toggleFanControl, 1, 0);
            master.Controls.Add(controlNote, 0, 1);
            master.SetColumnSpan(controlNote, 2);
            modeSelector = new ModeSelector
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(12, 8, 12, 10),
                AccessibleName = strings["modeLabel"]
            };
            var manual = Grid(2, 2);
            manual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            manual.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            manual.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            modeTitle = LocalLabel("manualTitle", 9, true);
            modeTitle.AutoSize = false;
            modeTitle.Dock = DockStyle.Fill;
            labelManualSpeed = CreateLabel("90%", 12, FontStyle.Bold, UiPalette.PrimaryDark);
            labelManualSpeed.Dock = DockStyle.Fill;
            labelManualSpeed.TextAlign = ContentAlignment.MiddleRight;
            manualSpeedSlider = new ModernSlider
            {
                Dock = DockStyle.Fill,
                AccessibleName = strings["manualTitle"]
            };
            modeDescription = LocalLabel("curveModeDescription", 9, false, UiPalette.Muted);
            modeDescription.AutoSize = false;
            modeDescription.Dock = DockStyle.Fill;
            modeDescription.Visible = false;
            manual.Controls.Add(modeTitle, 0, 0);
            manual.Controls.Add(labelManualSpeed, 1, 0);
            var sliderHost = new Panel
            {
                Dock = DockStyle.Fill
            };
            sliderHost.Controls.AddRange(new Control[] { manualSpeedSlider, modeDescription });
            manual.Controls.Add(sliderHost, 0, 1);
            manual.SetColumnSpan(sliderHost, 2);
            layout.Controls.Add(master, 0, 0);
            layout.Controls.Add(modeSelector, 1, 0);
            layout.Controls.Add(manual, 2, 0);
            card.Controls.Add(layout);
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
            var top = Grid(2, 1);
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            var title = LocalLabel("curveTitle", 10.5f, true);
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            var reset = LocalButton("resetCurve");
            ((FluentButton)reset).Glyph = UiGlyph.Reset;
            reset.Dock = DockStyle.Fill;
            reset.Click += ResetFanCurve_Click;
            top.Controls.Add(title, 0, 0);
            top.Controls.Add(reset, 1, 0);
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

        private Control BuildPointEditor()
        {
            var card = new ModernCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10)
            };
            var layout = Grid(1, 8);
            foreach (int height in new[]
            {
                32,
                36,
                24,
                34,
                24,
                34,
                42
            }

            )
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LocalLabel("editPoint", 10.5f, true);
            title.Dock = DockStyle.Fill;
            title.AutoSize = false;
            pointPicker = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            pointTemperature = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 30,
                Maximum = 100,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 0, 4)
            };
            pointSpeed = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 0, 4)
            };
            var temperature = LocalLabel("temperatureAxis", 9, false, UiPalette.Muted);
            temperature.Dock = DockStyle.Fill;
            var speed = LocalLabel("speedAxis", 9, false, UiPalette.Muted);
            speed.Dock = DockStyle.Fill;
            var apply = LocalButton("applyPoint", true);
            apply.Dock = DockStyle.Fill;
            apply.Margin = new Padding(0, 6, 0, 0);
            applyPointButton = apply;
            apply.Click += (sender, args) =>
            {
                if (pointPicker.SelectedIndex < 0)
                    return;
                fanCurveControl.UpdatePoint(pointPicker.SelectedIndex, (int)pointTemperature.Value, (int)pointSpeed.Value);
            };
            pointPicker.SelectedIndexChanged += (sender, args) =>
            {
                if (updatingPointEditor || pointPicker.SelectedIndex < 0)
                    return;
                fanCurveControl.SelectedPointIndex = pointPicker.SelectedIndex;
                RefreshPointEditor();
            };
            fanCurveControl.SelectedPointChanged += (sender, args) => RefreshPointEditor();
            fanCurveControl.CurveChanged += (sender, args) => RefreshPointEditor();
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(pointPicker, 0, 1);
            layout.Controls.Add(temperature, 0, 2);
            layout.Controls.Add(pointTemperature, 0, 3);
            layout.Controls.Add(speed, 0, 4);
            layout.Controls.Add(pointSpeed, 0, 5);
            layout.Controls.Add(apply, 0, 6);
            card.Controls.Add(layout);
            return card;
        }

        private void RefreshPointEditor()
        {
            if (pointPicker == null)
                return;
            updatingPointEditor = true;
            try
            {
                int index = fanCurveControl.SelectedPointIndex;
                var points = fanCurveControl.Curve.Points;
                pointPicker.Items.Clear();
                for (int i = 0; i < points.Count; i++)
                    pointPicker.Items.Add(strings.Format("pointNumber", i + 1) + "  ·  " + points[i].Temperature + "° / " + points[i].FanSpeed + "%");
                pointPicker.SelectedIndex = index;
                pointTemperature.Minimum = 30;
                pointTemperature.Maximum = 100;
                pointSpeed.Minimum = 0;
                pointSpeed.Maximum = 100;
                pointTemperature.Value = points[index].Temperature;
                pointSpeed.Value = points[index].FanSpeed;
                pointTemperature.Minimum = index == 0 ? 30 : points[index - 1].Temperature + 1;
                pointTemperature.Maximum = index == points.Count - 1 ? 100 : points[index + 1].Temperature - 1;
                pointSpeed.Minimum = index == 0 ? 0 : points[index - 1].FanSpeed;
                pointSpeed.Maximum = index == points.Count - 1 ? 100 : points[index + 1].FanSpeed;
            }
            finally
            {
                updatingPointEditor = false;
            }
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
            labelSettingsStatus = CreateLabel("SimpleFanControl for Asus · 2.3.0", 9, FontStyle.Regular, UiPalette.Muted);
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
            layout.Controls.Add(groups, 0, 1);
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
            row.Size = new Size(400, 94);
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
            fanCurveControl.AccessibleName = strings["curveTitle"];
            fanCurveControl.Invalidate();
            pointTemperature.AccessibleName = strings["temperatureAxis"];
            pointSpeed.AccessibleName = strings["speedAxis"];
            pointPicker.AccessibleName = strings["editPoint"];
            toggleFanControl.AccessibleName = strings["coolingControl"];
            modeSelector.AccessibleName = strings["modeLabel"];
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
            labelCurveStatus.Text = strings[toggleFanControl.Checked ? (modeSelector.CurveSelected ? "curveActive" : "manualActive") : "firmwareActive"];
            if (fanSpeed <= 0)
                labelAppliedSpeed.Text = strings["auto"];
            if (trayIcon != null)
            {
                trayIcon.ContextMenu.MenuItems[0].Text = strings["open"];
                trayIcon.ContextMenu.MenuItems[1].Text = strings["exit"];
            }

            RefreshPointEditor();
            UpdateControlMode();
        }
    }
}
