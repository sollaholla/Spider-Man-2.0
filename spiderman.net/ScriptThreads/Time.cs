using System;
using System.Diagnostics;
using GTA;

namespace SpiderMan.ScriptThreads
{
    public class Time : Script
    {
        private readonly Stopwatch sw;

        public Time()
        {
            sw = new Stopwatch();
            sw.Start();
            Tick += OnTick;
        }

        /// <summary>
        ///     Time passed since the last frame. Used for calculations independant of frame rate.
        /// </summary>
        public static float UnscaledDeltaTime { get; private set; }

        public static float DeltaTime { get; private set; }

        private void OnTick(object sender, EventArgs e)
        {
            DeltaTime = Game.LastFrameTime;
            sw.Stop();
            if (sw.ElapsedMilliseconds != 0)
                UnscaledDeltaTime = sw.ElapsedMilliseconds / 1000f;
            sw.Restart();
        }
    }
}