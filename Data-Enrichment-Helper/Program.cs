using System;
using System.IO;
using System.Windows.Forms;

namespace Data_Enrichment_Helper;

static class Program
{
    [STAThread]
    static void Main()
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_crash.log");
        void W(string msg)
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}"); } catch { }
        }

        try { File.WriteAllText(logPath, ""); } catch { }

        Application.ThreadException += (s, e) =>
        {
            W($"THREAD EXCEPTION: {e.Exception}");
            MessageBox.Show(e.Exception.ToString(), "Thread Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            W($"APPDOMAIN EXCEPTION: {e.ExceptionObject}");

        try
        {
            W("1. ApplicationConfiguration.Initialize()");
            ApplicationConfiguration.Initialize();

            W("2. new MainForm()");
            var form = new MainForm();
            W("   MainForm constructed OK");

            W("3. Application.Run(form)");
            Application.Run(form);

            W("4. Application.Run returned (form closed normally)");
        }
        catch (Exception ex)
        {
            W($"FATAL: {ex}");
            MessageBox.Show(ex.ToString(), "Startup Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}