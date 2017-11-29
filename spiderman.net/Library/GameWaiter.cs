using GTA;
using spiderman.net.Scripts;

namespace spiderman.net.Library
{
    public static class GameWaiter
    {
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
