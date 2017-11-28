using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spiderman.net.Library.Extensions
{
    public static class Vector3Extensions
    {
        public static Vector3 TransformVector(this Vector3 i, Func<float, float> method)
        {
            return new Vector3()
            {
                X = method(i.X),
                Y = method(i.Y),
                Z = method(i.Z),
            };
        }

        public static Vector3 Denormalize(this Vector3 v)
        {
            return new Vector3(v.X.Denormalize(), v.Y.Denormalize(), v.Z.Denormalize());
        }
    }
}
