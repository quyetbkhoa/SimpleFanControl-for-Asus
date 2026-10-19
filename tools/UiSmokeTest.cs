using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using AsusFanControlGUI;
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2")]

// Runs the real form with hardware access disabled. Usage: UiSmokeTest.exe output-directory
internal static class UiSmokeTest
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW", "1");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        var program = typeof(Form1).Assembly.GetType("AsusFanControlGUI.Program");
        var validateProcess = program.GetMethod("IsExpectedDetachedProcess", BindingFlags.Static | BindingFlags.NonPublic);
        using (var self = Process.GetCurrentProcess())
        {
            if (!(bool)validateProcess.Invoke(null, new object[] { self.Id, (uint)self.SessionId, self.ProcessName }) ||
                (bool)validateProcess.Invoke(null, new object[] { self.Id, (uint)self.SessionId, "UnrelatedApplication" }) ||
                (bool)validateProcess.Invoke(null, new object[] { self.Id, uint.MaxValue, self.ProcessName }) ||
                (bool)validateProcess.Invoke(null, new object[] { -1, (uint)self.SessionId, self.ProcessName }))
                throw new Exception("Detached launcher PID validation failed.");
        }
        Console.WriteLine("PASS: detached PsExec PID is accepted only for the expected process and session.");
        var settingsType = typeof(Form1).Assembly.GetType("AsusFanControlGUI.Properties.Settings");
        var settings = settingsType.GetProperty("Default").GetValue(null, null);
        var savedMode = settingsType.GetProperty("controlMode");
        savedMode.SetValue(settings, 0, null);
        using (var form = new Form1())
        {
            form.TopMost = true;
            form.Show();
            form.Activate();
            Application.DoEvents();
            Console.WriteLine("Layout DPI=" + form.DeviceDpi + "; dimensions=" + form.CurrentAutoScaleDimensions + "; baseline=" + form.AutoScaleDimensions);
            var language = Field<Control>(form, "languageButton");
            var localizer = form.GetType().GetField("strings", Private).GetValue(form);
            var property = localizer.GetType().GetProperty("Language");
            var vi = Enum.Parse(property.PropertyType, "Vietnamese");
            property.SetValue(localizer, vi, null);
            form.GetType().GetMethod("ApplyLanguage", Private).Invoke(form, null);
            Save(form, output, "off-vi.png");
            var selector=Field<Control>(form,"modeSelector");
            SelectMode(selector,2);
            Save(form, output, "dashboard-vi.png");
            Click(language);
            if (property.GetValue(localizer, null).ToString() != "English")
                throw new Exception("A mouse click must change language exactly once. Form=" + form.Bounds +
                    "; flag=" + language.RectangleToScreen(language.ClientRectangle) +
                    "; screen=" + Screen.FromControl(form).Bounds);
            Save(form, output, "dashboard-en.png");
            Field<Button>(form, "settingsNavigationButton").PerformClick();
            Save(form, output, "settings-en.png");
            Click(language);
            Save(form, output, "settings-vi.png");
            var settingsScroll=(ScrollableControl)form.Controls.Find("settingsScroll",true)[0];
            settingsScroll.ScrollControlIntoView(Field<NumericUpDown>(form,"rampDownInput"));
            Save(form,output,"smoothing-vi.png");
            Field<Button>(form, "dashboardNavigationButton").PerformClick();
            var curve = Field<Control>(form, "fanCurveControl");
            var plot = (Rectangle)curve.GetType().GetMethod("GetPlotRectangle", Private).Invoke(curve, null);
            var model = curve.GetType().GetProperty("Curve").GetValue(curve, null);
            var points = (System.Collections.IList)model.GetType().GetProperty("Points").GetValue(model, null);
            var point = points[2];
            int temperature = (int)point.GetType().GetProperty("Temperature").GetValue(point, null);
            int speed = (int)point.GetType().GetProperty("FanSpeed").GetValue(point, null);
            int x = plot.Left + (int)Math.Round((temperature - 30) / 70.0 * plot.Width);
            int y = plot.Bottom - (int)Math.Round(speed / 100.0 * plot.Height);
            Mouse(curve, 0x201, x, y, false);
            Mouse(curve, 0x200, x + 8, y - 8, false);
            Mouse(curve, 0x202, x + 8, y - 8, false);
            Application.DoEvents();
            int after = (int)point.GetType().GetProperty("FanSpeed").GetValue(point, null);
            if (after <= speed) throw new Exception("Curve dragging did not update speed. Before=" + speed + "; after=" + after);
            var update=curve.GetType().GetMethod("UpdatePoint");
            update.Invoke(curve,new object[]{2,70,30});
            for(int i=0;i<2;i++) if((int)points[i].GetType().GetProperty("FanSpeed").GetValue(points[i],null)>30)
                throw new Exception("Lowering a point did not propagate left.");
            update.Invoke(curve,new object[]{2,70,95});
            for(int i=3;i<points.Count;i++) if((int)points[i].GetType().GetProperty("FanSpeed").GetValue(points[i],null)<95)
                throw new Exception("Raising a point did not propagate right.");
            int count=points.Count;
            curve.GetType().GetMethod("AddPoint").Invoke(curve,new object[]{true});
            curve.GetType().GetMethod("AddPoint").Invoke(curve,new object[]{false});
            if(points.Count!=count+2)throw new Exception("Endpoint addition failed.");
            var parsed=model.GetType().GetMethod("Parse").Invoke(null,new object[]{model.ToString()});
            if(parsed.ToString()!=model.ToString())throw new Exception("Added endpoints did not round-trip.");
            form.GetType().GetMethod("ResetFanCurve_Click",Private).Invoke(form,new object[]{form,EventArgs.Empty});
            SelectMode(selector,1);
            if(curve.Visible || !Field<Control>(form,"manualSpeedSlider").Visible)throw new Exception("Manual must show only the slider.");
            Save(form,output,"manual-vi.png");
            SelectMode(selector,0);
            if(curve.Visible || Field<Control>(form,"manualSpeedSlider").Visible ||
                (int)form.GetType().GetField("desiredFanSpeed",Private).GetValue(form)!=0)
                throw new Exception("Off must hide editors and restore firmware target.");
            SelectMode(selector,2);
            Console.WriteLine("PASS: endpoint add/save, neighbor propagation, Off/Manual/Curve visibility.");
            form.Size = form.MinimumSize;
            Save(form, output, "dashboard-minimum.png");
            Console.WriteLine("PASS: language mouse toggle, both pages and languages, curve drag, mode switch, minimum window. DPI=" + form.DeviceDpi);
            form.Hide();
        }
        using (var trayForm = new Form1(true))
        {
            trayForm.Show();
            Application.DoEvents();
            var tray = (NotifyIcon)trayForm.GetType().GetField("trayIcon", Private).GetValue(trayForm);
            if (trayForm.Visible || tray == null || !tray.Visible)
                throw new Exception("Startup must show only the system tray icon.");
            trayForm.GetType().GetMethod("RestoreFromTray", Private).Invoke(trayForm, null);
            Application.DoEvents();
            if (!trayForm.Visible || tray.Visible)
                throw new Exception("Restoring the startup window from tray failed.");
            trayForm.Hide();
        }
        // Recreate the window like a new launch, including tray-only startup.
        foreach (int mode in new[] { 0, 1, 2 })
        {
            using (var first = new Form1())
            {
                first.Show();
                Application.DoEvents();
                SelectMode(Field<Control>(first, "modeSelector"), mode);
                if ((int)savedMode.GetValue(settings, null) != mode)
                    throw new Exception("Selected mode was not persisted: " + mode);
                first.Hide();
            }
            foreach (bool inTray in new[] { false, true })
            {
                using (var reopened = new Form1(inTray))
                {
                    reopened.Show();
                    Application.DoEvents();
                    var selector = Field<Control>(reopened, "modeSelector");
                    int restored = Convert.ToInt32(selector.GetType().GetProperty("SelectedMode").GetValue(selector, null));
                    int target = (int)reopened.GetType().GetField("desiredFanSpeed", Private).GetValue(reopened);
                    int expected = mode == 0 ? 0 : mode == 1
                        ? (int)settingsType.GetProperty("fanSpeed").GetValue(settings, null) : 56;
                    if (restored != mode || target != expected)
                        throw new Exception("Mode/target restoration failed: mode=" + mode + ", tray=" + inTray + ", target=" + target);
                    reopened.Hide();
                }
            }
        }
        savedMode.SetValue(settings, 999, null);
        using (var invalid = new Form1())
        {
            var selector = Field<Control>(invalid, "modeSelector");
            if (Convert.ToInt32(selector.GetType().GetProperty("SelectedMode").GetValue(selector, null)) != 0)
                throw new Exception("Invalid stored mode must fall back to Off.");
        }
        savedMode.SetValue(settings, 0, null);
        Console.WriteLine("PASS: Off/Manual/Curve survive window recreation and tray startup; invalid preferences fall back to Off.");
        var rampType=typeof(Form1).Assembly.GetType("AsusFanControlGUI.FanSpeedRamp");
        var ramp=Activator.CreateInstance(rampType,true);
        rampType.GetMethod("Reset").Invoke(ramp,new object[]{50});
        var next=rampType.GetMethod("Next");
        if((int)next.Invoke(ramp,new object[]{100,1.0,20.0,5.0,true,false})!=70 ||
            (int)next.Invoke(ramp,new object[]{40,1.0,20.0,5.0,true,false})!=65 ||
            (int)next.Invoke(ramp,new object[]{100,0.1,20.0,5.0,true,true})!=100 ||
            (int)next.Invoke(ramp,new object[]{0,0.1,20.0,5.0,true,false})!=0 ||
            (int)next.Invoke(ramp,new object[]{80,0.1,20.0,5.0,true,false})!=80)
            throw new Exception("Ramp rates, urgent rise or Off bypass failed.");
        Console.WriteLine("PASS: +20/-5 percent-points/sec ramp, urgent rise, immediate Off/initial enable.");
        var manager = typeof(Form1).Assembly.GetType("AsusFanControlGUI.StartupTaskManager");
        var xml = (string)manager.GetMethod("GetDefinitionXml", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { @"C:\Program Files\Simple Fan\SimpleFanControlForAsus.exe" });
        var document = new XmlDocument();
        document.LoadXml(xml);
        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("t", "http://schemas.microsoft.com/windows/2004/02/mit/task");
        if (document.SelectSingleNode("//t:Exec/t:Command", namespaces).InnerText !=
            @"C:\Program Files\Simple Fan\SimpleFanControlForAsus.exe" ||
            document.SelectSingleNode("//t:Exec/t:Arguments", namespaces).InnerText != "--launch-interactive --start-tray" ||
            document.SelectSingleNode("//t:DisallowStartIfOnBatteries", namespaces).InnerText != "false" ||
            document.SelectSingleNode("//t:StopIfGoingOnBatteries", namespaces).InnerText != "false")
            throw new Exception("Startup task configuration is invalid.");
        ((XmlDeclaration)document.FirstChild).Encoding = "utf-8";
        document.Save(Path.Combine(output, "startup-task-preview.xml"));
        Console.WriteLine("PASS: tray-only startup and restore; direct EXE task with space-containing path and battery support.");
    }
    private static T Field<T>(Form form, string name) where T : Control
    {
        return (T)form.GetType().GetField(name, Private).GetValue(form);
    }
    private static void Mouse(Control control, uint message, int x, int y, bool pump = true)
    {
        SendMessage(control.Handle, message, message == 0x202 ? IntPtr.Zero : new IntPtr(1),
            new IntPtr((y << 16) | (x & 65535)));
        if (pump) Application.DoEvents();
    }
    private static void SelectMode(Control selector,int mode)
    {
        int x=(mode*2+1)*selector.Width/6;
        Mouse(selector,0x201,x,selector.Height/2,false);
        Mouse(selector,0x202,x,selector.Height/2,false);
        Application.DoEvents();
    }
    private static void Click(Control control)
    {
        Mouse(control, 0x201, 12, 12);
        Mouse(control, 0x202, 12, 12);
    }
    private static void Save(Form form, string directory, string name)
    {
        Application.DoEvents();
        using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(directory, name));
        }
    }
}
