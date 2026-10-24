using System;
using System.Runtime.InteropServices;

namespace Life4Up
{
    // 对应 1up ActivityTracker：闲置采样（≥15min 判定为离开=休息）
    static class Monitor
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static int IdleSeconds()
        {
            LASTINPUTINFO li = new LASTINPUTINFO();
            li.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
            if (!GetLastInputInfo(ref li)) return 0;
            long now = (uint)Environment.TickCount;
            long diff = now - li.dwTime;
            if (diff < 0) diff = 0;
            return (int)(diff / 1000);
        }
    }
}
