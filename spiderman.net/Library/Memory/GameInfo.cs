using static spiderman.net.Library.Memory.MemoryAccess;

namespace spiderman.net.Library.Memory
{
    public unsafe static class CTimeScale
    {
        static float* g_timeScaleValues;

        public static void Init()
        {
            unsafe
            {
                var p = new Pattern("\x48\x8D\x05\x00\x00\x00\x00\xB9\x03\x00\x00\x00\xF3\x0F\x10\x00\x0F\x2F\xC1", "xxx????xxxxxxxxxxxx");
                p.Init();
                var addr = p.Get().ToInt64();
                g_timeScaleValues = (float*)(addr + *(int*)(addr + 3) + 7);
            }
        }

        /// <summary>
        /// Get's the timescale set by this script.
        /// </summary>
        public static float ScriptTimeScale {
            get {
                return g_timeScaleValues[0];
            }
        }

        /// <summary>
        /// Get's the timescale set by the game.
        /// </summary>
        public static float GameTimeScale {
            get {
                return g_timeScaleValues[2];
            }
        }
    }
}
