using System;
using System.Drawing;
using System.Windows.Forms;

namespace Life4Up
{
    static class Palette
    {
        public static Color Of(string key)
        {
            switch (key)
            {
                case "hp": return Color.FromArgb(255, 107, 107);
                case "water": return Color.FromArgb(79, 195, 247);
                case "san": return Color.FromArgb(129, 199, 132);
                case "kegel": return Color.FromArgb(203, 166, 247);
                default: return Color.FromArgb(160, 160, 180);
            }
        }

        public static Color For(Gauge g, int index)
        {
            if (g != null && g.Key != null && g.Key.StartsWith("u")) return ByIndex(index);
            return Of(g == null ? "" : g.Key);
        }

        public static Color ByIndex(int i)
        {
            switch (((i % 8) + 8) % 8)
            {
                case 0: return Of("hp");
                case 1: return Of("water");
                case 2: return Of("san");
                case 3: return Of("kegel");
                case 4: return Color.FromArgb(255, 180, 60);
                case 5: return Color.FromArgb(240, 98, 146);
                case 6: return Color.FromArgb(77, 182, 172);
                default: return Color.FromArgb(120, 144, 156);
            }
        }
    }

    // 对应 1up NSStatusItem：托盘图标随四槽状态实时重绘；左键开合浮窗，右键菜单
    public class TrayController : IDisposable
    {
        NotifyIcon ni;
        public Action OnActivate;

        public TrayController()
        {
            ni = new NotifyIcon();
            ni.Text = "4UP";
            ni.Visible = true;
            ni.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && OnActivate != null) OnActivate();
            };
        }

        public void AttachMenu(ContextMenuStrip m) { ni.ContextMenuStrip = m; }

        public void Update(LifeState s)
        {
            ni.Icon = IconFor(s);
            ni.Text = "4UP · 连击 " + s.Streak + (s.PauseUntil > DateTime.Now ? " · 已暂停" : "");
        }

        Icon IconFor(LifeState s)
        {
            Bitmap b = new Bitmap(16, 16);
            Graphics g = Graphics.FromImage(b);
            g.Clear(Color.FromArgb(24, 24, 34));
            int i = 0;
            int n = s.Gauges.Count; if (n < 1) n = 1;
            int step = n <= 4 ? 3 : 12 / n;
            int hh = step >= 2 ? step - 1 : 1;
            foreach (Gauge ga in s.Gauges)
            {
                int w = 2 + (int)(ga.Level / 100.0 * 12.0);
                using (SolidBrush br = new SolidBrush(Palette.For(ga, i)))
                    g.FillRectangle(br, 1, 2 + i * step, w, hh);
                i++;
            }
            IntPtr h = b.GetHicon();
            g.Dispose(); b.Dispose();
            return Icon.FromHandle(h);
        }

        public void Balloon(Gauge g) { Note(g.Name, g.Balloon); }

        public void Note(string title, string text)
        {
            ni.BalloonTipTitle = title;
            ni.BalloonTipText = text;
            ni.ShowBalloonTip(6000);
        }

        public void Dispose()
        {
            ni.Visible = false;
            ni.Dispose();
        }
    }
}
