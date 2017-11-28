using GTA;
using GTA.Native;

namespace spiderman.net.Library.Extensions
{
    public static class CameraExtensions
    {
        /// <summary>
        /// Start's rendering script cameras.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="easeTime"></param>
        public static void TransitionIn(this Camera camera, int easeTime)
        {
            Function.Call(Hash.RENDER_SCRIPT_CAMS, true, true, easeTime, false, false);
        }
    }
}
