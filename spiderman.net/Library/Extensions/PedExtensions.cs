using GTA;
using GTA.Math;
using GTA.Native;

namespace spiderman.net.Library.Extensions
{
    /// <summary>
    /// Used for SetIKTarget.
    /// </summary>
    public enum IKIndex
    {
        Head = 1,
        LeftArm = 3,
        RightArm = 4
    }

    /// <summary>
    /// A class that extends the GTA.Ped type.
    /// </summary>
    public static class PedExtensions
    {
        /// <summary>
        /// Start a looped particle on the specified ped.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="assetName">The name of the asset that holds the particle.</param>
        /// <param name="effectName">The name of the particle effect.</param>
        /// <param name="offset">The offset from the bone to play the effect.</param>
        /// <param name="rotation">The rotation of the particle.</param>
        /// <param name="bone">The bone to play the effect on.</param>
        /// <param name="scale">The scale of the particle.</param>
        /// <param name="xAxis"></param>
        /// <param name="yAxis"></param>
        /// <param name="zAxis"></param>
        /// <returns></returns>
        public static ParticleLooped StartLoopedParticle(this Ped ped, string assetName, string effectName, 
            Vector3 offset, Vector3 rotation, Bone bone, float scale, 
            bool xAxis = false, bool yAxis = false, bool zAxis = false)
        {
            var boneIndex = ped.GetBoneIndex(bone);
            Function.Call(Hash._SET_PTFX_ASSET_NEXT_CALL, assetName);
            return new ParticleLooped(Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_ON_PED_BONE, effectName, ped.Handle,
                offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, (int)bone, scale, xAxis, yAxis, zAxis));
        }

        /// <summary>
        /// Face our heading to look at the specified entity.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="other"></param>
        public static void FaceEntity(this Ped ped, Entity other)
        {
            ped.Heading = (other.Position - ped.Position).ToHeading();
        }

        //public static void foo()
        //{
        //    Hash.REQUEST_ADDITIONAL_COLLISION_AT_COORD
        //}

        /// <summary>
        /// Disable / enable ped pain audio (screaming, yelling, etc.) using a toggle.
        /// </summary>
        /// <param name="ped">The ped whom we'd like to disable the pain audio on.</param>
        /// <param name="toggle">The toggle (true = disabled | false = enabled).</param>
        public static void DisablePainAudio(this Ped ped, bool toggle)
        {
            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, ped.Handle, toggle);
        }

        /// <summary>
        /// Clear's the ped's tasks instantly.
        /// </summary>
        /// <param name="ped">The ped.</param>
        public static void ClearTasksImmediately(this Ped ped)
        {
            Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, ped.Handle);
        }

        /// <summary>
        /// Returns true if the ped is in parachute freefall.
        /// </summary>
        /// <param name="ped">The ped to check.</param>
        /// <returns></returns>
        public static bool IsInParachuteFreefall(this Ped ped)
        {
            return Function.Call<bool>(Hash.IS_PED_IN_PARACHUTE_FREE_FALL, ped.Handle);
        }

        /// <summary>
        /// Set's the ik target for this ped.
        /// </summary>
        /// <param name="ped"></param>
        public static void SetIKTarget(this Ped ped, IKIndex index, Vector3 position, float blendInSpeed, float blendOutSpeed)
        {
            Function.Call(Hash.SET_IK_TARGET, ped.Handle, (int)index, 0, 0, position.X, position.Y, position.Z, 0, blendInSpeed, blendOutSpeed);
        }

        /// <summary>
        /// Make this ped ragdoll for the speicified duration.
        /// </summary>
        /// <param name="ped">The ped.</param>
        /// <param name="duration">The amount of time in ms to ragdoll.</param>
        public static bool SetToRagdoll(this Ped ped, int duration, int type = 0)
        {
            if (Function.Call<bool>(Hash.IS_PED_RAGDOLL, ped.Handle) || ped.IsDead)
                return true;
            Function.Call(Hash.SET_PED_CAN_RAGDOLL, ped.Handle, true);
            Function.Call(Hash.SET_PED_TO_RAGDOLL, ped.Handle, duration, duration, type, 1, 1, 0);
            return false;
        }
    }
}
