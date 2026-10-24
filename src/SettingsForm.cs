using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Life4Up
{
    // 设置窗：提醒项可增删改（名称/周期）+ 显示模式；保存经回调落盘并即时生效
    public class SettingsForm : Form
    {
        class Row
        {
            public TextBox Name;
            public NumericUpDown Min;
            public Button Del;
            public Panel Host;
            public Gauge Src;
        }

        LifeState s;
        Action onSaved;
        Panel list;
        List<Row> rows;
        Button add;
        ComboBox mode;

        public SettingsForm(LifeState state, Action saved)
        {
            s = state; onSaved = saved;
            rows = new List<Row>();
            Text = "4UP 设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true; ShowInTaskbar = false;
            Font = new Font("Microsoft YaHei UI", 9f);
            ClientSize = new Size(452, 452);

            Label head = new Label(); head.Text = "提醒项：可改名 / 改周期（5-480 分钟）/ 删除，最多 10 个"; head.Location = new Point(16, 10); head.AutoSize = true;
            Controls.Add(head);

            list = new Panel(); list.Location = new Point(12, 34); list.Size = new Size(428, 296); list.AutoScroll = true;
            Controls.Add(list);

            for (int i = 0; i < s.Gauges.Count; i++) AddRow(s.Gauges[i]);
            if (rows.Count == 1) rows[0].Del.Enabled = false;

            add = new Button(); add.Text = "＋ 添加提醒项"; add.Location = new Point(12, 338); add.Size = new Size(120, 26);
            add.Click += delegate { if (rows.Count < 10) { AddRow(null); Reflow(); if (rows.Count == 2) foreach (Row r in rows) r.Del.Enabled = true; } };
            Controls.Add(add);

            Label ml = new Label(); ml.Text = "显示模式"; ml.Location = new Point(32, 380); ml.AutoSize = true;
            mode = new ComboBox(); mode.DropDownStyle = ComboBoxStyle.DropDownList;
            mode.Items.AddRange(new object[] { "常驻桌面（始终可见）", "仅提醒时出现（平时隐身）" });
            mode.SelectedIndex = (s.Mode == "onempty") ? 1 : 0;
            mode.Location = new Point(120, 376); mode.Width = 220;
            Controls.Add(ml); Controls.Add(mode);

            Button ok = new Button(); ok.Text = "保存"; ok.Location = new Point(140, 412); ok.Width = 100;
            ok.Click += delegate { Apply(); };
            Button ca = new Button(); ca.Text = "取消"; ca.Location = new Point(252, 412); ca.Width = 100;
            ca.Click += delegate { Close(); };
            Controls.Add(ok); Controls.Add(ca);
            AcceptButton = ok; CancelButton = ca;
        }

        void AddRow(Gauge src)
        {
            Row r = new Row();
            r.Src = src;
            r.Host = new Panel(); r.Host.Size = new Size(404, 26);
            r.Name = new TextBox(); r.Name.Location = new Point(2, 2); r.Name.Width = 150; r.Name.MaxLength = 12;
            r.Name.Text = (src != null) ? src.Name : "新提醒";
            r.Min = new NumericUpDown(); r.Min.Minimum = 5; r.Min.Maximum = 480;
            r.Min.Value = (src != null) ? src.DurationMin : 30;
            r.Min.Location = new Point(162, 2); r.Min.Width = 64;
            r.Del = new Button(); r.Del.Text = "删"; r.Del.Size = new Size(30, 23); r.Del.Location = new Point(234, 1);
            r.Del.Click += delegate
            {
                if (rows.Count <= 1) return;
                rows.Remove(r); list.Controls.Remove(r.Host);
                if (rows.Count == 1) rows[0].Del.Enabled = false;
                Reflow();
            };
            r.Host.Controls.Add(r.Name); r.Host.Controls.Add(r.Min); r.Host.Controls.Add(r.Del);
            r.Host.Top = rows.Count * 28; r.Host.Left = 0;
            list.Controls.Add(r.Host);
            rows.Add(r);
            if (add != null) add.Enabled = rows.Count < 10;
        }

        void Reflow()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].Host.Top = i * 28;
        }

        void Apply()
        {
            List<Gauge> ns = new List<Gauge>();
            foreach (Row r in rows)
            {
                string name = r.Name.Text.Trim();
                if (name.Length == 0) name = (r.Src != null) ? r.Src.Name : "提醒";
                int min = (int)r.Min.Value;
                Gauge g = r.Src;
                if (g != null) { g.Name = name; g.DurationMin = min; }
                else g = new Gauge(LifeState.NewKey(), name, "", min);
                ns.Add(g);
            }
            s.Gauges.Clear(); foreach (Gauge g in ns) s.Gauges.Add(g);
            s.Mode = (mode.SelectedIndex == 1) ? "onempty" : "always";
            if (onSaved != null) onSaved();
            Close();
        }
    }
}