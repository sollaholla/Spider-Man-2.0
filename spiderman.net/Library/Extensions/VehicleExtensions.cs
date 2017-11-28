using GTA;
using GTA.Math;
using GTA.Native;

namespace spiderman.net.Library.Extensions
{
    public static class VehicleExtensions
    {
        /// <summary>
        /// Add's deformation to the specified vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="offset">The offset, relative to the vehicle, where the damage will be applied.</param>
        /// <param name="damage">The amount of damage.</param>
        /// <param name="radius">The radius of the damage.</param>
        public static void SetDamage(this Vehicle vehicle, Vector3 offset, 
            float damage, float radius)
        {
            Function.Call(Hash.SET_VEHICLE_DAMAGE, vehicle.Handle,
                offset.X, offset.Y, offset.Z,
                damage, radius, true);
        }
    }
}
