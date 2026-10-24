using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web.Script.Serialization;

namespace Life4Up
{
    public class ItemData
    {
        public string n;
        public int m;
        public double l;
    }

    // 对应 1up Persistence：本机 JSON；v2=items 自定义提醒项；v1 旧档自动迁移；日期只前进
    static class Store
    {
        static string Dir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Life4Up");
        }

        static string StatePath()
        {
            return Path.Combine(Dir(), "state.json");
        }

        public static void Save(LifeState s, Point pos)
        {
            try
            {
                Directory.CreateDirectory(Dir());
                SaveData d = new SaveData();
                d.v = 2;
                d.day = s.Day.ToString("yyyy-MM-dd");
                d.streak = s.Streak;
                d.used = s.UsedToday;
                d.failed = s.FailedTodayCount;
                d.x = pos.X; d.y = pos.Y;
                d.mode = s.Mode;
                d.pause = (s.PauseUntil > DateTime.Now) ? s.PauseUntil.Ticks : 0;
                ItemData[] its = new ItemData[s.Gauges.Count];
                for (int i = 0; i < s.Gauges.Count; i++)
                {
                    ItemData it = new ItemData();
                    it.n = s.Gauges[i].Name;
                    it.m = s.Gauges[i].DurationMin;
                    it.l = s.Gauges[i].Level;
                    its[i] = it;
                }
                d.items = its;
                JavaScriptSerializer js = new JavaScriptSerializer();
                File.WriteAllText(StatePath(), js.Serialize(d));
            }
            catch (Exception) { }
        }

        static string BuiltinKey(string name)
        {
            switch (name)
            {
                case "起身": return "hp";
                case "喝水": return "water";
                case "远眺": return "san";
                case "提肛": return "kegel";
                default: return LifeState.NewKey();
            }
        }

        public static LifeState Load(out Point pos)
        {
            pos = new Point(0, 0);
            LifeState s = new LifeState();
            try
            {
                string p = StatePath();
                if (!File.Exists(p)) return s;
                JavaScriptSerializer js = new JavaScriptSerializer();
                SaveData d = js.Deserialize<SaveData>(File.ReadAllText(p));
                if (d == null) return s;
                if (d.v == 2 && d.items != null && d.items.Length > 0)
                {
                    List<Gauge> ns = new List<Gauge>();
                    foreach (ItemData it in d.items)
                    {
                        if (it == null || string.IsNullOrEmpty(it.n)) continue;
                        int m = it.m; if (m < 5) m = 5; if (m > 480) m = 480;
                        double lv = it.l; if (lv < 0) lv = 0; if (lv > 100) lv = 100;
                        Gauge g = new Gauge(BuiltinKey(it.n), it.n, "", m);
                        g.Level = lv;
                        if (lv <= 0.0) g.Notified = true;   // 启动不补弹历史空槽
                        ns.Add(g);
                    }
                    if (ns.Count > 0) s.Gauges = ns;
                }
                else if (d.v == 1 && d.levels != null && d.levels.Length == s.Gauges.Count)
                {
                    for (int i = 0; i < s.Gauges.Count; i++)
                    {
                        double v = d.levels[i];
                        if (v < 0) v = 0; if (v > 100) v = 100;
                        s.Gauges[i].Level = v;
                        if (v <= 0.0) s.Gauges[i].Notified = true;   // 启动不补弹历史空槽
                    }
                    if (d.mins != null && d.mins.Length == s.Gauges.Count)
                        for (int i = 0; i < s.Gauges.Count; i++)
                            if (d.mins[i] >= 5 && d.mins[i] <= 480) s.Gauges[i].DurationMin = d.mins[i];
                }
                s.Streak = d.streak; s.UsedToday = d.used; s.FailedTodayCount = d.failed;
                if (d.mode == "onempty" || d.mode == "always") s.Mode = d.mode;
                if (d.pause > DateTime.Now.Ticks) s.PauseUntil = new DateTime(d.pause);
                DateTime day;
                if (DateTime.TryParseExact(d.day, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out day)) s.Day = day;
                pos = new Point(d.x, d.y);
            }
            catch (Exception) { }
            return s;
        }
    }
}