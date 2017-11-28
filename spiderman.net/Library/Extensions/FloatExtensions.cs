using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spiderman.net.Library.Extensions
{
    public static class FloatExtensions
    {
        public static float ToDegrees(this float val)
        {
            return (float)(val * (180 / Math.PI));
        }

        public static float Denormalize(this float h)
        {
            return h < 0f ? h + 360f : h;
        }
    }
}
