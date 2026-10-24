using System;
using System.Collections.Generic;

namespace Life4Up
{
    public class Gauge
    {
        public string Key;
        public string Name;
        public string Balloon;
        public int DurationMin;
        public double Level;
        public double EmptyMin;
        public bool FailedToday;
        public bool Notified;

        public Gauge(string key, string name, string balloon, int min)
        {
            Key = key; Name = name; Balloon = balloon; DurationMin = min; Level = 100;
        }
    }

    // 机制 1:1 对齐 1up（见 01-reversing.md §2）：
    // 线性衰减 / water·kegel 补给 +100/3 其余回满 / 空槽 30min 宽限 / 跨天 used+1 failed清零 / 锁屏睡眠=回满
    public class LifeState
    {
        public List<Gauge> Gauges;
        public DateTime Day;
        public int Streak;
        public int UsedToday;          // 语义：当日主动补给次数（复刻取舍，见 README）
        public int FailedTodayCount;
        public bool Resting;
        public string Mode = "always";   // always=常驻桌面 | onempty=仅提醒时出现
        public DateTime PauseUntil = DateTime.MinValue;   // 勿扰：暂停衰减与提醒

        public LifeState()
        {
            Day = DateTime.Now.Date;
            Gauges = new List<Gauge>();
            Gauges.Add(new Gauge("hp", "起身", "该起身活动一下了", 45));
            Gauges.Add(new Gauge("water", "喝水", "喝杯水吧，身体在等你", 30));
            Gauges.Add(new Gauge("san", "远眺", "看看 6 米外，20 秒", 20));
            Gauges.Add(new Gauge("kegel", "提肛", "来一组提肛 5 秒 x10", 60));
        }

        public static string NewKey()
        {
            return "u" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public void Work(int seconds)
        {
            double minutes = seconds / 60.0;
            foreach (Gauge g in Gauges)
            {
                if (g.Level <= 0.0)
                {
                    g.EmptyMin += minutes;
                    if (!g.FailedToday && g.EmptyMin >= 30.0) { g.FailedToday = true; FailedTodayCount++; }
                }
                else
                {
                    g.Level -= seconds / (g.DurationMin * 60.0) * 100.0;
                    g.EmptyMin = 0;
                    if (g.Level < 0.0) g.Level = 0.0;
                }
            }
        }

        public void Rest()
        {
            foreach (Gauge g in Gauges) { g.Level = 100; g.EmptyMin = 0; }
        }

        public void Replenish(string key)
        {
            foreach (Gauge g in Gauges)
            {
                if (g.Key == key)
                {
                    if (key == "water" || key == "kegel") g.Level += 100.0 / 3.0; else g.Level = 100;
                    if (g.Level > 100) g.Level = 100;
                    g.EmptyMin = 0;
                    UsedToday++;
                }
            }
        }

        public bool Rollover()
        {
            DateTime today = DateTime.Now.Date;
            if (today <= Day) return false;
            if (UsedToday > 0) Streak++;
            else if (FailedTodayCount > 0) Streak = 0;
            UsedToday = 0; FailedTodayCount = 0;
            foreach (Gauge g in Gauges) g.FailedToday = false;
            Day = today;
            return true;
        }

        public Gauge Weakest()
        {
            Gauge w = null;
            foreach (Gauge g in Gauges) if (w == null || g.Level < w.Level) w = g;
            return w;
        }

        public bool AllEmpty()
        {
            foreach (Gauge g in Gauges) if (g.Level > 0.0) return false;
            return true;
        }
    }

    public class SaveData
    {
        public int v;
        public string day;
        public int streak;
        public int used;
        public int failed;
        public double[] levels;
        public int x;
        public int y;
        public string mode;
        public int[] mins;
        public ItemData[] items;
        public long pause;
    }
}
