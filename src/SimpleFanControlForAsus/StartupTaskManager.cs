using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AsusFanControlGUI
{
    internal static class StartupTaskManager
    {
        private const string TaskName = "SimpleFanControl for Asus";
        private const int TaskNotFound = unchecked((int)0x80070002);

        private static dynamic Connect()
        {
            var type = Type.GetTypeFromProgID("Schedule.Service", true);
            dynamic scheduler = Activator.CreateInstance(type);
            scheduler.Connect();
            return scheduler;
        }

        public static bool IsEnabled()
        {
            dynamic scheduler = Connect();
            try
            {
                dynamic task = scheduler.GetFolder("\\").GetTask(TaskName);
                return task.Enabled;
            }
            catch (COMException exception) when (exception.HResult == TaskNotFound)
            {
                return false;
            }
        }

        public static bool IsCurrentConfiguration()
        {
            dynamic scheduler = Connect();
            try
            {
                dynamic task = scheduler.GetFolder("\\").GetTask(TaskName);
                dynamic definition = task.Definition;
                if (definition.Actions.Count != 1)
                    return false;
                dynamic action = definition.Actions.Item(1);
                return string.Equals((string)action.Path, ExecutablePath,
                           StringComparison.OrdinalIgnoreCase) &&
                       (string)action.Arguments == "--launch-interactive --start-tray" &&
                       !definition.Settings.DisallowStartIfOnBatteries &&
                       !definition.Settings.StopIfGoingOnBatteries;
            }
            catch (COMException exception) when (exception.HResult == TaskNotFound)
            {
                return false;
            }
        }

        private static string ExecutablePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleFanControlForAsus.exe"); }
        }

        private static object CreateDefinition(dynamic scheduler, string executable)
        {
            dynamic definition = scheduler.NewTask(0);
            definition.RegistrationInfo.Description = "Start SimpleFanControl for Asus in the notification area after Windows sign-in.";
            definition.Principal.UserId = "SYSTEM";
            definition.Principal.LogonType = 5; // Service account; no password is stored.
            definition.Principal.RunLevel = 1;
            definition.Settings.Enabled = true;
            definition.Settings.AllowDemandStart = true;
            definition.Settings.StartWhenAvailable = true;
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.ExecutionTimeLimit = "PT1M";
            definition.Settings.MultipleInstances = 2; // Ignore another overlapping launch.
            dynamic trigger = definition.Triggers.Create(9); // At user sign-in.
            trigger.Enabled = true;
            trigger.Delay = "PT15S";
            dynamic action = definition.Actions.Create(0);
            action.Path = executable;
            action.Arguments = "--launch-interactive --start-tray";
            action.WorkingDirectory = Path.GetDirectoryName(executable);
            return definition;
        }

        // Used by the smoke test to validate the task without installing or running it.
        internal static string GetDefinitionXml(string executable)
        {
            dynamic scheduler = Connect();
            dynamic definition = CreateDefinition(scheduler, executable);
            dynamic action = definition.Actions.Item(1);
            if ((string)action.Path != executable)
                throw new InvalidOperationException("Windows did not retain the executable path.");
            return definition.XmlText;
        }

        public static void SetEnabled(bool enabled)
        {
            dynamic scheduler = Connect();
            dynamic folder = scheduler.GetFolder("\\");
            if (!enabled)
            {
                try { folder.DeleteTask(TaskName, 0); }
                catch (COMException exception) when (exception.HResult == TaskNotFound) { }
                return;
            }
            if (!File.Exists(ExecutablePath))
                throw new FileNotFoundException("The application executable is missing.", ExecutablePath);
            dynamic definition = CreateDefinition(scheduler, ExecutablePath);
            // The hardware UI already runs as SYSTEM, which can register this task.
            folder.RegisterTaskDefinition(TaskName, definition, 6, null, null, 5, null);
        }
    }
}
