namespace SpiderMan.Library.Memory
{
    public static unsafe class CTimeScale
    {
        private static float* g_timeScaleValues;

        /// <summary>
        ///     Get's the timescale set by this script.
        /// </summary>
        public static float ScriptTimeScale => g_timeScaleValues[0];

        /// <summary>
        ///     Get's the timescale set by the game.
        /// </summary>
        public static float GameTimeScale => g_timeScaleValues[2];

        public static void Init()
        {
            var p = new MemoryAccess.Pattern(
                "\x48\x8D\x05\x00\x00\x00\x00\xB9\x03\x00\x00\x00\xF3\x0F\x10\x00\x0F\x2F\xC1", "xxx????xxxxxxxxxxxx");
            p.Init();
            var addr = p.Get().ToInt64();
            g_timeScaleValues = (float*) (addr + *(int*) (addr + 3) + 7);
        }
    }
}