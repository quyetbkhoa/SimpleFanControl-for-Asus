using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // The ASUS driver calls used by this application need the SYSTEM
            // context on some models. The release already includes run.bat and
            // PsExec.exe; redirect a normal double-click through that launcher.
            if (!IsRunningAsSystem() && TryStartSystemLauncher())
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static bool IsRunningAsSystem()
        {
            using (var identity = WindowsIdentity.GetCurrent())
                return identity != null && identity.IsSystem;
        }

        private static bool TryStartSystemLauncher()
        {
            var launcherPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run.bat");
            if (!File.Exists(launcherPath))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launcherPath,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Could not start with SYSTEM privileges.\n" +
                    "Không thể khởi động với quyền SYSTEM.\n\n" +
                    exception.Message +
                    "\n\nUse run.bat / Hãy thử chạy run.bat.",
                    "SimpleFanControl for Asus",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return true;
            }
        }
    }
}
