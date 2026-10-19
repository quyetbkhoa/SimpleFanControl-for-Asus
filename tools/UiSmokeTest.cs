using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using AsusFanControlGUI;

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
        using (var form = new Form1())
        {
            form.TopMost = true;
            form.Show();
            form.Activate();
            Application.DoEvents();
            var language = Field<Control>(form, "languageButton");
            var localizer = form.GetType().GetField("strings", Private).GetValue(form);
            var property = localizer.GetType().GetProperty("Language");
            var vi = Enum.Parse(property.PropertyType, "Vietnamese");
            property.SetValue(localizer, vi, null);
            form.GetType().GetMethod("ApplyLanguage", Private).Invoke(form, null);
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
            Mouse(curve, 0x201, x, y);
            Mouse(curve, 0x200, x + 8, y - 8);
            Mouse(curve, 0x202, x + 8, y - 8);
            int after = (int)point.GetType().GetProperty("FanSpeed").GetValue(point, null);
            if (after <= speed) throw new Exception("Curve dragging did not update speed.");
            var mode = Field<Control>(form, "modeSelector");
            Mouse(mode, 0x201, mode.Width - 20, mode.Height / 2);
            Mouse(mode, 0x202, mode.Width - 20, mode.Height / 2);
            if (Field<Control>(form, "manualSpeedSlider").Enabled)
                throw new Exception("Manual slider must be disabled in curve mode.");
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
    private static void Mouse(Control control, uint message, int x, int y)
    {
        SendMessage(control.Handle, message, message == 0x202 ? IntPtr.Zero : new IntPtr(1),
            new IntPtr((y << 16) | (x & 65535)));
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
