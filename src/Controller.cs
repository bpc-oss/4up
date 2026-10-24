using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Life4Up
{
    // 对应 1up AppController：1s 采样/30s 落盘/开窗 1s 关窗 5s 节奏；锁屏与睡眠=休息；空槽一次 balloon
    public class Controller : Form
    {
        LifeState state;
        CopyBook copy;
        TrayController tray;
        FloatPanel panel;
        Timer timer;
        ContextMenuStrip menu;
        int tick;
        SettingsForm settings;
        ToolStripMenuItem miPause;
        ToolStripMenuItem miResume;

        public Controller()
        {
            Point pos;
            state = Store.Load(out pos);
            copy = new CopyBook(Application.StartupPath);
            foreach (Gauge g in state.Gauges) g.Balloon = copy.Resolve(g.Key, g.Name);
            tray = new TrayController();
            panel = new FloatPanel(state, copy, OnReplenish);
            if (pos.X != 0 || pos.Y != 0) panel.Location = Clamp(pos);
            else
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                panel.Location = new Point(wa.Width - 264, 120);
            }
            if (state.Mode != "onempty") panel.Show();   // 常驻模式启动即显示；仅提醒模式隐身等待
            else tray.Note("4UP", "已就绪：提醒项空槽时才弹出浮窗，托盘左键可随时呼出");
            tray.OnActivate = TogglePanel;
            BuildMenu();
            tray.AttachMenu(menu);
            panel.AttachMenu(menu);
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += OnTick;
            timer.Start();
            SystemEvents.PowerModeChanged += OnPower;
            SystemEvents.SessionSwitch += OnSession;
            object handle = Handle;   // 建句柄，隐藏窗体也跑定时器
            tray.Update(state);
            Store.Save(state, panel.Location);
        }

        void BuildMenu()
        {
            menu = new ContextMenuStrip();
            ToolStripMenuItem miShow = new ToolStripMenuItem("显示 / 隐藏浮窗");
            miPause = new ToolStripMenuItem("暂停提醒 1 小时");
            miPause.Click += delegate
            {
                state.PauseUntil = DateTime.Now.AddHours(1);
                Store.Save(state, panel.Location);
                tray.Note("4UP", "已暂停 1 小时，到点自动恢复");
                panel.Invalidate();
            };
            miResume = new ToolStripMenuItem("恢复提醒");
            miResume.Enabled = false;
            miResume.Click += delegate
            {
                state.PauseUntil = DateTime.MinValue;
                Store.Save(state, panel.Location);
                panel.Invalidate();
            };
            ToolStripMenuItem miSet = new ToolStripMenuItem("设置…");
            miSet.Click += delegate { OpenSettings(); };
            miShow.Click += delegate { TogglePanel(); };
            ToolStripMenuItem miAuto = new ToolStripMenuItem("开机自启");
            miAuto.Checked = AutoStart.On();
            miAuto.Click += delegate { AutoStart.Set(!AutoStart.On()); miAuto.Checked = AutoStart.On(); };
            ToolStripMenuItem miEdit = new ToolStripMenuItem("编辑文案…");
            miEdit.Click += delegate { EditCopy(); };
            ToolStripMenuItem miAbout = new ToolStripMenuItem("关于 4UP");
            miAbout.Click += delegate { About(); };
            ToolStripMenuItem miExit = new ToolStripMenuItem("退出");
            miExit.Click += delegate { ExitApp(); };
            menu.Items.AddRange(new ToolStripItem[] { miShow, miPause, miResume, miSet, miAuto, miEdit, miAbout, new ToolStripSeparator(), miExit });
        }

        void TogglePanel()
        {
            if (panel.Visible) panel.Hide(); else { panel.Show(); panel.Activate(); }
        }

        void EditCopy()
        {
            try
            {
                System.Diagnostics.Process p = System.Diagnostics.Process.Start("notepad.exe", copy.Path);
                System.Threading.Thread t = new System.Threading.Thread(delegate()
                {
                    try { p.WaitForExit(); }
                    catch (Exception) { }
                    copy.Reload();
                });
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception) { }
        }

        void OpenSettings()
        {
            if (settings != null && !settings.IsDisposed) { settings.Activate(); return; }
            string prev = state.Mode;
            settings = new SettingsForm(state, delegate
            {
                try
                {
                    Store.Save(state, panel.Location);
                    foreach (Gauge g in state.Gauges) g.Balloon = copy.Resolve(g.Key, g.Name);
                    panel.Relayout();
                    panel.Invalidate();
                    tray.Update(state);
                    if (state.Mode == "onempty" && panel.Visible) panel.Hide();
                    if (prev != "onempty" && state.Mode == "onempty")
                        tray.Note("4UP 仅提醒模式", "浮窗已隐身，到点自动弹出；托盘图标保留（任务栏右下角，可能在 ∧ 折叠区）。退出请用托盘右键菜单。");
                }
                catch (Exception ex) { Program.Log(ex); }
            });
            settings.Show();
        }

        void About()
        {
            string mins = "";
            foreach (Gauge g in state.Gauges) mins += g.Name + " " + g.DurationMin + " 分 / ";
            MessageBox.Show("4UP — 灵感来自 xfey/1up 的 Windows 复刻。\n\n四槽：" + mins + "\n离开 15 分钟或锁屏睡眠 → 全部回满\n空槽 30 分钟记一次失败；跨天结算连击\n数据仅存本机 %APPDATA%\\Life4Up", "关于 4UP");
        }

        void OnReplenish(string key)
        {
            state.Replenish(key);
            panel.Celebrate();
            panel.Invalidate();
            tray.Update(state);
            Store.Save(state, panel.Location);
        }

        void OnTick(object sender, EventArgs e)
        {
            tick++;
            if (tick % 600 == 0) Program.Trace("alive t=" + tick);
            bool paused = state.PauseUntil > DateTime.Now;
            int idle = Monitor.IdleSeconds();
            bool restNow = idle >= 900;
            if (restNow && !state.Resting)
            {
                state.Resting = true; state.Rest();
                tray.Update(state); Store.Save(state, panel.Location);
            }
            if (!restNow && state.Resting) state.Resting = false;
            if (!restNow && !paused) state.Work(1);
            if (state.Rollover()) { Program.Trace("rollover -> streak=" + state.Streak); tray.Update(state); Store.Save(state, panel.Location); }
            foreach (Gauge g in state.Gauges)
            {
                if (g.Level <= 0.0)
                {
                    if (!g.Notified)
                    {
                        g.Notified = true;
                        if (state.Mode == "onempty") panel.FlashEmpty();
                        else if (!panel.Visible) tray.Balloon(g);
                        Program.Trace("empty-notify key=" + g.Key);
                    }
                }
                else if (g.Level > 1.0) g.Notified = false;
            }
            float op = paused ? 0.55f : 1f;
            if (panel.Opacity != op) panel.Opacity = op;
            if (miPause != null) miPause.Checked = paused;
            if (miResume != null) miResume.Enabled = paused;
            if (panel.Visible) panel.Invalidate();
            else if (tick % 300 == 0) { panel.Invalidate(); tray.Update(state); }
            if (tick % 30 == 0) Store.Save(state, panel.Location);
        }

        void OnPower(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                state.Resting = true; state.Rest();
                Store.Save(state, panel.Location);
            }
        }

        void OnSession(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock
                || e.Reason == SessionSwitchReason.RemoteDisconnect
                || e.Reason == SessionSwitchReason.ConsoleDisconnect)
            {
                state.Resting = true; state.Rest();
                Store.Save(state, panel.Location);
            }
        }

        Point Clamp(Point p)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (p.X < wa.Left || p.X > wa.Right - 100 || p.Y < wa.Top || p.Y > wa.Bottom - 60)
                p = new Point(wa.Width - 264, 120);
            return p;
        }

        void ExitApp()
        {
            Store.Save(state, panel.Location);
            tray.Dispose();
            panel.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timer != null) { timer.Stop(); timer.Dispose(); }
                SystemEvents.PowerModeChanged -= OnPower;
                SystemEvents.SessionSwitch -= OnSession;
                if (tray != null) tray.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    static class AutoStart
    {
        const string KeyName = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string ValueName = "4up";

        public static bool On()
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(KeyName, false))
            {
                if (k == null) return false;
                return k.GetValue(ValueName) != null;
            }
        }

        public static void Set(bool on)
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(KeyName))
            {
                if (on) k.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue(ValueName, false);
            }
        }
    }
}
