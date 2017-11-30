using GTA;
using spiderman.net.Scripts;

namespace spiderman.net.Library
{
    public static class GameWaiter
    {
        /// <summary>
        /// Essentially the same as SHVDN Script.Wait except
        /// this uses real game time (and frame rate) instead of system time.
        /// </summary>
        /// <param name="ms"></param>
        public static void Wait(int ms)
        {
            var sec = ms / 1000f;
            while (sec > 0)
            {
                sec -= Time.DeltaTime;
                Script.Yield();
            }
        }
    }
}
