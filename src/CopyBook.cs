using System;
using System.Collections.Generic;
using System.IO;

namespace Life4Up
{
    // 对应 1up 外置文案 config/microcopy.js：纯文本分节，可随意增删行，改完即时生效（菜单“编辑文案”）
    public class CopyBook
    {
        public string Path;
        Dictionary<string, List<string>> sections;
        static Random rand = new Random();

        public CopyBook(string dir)
        {
            Path = System.IO.Path.Combine(dir, "copy.txt");
            if (!File.Exists(Path)) File.WriteAllText(Path, DEFAULT);
            Load();
        }

        public void Reload() { Load(); }

        void Load()
        {
            try { sections = Parse(File.ReadAllLines(Path)); }
            catch (Exception) { sections = new Dictionary<string, List<string>>(); }
        }

        static Dictionary<string, List<string>> Parse(string[] lines)
        {
            Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
            List<string> cur = null;
            foreach (string raw in lines)
            {
                string t = raw.Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    cur = new List<string>();
                    d[t.Substring(1, t.Length - 2)] = cur;
                }
                else if (cur != null && t.Length > 0) cur.Add(t);
            }
            return d;
        }

        public string Pick(string section)
        {
            if (sections == null) return "";
            List<string> list;
            if (!sections.TryGetValue(section, out list) || list.Count == 0) return "";
            return list[rand.Next(list.Count)];
        }

        public bool Has(string section)
        {
            return sections != null && !string.IsNullOrEmpty(section) && sections.ContainsKey(section);
        }

        // 自定义提醒项文案：[节名]/[名字] 命中即用，否则套用通用模板
        public string Resolve(string key, string name)
        {
            string s = Pick(key);
            if (s.Length > 0) return s;
            s = Pick(name);
            if (s.Length > 0) return s;
            string[] gen = { "该" + name + "了", name + "的时间到了", "来，做一组" + name + "！" };
            return gen[rand.Next(gen.Length)];
        }

        const string DEFAULT = @"[normal]
状态条不代表真实健康，只是提醒你照顾自己。
连击越高，今天越稳。
空了也别慌，补一口就回来。
数据只存在本机，放心发呆。

[hp]
起来晃两步，顺便接杯水。
站起身，伸个懒腰。

[water]
喝口水，就现在。
小口慢咽，别牛饮。

[san]
看 6 米外 20 秒，眨眨眼。
窗外的绿色更养眼。

[kegel]
收腹提肛 5 秒 x10，没人看得出来。
悄悄练，稳稳的。

[empty]
有槽空了，照顾一下自己。

[happy]
回血成功，漂亮！
";
    }
}
