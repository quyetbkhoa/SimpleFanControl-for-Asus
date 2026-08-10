using System;
using System.Windows.Forms;

namespace AsusFanControlGUI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Fully native Windows UAC elevation via app.manifest (requireAdministrator).
            // Eliminates any reliance on external tools like PsExec or batch scripts.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
