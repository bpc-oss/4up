using System;
using System.Threading;
using System.Windows.Forms;

namespace Life4Up
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            Mutex m = new Mutex(true, "4up-life-single-instance", out created);
            if (!created) { Trace("start skipped: another instance holds mutex"); return; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += delegate { Trace("ApplicationExit"); };
            AppDomain.CurrentDomain.ProcessExit += delegate { Trace("ProcessExit"); };
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { Trace("ThreadException"); Log(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { Trace("UnhandledExit isTerminating=" + e.IsTerminating); Log(e.ExceptionObject as Exception); };
            Trace("start");
            new Controller();
            Application.Run();
        }

        internal static void Trace(string msg)
        {
            try
            {
                string dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Life4Up");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "life.log"), DateTime.Now.ToString("MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine);
            }
            catch (Exception) { }
        }

        internal static void Log(Exception ex)
        {
            if (ex == null) return;
            try
            {
                string dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Life4Up");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), DateTime.Now + "  " + ex + Environment.NewLine);
            }
            catch (Exception) { }
        }
    }
}
