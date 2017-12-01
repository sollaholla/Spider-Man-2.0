using GTA;
using GTA.Math;
using GTA.Native;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     Contains raycast helper functions.
    /// </summary>
    public static class WorldProbe
    {
        /// <summary>
        ///     Start's a shape test ray (line).
        /// </summary>
        /// <param name="startCoords">The starting coordinates.</param>
        /// <param name="endCoords">The ending coordinates.</param>
        /// <param name="flags">The flags specifying what this shape test interacts with.</param>
        /// <param name="ignoreEntity">The entity to ignore.</param>
        /// <returns></returns>
        public static ShapeTest StartShapeTestRay(Vector3 startCoords, Vector3 endCoords, ShapeTestFlags flags,
            Entity ignoreEntity)
        {
            var entityHandle = ignoreEntity?.Handle ?? 0;
            return new ShapeTest(Function.Call<int>(Hash._0x377906D8A31E5586,
                startCoords.X, startCoords.Y, startCoords.Z,
                endCoords.X, endCoords.Y, endCoords.Z,
                (int) flags,
                entityHandle));
        }

        /// <summary>
        ///     Start's a shape test in the form of a capsule.
        /// </summary>
        /// <param name="startCoords">The starting coords (bottom of the capsule)</param>
        /// <param name="endCoords">The ending coords (top of the capsule)</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="flags">The flags specifying what this shape test interacts with.</param>
        /// <param name="ignoreEntity">The entity to ignore.</param>
        /// <param name="p9">An unknown variable (always 7). Just including this incase it needs changed.</param>
        /// <returns></returns>
        public static ShapeTest StartShapeTestCapsule(Vector3 startCoords, Vector3 endCoords, float radius,
            ShapeTestFlags flags,
            Entity ignoreEntity, int p9 = 7)
        {
            var entityHandle = ignoreEntity?.Handle ?? 0;
            return new ShapeTest(Function.Call<int>(Hash._0x28579D1B8F8AAC80,
                startCoords.X, startCoords.Y, startCoords.Z,
                endCoords.X, endCoords.Y, endCoords.Z,
                radius,
                (int) flags,
                entityHandle));
        }
    }
}