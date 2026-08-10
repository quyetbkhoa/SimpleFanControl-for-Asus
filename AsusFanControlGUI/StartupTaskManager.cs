using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class StartupTaskManager
    {
        private const string TaskName = "SimpleFanControl for Asus";

        public static bool IsEnabled()
        {
            return RunSchtasks("/Query /TN " + Quote(TaskName), false) == 0;
        }

        public static void SetEnabled(bool enabled)
        {
            if (!enabled)
            {
                var deleteExitCode = RunSchtasks(
                    "/Delete /F /TN " + Quote(TaskName), true);
                if (deleteExitCode != 0 && IsEnabled())
                    throw new InvalidOperationException("Windows could not remove the startup task.");
                return;
            }

            var exePath = Application.ExecutablePath;
            var arguments = "/Create /F /TN " + Quote(TaskName) +
                            " /SC ONLOGON /DELAY 0000:10 /RU SYSTEM /RL HIGHEST /TR " +
                            Quote(exePath);

            if (RunSchtasks(arguments, true) != 0)
                throw new InvalidOperationException("Windows could not create the startup task.");
        }

        private static int RunSchtasks(string arguments, bool wait)
        {
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "schtasks.exe"),
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            }))
            {
                if (process == null)
                    return -1;

                if (!wait)
                    process.WaitForExit(3000);
                else
                    process.WaitForExit();

                return process.HasExited ? process.ExitCode : -1;
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
