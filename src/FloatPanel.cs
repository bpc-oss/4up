using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Life4Up
{
    // 对应 1up NSPanel+PanelViews：无边框置顶浮窗，10 段能量条+补给钮+轮值文案+连击
    public class FloatPanel : Form
    {
        LifeState s;
        CopyBook copy;
        Action<string> onReplenish;
        ContextMenuStrip menu;
        Timer ui;
        int slow;
        string msg;
        DateTime happyUntil = DateTime.MinValue;
        struct Hit { public Rectangle R; public string Key; }
        List<Hit> hits;
        bool dragging;
        Point down;
        Point last;
        string pending;
        int visSec;   // onempty 模式下浮窗已可见秒数
        Font nameFont;
        Font smallFont;
        List<Hit> rowHits;
        ToolTip tip;
        string lastTip = "";

        public FloatPanel(LifeState state, CopyBook copyBook, Action<string> replenish)
        {
            s = state; copy = copyBook; onReplenish = replenish;
            hits = new List<Hit>();
            rowHits = new List<Hit>();
            tip = new ToolTip();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            Size = new Size(244, 150);
            BackColor = Color.FromArgb(30, 30, 40);
            DoubleBuffered = true;
            KeyPreview = true;
            nameFont = new Font("Microsoft YaHei UI", 9f);
            smallFont = new Font("Microsoft YaHei UI", 8.25f);
            msg = copy.Pick("normal");
            ui = new Timer();
            ui.Interval = 1000;
            ui.Tick += UiTick;
            ui.Start();
            VisibleChanged += delegate(object a, EventArgs b)
            {
                if (Visible) { slow = 0; visSec = 0; msg = copy.Pick(ThemeKey()); Invalidate(); }
            };
        }

        public void AttachMenu(ContextMenuStrip m) { menu = m; ContextMenuStrip = m; }

        public void Celebrate() { happyUntil = DateTime.Now.AddMilliseconds(1200); }

        public void FlashEmpty() { visSec = 0; Show(); Activate(); }

        int LayoutH() { int h = 14 + s.Gauges.Count * 27 + 28; return h < 96 ? 96 : h; }

        public void Relayout() { Height = LayoutH(); }

        void UiTick(object sender, EventArgs e)
        {
            slow++;
            if (Visible && s.Mode == "onempty") { visSec++; if (visSec >= 20) Hide(); }   // 仅提醒模式：20s 后自动隐身
            if (slow % 9 == 0) msg = (DateTime.Now < s.PauseUntil) ? "已暂停，托盘右键可恢复" : copy.Pick(ThemeKey());   // 低频刷新
            Invalidate();
        }

        string ThemeKey()
        {
            if (DateTime.Now < happyUntil) return "happy";
            if (s.AllEmpty()) return "empty";
            Gauge w = s.Weakest();
            if (w != null && w.Level < 40.0) return copy.Has(w.Key) ? w.Key : "normal";
            return "normal";
        }

        Color BorderColor()
        {
            if (DateTime.Now < happyUntil) return Color.FromArgb(150, 220, 150);
            Gauge w = s.Weakest();
            if (w != null && w.Level < 40.0) return Palette.For(w, s.Gauges.IndexOf(w));
            if (s.Streak >= 3) return Color.FromArgb(168, 138, 73);
            return Color.FromArgb(70, 70, 92);
        }

        static GraphicsPath Rounded(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle body = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            using (GraphicsPath path = Rounded(body, 14))
            {
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(30, 30, 40))) g.FillPath(bg, path);
                using (Pen pen = new Pen(BorderColor(), 2)) g.DrawPath(pen, path);
            }
            hits.Clear();
            rowHits.Clear();
            int msgY = 14 + s.Gauges.Count * 27 + 2;
            int y = 14;
            for (int gi = 0; gi < s.Gauges.Count; gi++)
            {
                Gauge ga = s.Gauges[gi];
                TextRenderer.DrawText(g, ga.Name, nameFont, new Point(14, y - 1),
                    ga.Level <= 0.0 ? Color.FromArgb(96, 96, 112) : Color.FromArgb(150, 150, 165));
                for (int c = 0; c < 10; c++)
                {
                    Rectangle cell = new Rectangle(54 + c * 15, y, 12, 9);
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(52, 52, 68)))
                        g.FillRectangle(bg, cell);
                    double fill = (ga.Level - c * 10.0) / 10.0;
                    if (fill > 1.0) fill = 1.0;
                    if (fill > 0.0)
                    {
                        int wpx = (int)(cell.Width * fill);
                        if (wpx < 1) wpx = 1;
                        using (SolidBrush br = new SolidBrush(Palette.For(ga, gi)))
                            g.FillRectangle(br, cell.X, cell.Y, wpx, cell.Height);
                    }
                }
                Rectangle pr = new Rectangle(206, y - 3, 18, 15);
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(58, 58, 76))) g.FillRectangle(pb, pr);
                using (Pen pp = PenBorder())
                {
                    g.DrawLine(pp, pr.X + 6, pr.Y + 7, pr.X + 12, pr.Y + 7);
                    g.DrawLine(pp, pr.X + 9, pr.Y + 4, pr.X + 9, pr.Y + 10);
                }
                Hit h = new Hit(); h.R = pr; h.Key = ga.Key; hits.Add(h);
                Hit rh = new Hit(); rh.R = new Rectangle(8, y - 5, 194, 26); rh.Key = ga.Key; rowHits.Add(rh);
                y += 27;
            }
            TextRenderer.DrawText(g, msg, smallFont, new Rectangle(14, msgY, 148, 18),
                Color.FromArgb(226, 226, 238), TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            if (s.Streak > 0)
                TextRenderer.DrawText(g, "连击 x" + s.Streak, smallFont, new Rectangle(160, msgY, 72, 18),
                    Color.FromArgb(255, 176, 64), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        static Pen PenBorder() { return new Pen(Color.FromArgb(200, 200, 215), 1.6f); }

        Gauge GaugeBy(string key)
        {
            foreach (Gauge g in s.Gauges) if (g.Key == key) return g;
            return null;
        }

        string HoverTip(Point p)
        {
            foreach (Hit h in hits)
                if (h.R.Contains(p)) { Gauge g = GaugeBy(h.Key); if (g != null) return TipText(g); }
            foreach (Hit h in rowHits)
                if (h.R.Contains(p)) { Gauge g = GaugeBy(h.Key); if (g != null) return TipText(g); }
            return "";
        }

        string TipText(Gauge g)
        {
            if (DateTime.Now < s.PauseUntil) return g.Name + " · 已暂停";
            if (g.Level <= 0.0) return g.Name + " · 已空 " + (int)g.EmptyMin + " 分";
            int remain = (int)Math.Ceiling(g.Level / 100.0 * g.DurationMin);
            return g.Name + " " + (int)Math.Round(g.Level) + "%，约剩 " + remain + " 分";
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                down = e.Location; last = e.Location; dragging = false; pending = null;
                foreach (Hit h in hits) if (h.R.Contains(down)) pending = h.Key;
                if (pending == null)
                    foreach (Hit h in rowHits) if (h.R.Contains(down)) pending = h.Key;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.None)
            {
                string txt = HoverTip(e.Location);
                if (txt != lastTip) { tip.SetToolTip(this, txt); lastTip = txt; }
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (!dragging && Math.Abs(e.X - down.X) + Math.Abs(e.Y - down.Y) > 4) dragging = true;
            if (dragging)
            {
                Left += e.X - last.X;
                Top += e.Y - last.Y;
                last = e.Location;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                if (!dragging && pending != null)
                {
                    bool done = false;
                    foreach (Hit h in hits)
                        if (h.R.Contains(e.Location)) { onReplenish(h.Key); done = true; break; }
                    if (!done)
                        foreach (Hit h in rowHits)
                            if (h.R.Contains(e.Location)) onReplenish(h.Key);
                }
                dragging = false; pending = null;
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape) { Hide(); return true; }
            return base.ProcessDialogKey(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ui != null) { ui.Stop(); ui.Dispose(); }
                if (nameFont != null) nameFont.Dispose();
                if (smallFont != null) smallFont.Dispose();
                if (tip != null) tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
