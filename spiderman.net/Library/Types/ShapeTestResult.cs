using GTA;
using GTA.Math;
using SpiderMan.Library.Extensions;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     The result of a shape test.
    /// </summary>
    public struct ShapeTestResult
    {
        private int _handle;

        /// <summary>
        ///     The main constructor.
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
        ///     True if the ray hit anything.
        /// </summary>
        public bool Hit { get; }

        /// <summary>
        ///     The location that was hit by the shape test.
        /// </summary>
        public Vector3 EndCoords { get; }

        /// <summary>
        ///     The normal vector of the surface hit.
        /// </summary>
        public Vector3 SurfaceNormal { get; }

        /// <summary>
        ///     The entity that was hit by the shape test.
        /// </summary>
        public Entity EntityHit { get; }

        /// <summary>
        ///     The type of entity the shape test hit.
        /// </summary>
        public EntityType EntityType { get; }
    }
}