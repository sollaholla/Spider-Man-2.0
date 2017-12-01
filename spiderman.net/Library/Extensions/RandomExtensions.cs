using System;

namespace SpiderMan.Library.Extensions
{
    public static class RandomExtensions
    {
        /// <summary>
        ///     Returns a random double between minValue and maxValue.
        /// </summary>
        /// <param name="random">The random object.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <returns></returns>
        public static double RandomNumberBetween(this Random random, double minValue, double maxValue)
        {
            return minValue + random.NextDouble() * (maxValue - minValue);
        }
    }
}