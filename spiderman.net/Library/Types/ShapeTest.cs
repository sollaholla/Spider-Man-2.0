using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Library.Extensions;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     Used as a holder for a shape test, so it's result
    ///     can be recieved at any given time.
    /// </summary>
    public struct ShapeTest
    {
        /// <summary>
        ///     The main constructor.
        /// </summary>
        /// <param name="handle">The handle of the shape test. Used to attain a result.</param>
        public ShapeTest(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        ///     The index in the game's internal array of this shape test.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        ///     Get's the shape test result of this ray.
        /// </summary>
        /// <returns></returns>
        public ShapeTestResult GetResult()
        {
            if (Handle == 0)
                return default(ShapeTestResult);

            ShapeTestResult ss;

            var hit = new OutputArgument();
            var endCoords = new OutputArgument();
            var surfaceNormal = new OutputArgument();
            var entityHit = new OutputArgument();

            var handle = Function.Call<int>(Hash._0x3D87450E15D98694, Handle, hit, endCoords,
                surfaceNormal, entityHit);

            var entity = entityHit.GetResult<Entity>();
            var entityType = entity?.GetEntityType() ?? EntityType.None;

            ss = new ShapeTestResult(handle, hit.GetResult<bool>(), endCoords.GetResult<Vector3>(),
                surfaceNormal.GetResult<Vector3>(), entity, entityType);

            return ss;
        }
    }
}