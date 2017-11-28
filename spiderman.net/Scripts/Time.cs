using GTA;
using spiderman.net.Library.Memory;
using System.Diagnostics;
using System.Globalization;

namespace spiderman.net.Scripts
{
    public class Time : Script
    {
        private Stopwatch sw;

        /// <summary>
        /// Time passed since the last frame. Used for calculations independant of frame rate.
        /// </summary>
        public static float UnscaledDeltaTime {
            get; private set;
        }

        public static float DeltaTime {
            get; private set;
        }

        public Time()
        {
            sw = new Stopwatch();
            sw.Start();
            Tick += OnTick;
        }

        private void OnTick(object sender, System.EventArgs e)
        {
            DeltaTime = Game.LastFrameTime;
            sw.Stop();
            if (sw.ElapsedMilliseconds != 0)
            {
                UnscaledDeltaTime = sw.ElapsedMilliseconds / 1000f;
            }
            sw.Restart();
        }
    }
}
