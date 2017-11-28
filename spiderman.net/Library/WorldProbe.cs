using GTA;
using GTA.Math;
using GTA.Native;
using spiderman.net.Library.Extensions;

namespace spiderman.net.Library
{
    /// <summary>
    /// The flags that will determine what the raycasts check for.
    /// </summary>
    public enum ShapeTestFlags : int
    {
        IntersectMap = 1,
        IntersectVehicles = 2,
        IntersectPeds = 4,
        IntersectPeds2 = 8,
        IntersectObjects = 16,
        IntersectWater = 32,
        Unknown1 = 64,
        Unknown2 = 128,
        IntersectVegitation = 256,
        Everything = 511
    }

    /// <summary>
    /// Contains raycast helper functions.
    /// </summary>
    public static class WorldProbe
    {
        /// <summary>
        /// Start's a shape test ray (line).
        /// </summary>
        /// <param name="startCoords">The starting coordinates.</param>
        /// <param name="endCoords">The ending coordinates.</param>
        /// <param name="flags">The flags specifying what this shape test interacts with.</param>
        /// <param name="ignoreEntity">The entity to ignore.</param>
        /// <returns></returns>
        public static ShapeTest StartShapeTestRay(Vector3 startCoords, Vector3 endCoords, ShapeTestFlags flags, Entity ignoreEntity)
        {
            var entityHandle = ignoreEntity?.Handle ?? 0;
            return new ShapeTest(Function.Call<int>(Hash._0x377906D8A31E5586,
                startCoords.X, startCoords.Y, startCoords.Z,
                endCoords.X, endCoords.Y, endCoords.Z,
                (int)flags,
                entityHandle));
        }

        /// <summary>
        /// Start's a shape test in the form of a capsule.
        /// </summary>
        /// <param name="startCoords">The starting coords (bottom of the capsule)</param>
        /// <param name="endCoords">The ending coords (top of the capsule)</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="flags">The flags specifying what this shape test interacts with.</param>
        /// <param name="ignoreEntity">The entity to ignore.</param>
        /// <param name="p9">An unknown variable (always 7). Just including this incase it needs changed.</param>
        /// <returns></returns>
        public static ShapeTest StartShapeTestCapsule(Vector3 startCoords, Vector3 endCoords, float radius, ShapeTestFlags flags,
            Entity ignoreEntity, int p9 = 7)
        {
            var entityHandle = ignoreEntity?.Handle ?? 0;
            return new ShapeTest(Function.Call<int>(Hash._0x28579D1B8F8AAC80,
                startCoords.X, startCoords.Y, startCoords.Z,
                endCoords.X, endCoords.Y, endCoords.Z,
                radius, 
                (int)flags, 
                entityHandle));
        }
    }

    /// <summary>
    /// Used as a holder for a shape test, so it's result
    /// can be recieved at any given time. 
    /// </summary>
    public struct ShapeTest
    {
        private int _handle;

        /// <summary>
        /// The main constructor.
        /// </summary>
        /// <param name="handle">The handle of the shape test. Used to attain a result.</param>
        public ShapeTest(int handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// The index in the game's internal array of this shape test.
        /// </summary>
        public int Handle => _handle;

        /// <summary>
        /// Get's the shape test result of this ray.
        /// </summary>
        /// <returns></returns>
        public ShapeTestResult GetResult()
        {
            if (_handle == 0)
                return default(ShapeTestResult);

            ShapeTestResult ss;

            var hit = new OutputArgument();
            var endCoords = new OutputArgument();
            var surfaceNormal = new OutputArgument();
            var entityHit = new OutputArgument();

            var handle = Function.Call<int>(Hash._0x3D87450E15D98694, _handle, hit, endCoords,
                surfaceNormal, entityHit);

            var entity = entityHit.GetResult<Entity>();
            var entityType = entity?.GetEntityType() ?? EntityType.None;

            ss = new ShapeTestResult(handle, hit.GetResult<bool>(), endCoords.GetResult<Vector3>(),
                surfaceNormal.GetResult<Vector3>(), entity, (EntityType)entityType);

            return ss;
        }
    }

    /// <summary>
    /// The result of a shape test.
    /// </summary>
    public struct ShapeTestResult
    {
        private int _handle;

        /// <summary>
        /// The main constructor.
        /// </summary>
        /// <param name="handle">The results handle.</param>
        /// <param name="hit">Whether or not we hit something.</param>
        /// <param name="endCoords">The ending coordinates.</param>
        /// <param name="surfaceNormal">The normal vector of the surface.</param>
        /// <param name="entityHit">The entity that was hit.</param>
        /// <param name="entityType">The type of entity that was hit.</param>
        public ShapeTestResult(int handle, bool hit, Vector3 endCoords, 
            Vector3 surfaceNormal, Entity entityHit, EntityType entityType) : this()
        {
            _handle = handle;
            Hit = hit;
            EndCoords = endCoords;
            SurfaceNormal = surfaceNormal;
            EntityHit = entityHit;
            EntityType = entityType;
        }

        /// <summary>
        /// True if the ray hit anything.
        /// </summary>
        public bool Hit { get; }

        /// <summary>
        /// The location that was hit by the shape test.
        /// </summary>
        public Vector3 EndCoords { get; }

        /// <summary>
        /// The normal vector of the surface hit.
        /// </summary>
        public Vector3 SurfaceNormal { get; }

        /// <summary>
        /// The entity that was hit by the shape test.
        /// </summary>
        public Entity EntityHit { get; }

        /// <summary>
        /// The type of entity the shape test hit.
        /// </summary>
        public EntityType EntityType { get; }
    }
}
