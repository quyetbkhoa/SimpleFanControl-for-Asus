using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [STAThread]
        private static void Main(string[] args)
        {
            var preview = Environment.GetEnvironmentVariable("SIMPLEFANCONTROL_UI_PREVIEW") == "1";
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var system = identity != null && identity.IsSystem;
                var interactiveLaunch = Array.IndexOf(args, "--launch-interactive") >= 0;
                if (!preview && (!system || interactiveLaunch))
                {
                    LaunchHardwareInterface(identity, interactiveLaunch, Array.IndexOf(args, "--start-tray") >= 0);
                    return;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(Array.IndexOf(args, "--start-tray") >= 0));
        }

        private static void LaunchHardwareInterface(WindowsIdentity identity, bool interactiveLaunch, bool startInTray)
        {
            var english = Properties.Settings.Default.language == "en";
            try
            {
                var directory = AppDomain.CurrentDomain.BaseDirectory;
                var psExec = Path.Combine(directory, "PsExec.exe");
                if (!File.Exists(psExec))
                    throw new FileNotFoundException(english
                        ? "PsExec.exe is missing. Open the EXE from the complete release folder."
                        : "Thiếu PsExec.exe. Hãy mở EXE trong thư mục phát hành đầy đủ.", psExec);
                var administrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                if (!identity.IsSystem && !administrator)
                {
                    // Windows handles consent; no command interpreter is required.
                    Process.Start(new ProcessStartInfo {
                        FileName = Application.ExecutablePath,
                        WorkingDirectory = directory,
                        Arguments = "--launch-system" + (startInTray ? " --start-tray" : ""),
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    return;
                }
                var session = interactiveLaunch
                    ? WTSGetActiveConsoleSessionId()
                    : (uint)Process.GetCurrentProcess().SessionId;
                if (session == uint.MaxValue)
                    throw new InvalidOperationException(english ? "No active Windows session." : "Không có phiên Windows đang hoạt động.");
                using (var process = Process.Start(new ProcessStartInfo {
                    FileName = psExec,
                    WorkingDirectory = directory,
                    Arguments = "-accepteula -i " + session + " -s -d \"" + Application.ExecutablePath + "\"" +
                        (startInTray ? " --start-tray" : ""),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (process == null)
                        throw new InvalidOperationException("Could not start PsExec.");
                    if (!process.WaitForExit(15000))
                        throw new TimeoutException(english ? "PsExec did not respond in time." : "PsExec không phản hồi kịp thời.");
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException("PsExec: " + process.ExitCode);
                }
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
            {
                // UAC was cancelled. Exit without retrying or opening the hardware UI.
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    (english ? "Could not start fan control.\n\n" : "Không thể khởi động điều khiển quạt.\n\n") + exception.Message,
                    "SimpleFanControl for Asus", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
