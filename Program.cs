using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MatrixSCR
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr handle);

        [STAThread]
        private static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string command = args.Length == 0 ? "/c" : args[0].Trim().ToLowerInvariant();
            if (command.StartsWith("/p") || command.StartsWith("-p"))
            {
                long value;
                string handleText = command.Length > 2 ? command.Substring(2).TrimStart(':') :
                    (args.Length > 1 ? args[1] : "0");
                if (long.TryParse(handleText, out value) && value != 0)
                {
                    IntPtr parent = new IntPtr(value);
                    if (IsWindow(parent))
                        Application.Run(new SaverForm(parent, SaverSettings.Load()));
                }
                return;
            }

            if (command.StartsWith("/s") || command.StartsWith("-s"))
            {
                RunFullScreen();
                return;
            }

            using (ConfigForm dialog = new ConfigForm())
                dialog.ShowDialog();
        }

        private static void RunFullScreen()
        {
            SaverSettings settings = SaverSettings.Load();
            List<SaverForm> forms = new List<SaverForm>();
            foreach (Screen screen in Screen.AllScreens)
                forms.Add(new SaverForm(screen.Bounds, settings));

            Cursor.Hide();
            SaverContext context = new SaverContext(forms);
            SaverForm.ExitRequested += context.ExitAll;
            try { Application.Run(context); }
            finally
            {
                SaverForm.ExitRequested -= context.ExitAll;
                Cursor.Show();
            }
        }
    }

    internal sealed class SaverContext : ApplicationContext
    {
        private readonly List<SaverForm> forms;
        private bool exiting;

        public SaverContext(List<SaverForm> forms)
        {
            this.forms = forms;
            foreach (SaverForm form in forms)
            {
                form.FormClosed += FormClosed;
                form.Show();
            }
        }

        public void ExitAll(object sender, EventArgs e)
        {
            if (exiting) return;
            exiting = true;
            foreach (SaverForm form in forms.ToArray())
                if (!form.IsDisposed) form.Close();
            ExitThread();
        }

        private void FormClosed(object sender, FormClosedEventArgs e)
        {
            forms.Remove((SaverForm)sender);
            if (forms.Count == 0) ExitThread();
        }
    }
}
