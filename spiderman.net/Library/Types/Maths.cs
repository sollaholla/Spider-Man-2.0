using System;
using GTA.Math;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     Contains math related functions and extensions.
    /// </summary>
    public static class Maths
    {
        public const float PI = 3.14159274f;

        public const float Infinity = float.PositiveInfinity;

        public const float NegativeInfinity = float.NegativeInfinity;

        public const float Deg2Rad = 0.0174532924f;

        public const float Rad2Deg = 57.29578f;

        /// <summary>
        ///     Clamp a float between the range of 0 to 1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float Clamp01(float value)
        {
            float result;
            if (value < 0f)
                result = 0f;
            else if (value > 1f)
                result = 1f;
            else
                result = value;
            return result;
        }

        /// <summary>
        ///     Locks a value between min and max.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns></returns>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        /// <summary>
        ///     Moves a to b by the amount t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        /// <summary>
        ///     Creates a rotation which rotates angle degrees around axis.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            return AngleAxis(angle, ref axis);
        }

        /// <summary>
        ///     Our helper function to the other AngleAxis method.
        /// </summary>
        /// <param name="degress"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private static Quaternion AngleAxis(float degress, ref Vector3 axis)
        {
            if (axis.LengthSquared() == 0)
                return Quaternion.Identity;
            const float degToRad = (float) (Math.PI / 180.0);
            var result = Quaternion.Identity;
            var radians = degress * degToRad;
            radians *= 0.5f;
            axis.Normalize();
            axis = axis * (float) Math.Sin(radians);
            result.X = axis.X;
            result.Y = axis.Y;
            result.Z = axis.Z;
            result.W = (float) Math.Cos(radians);
            return Quaternion.Normalize(result);
        }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            float roll;
            float pitch;
            float yaw;

            // roll (x-axis rotation)
            var sinr = 2 * (q.W * q.X + q.Y * q.Z);
            var cosr = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            roll = (float) Math.Atan2(sinr, cosr);

            // pitch (y-axis rotation)
            var sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1f)
                if (sinp < 0f) pitch = -((float) Math.PI / 2);
                else pitch = (float) Math.PI / 2;
            else pitch = (float) Math.Asin(sinp);

            // yaw (z-axis rotation)
            var siny = 2 * (q.W * q.Z + q.X * q.Y);
            var cosy = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            yaw = (float) Math.Atan2(siny, cosy);

            return new Vector3(roll, pitch, yaw) * Rad2Deg;
        }

        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            forward.Normalize();

            var vector = Vector3.Normalize(forward);
            var vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
            var vector3 = Vector3.Cross(vector, vector2);
            var m00 = vector2.X;
            var m01 = vector2.Y;
            var m02 = vector2.Z;
            var m10 = vector3.X;
            var m11 = vector3.Y;
            var m12 = vector3.Z;
            var m20 = vector.X;
            var m21 = vector.Y;
            var m22 = vector.Z;


            var num8 = m00 + m11 + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float) Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion.Fix();
            }
            if (m00 >= m11 && m00 >= m22)
            {
                var num7 = (float) Math.Sqrt(1f + m00 - m11 - m22);
                var num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion.Fix();
            }
            if (m11 > m22)
            {
                var num6 = (float) Math.Sqrt(1f + m11 - m00 - m22);
                var num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion.Fix();
            }
            var num5 = (float) Math.Sqrt(1f + m22 - m00 - m11);
            var num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion.Fix();
        }

        /// <summary>
        ///     Get's the rotation needed to face the specified direction.
        ///     Default plane normal is our up vector.
        /// </summary>
        /// <param name="direction">The direction to face.</param>
        /// <returns></returns>
        public static Quaternion LookRotation(Vector3 direction)
        {
            return LookRotation(direction, Vector3.WorldUp);
        }

        private static Quaternion UnFix(this Quaternion q)
        {
            return q * Quaternion.Euler(0, 90, -180);
        }

        private static Quaternion Fix(this Quaternion q)
        {
            return q * Quaternion.Euler(0, -90, 180);
        }

        private static Vector3 Fix(this Vector3 v)
        {
            return new Vector3(v.X, v.Z, v.Y);
        }

        /// <summary>
        ///     Moves the current vector3 towards the target by maxDistanceDelta
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="maxDistanceDelta"></param>
        /// <returns></returns>
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            var a = target - current;
            var magnitude = a.Length();
            Vector3 result;
            if (magnitude <= maxDistanceDelta || magnitude < 1.401298E-45f)
                result = target;
            else
                result = current + a / magnitude * maxDistanceDelta;
            return result;
        }
    }
}