using GTA;
using GTA.Math;
using GTA.Native;

namespace spiderman.net.Library.Extensions
{
    public enum EntityType
    {
        None = 0,
        Ped = 1,
        Vehicle = 2,
        Object = 3
    }

    public enum ForceFlags
    {
        WeakForce = 0,
        StrongForce = 1,
        WeakMomentum = 4,
        StrongMomentum = 5
    }

    /// <summary>
    /// An extension class for the entity data type.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Get's whether or not the specified entity is playing the specified animation.
        /// </summary>
        /// <param name="entity">The entity we want to check.</param>
        /// <param name="animationDictionary">The dictionary containing the animation.</param>
        /// <param name="animationName">The name of the animation.</param>
        /// <param name="taskFlag">The task flag. 2 is for synchronized scenes, but it's usually 3.</param>
        /// <returns></returns>
        public static bool IsPlayingAnimation(this Entity entity, string animationDictionary, 
            string animationName, int taskFlag = 3)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, entity.Handle, animationDictionary, 
                animationName, taskFlag);
        }

        /// <summary>
        /// Removes any particles playing on this entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public static void RemovePlayingParticles(this Entity entity)
        {
            Function.Call(Hash.REMOVE_PARTICLE_FX_FROM_ENTITY, entity.Handle);
        }

        /// <summary>
        /// Set the speed of the entities currently playing animation.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="animationDictionary">The dictionary of the animation.</param>
        /// <param name="animationName">The animations name.</param>
        /// <param name="speed">The speed of the animation. 1.0 by default.</param>
        public static void SetAnimationSpeed(this Entity entity, string animationDictionary, 
            string animationName, float speed)
        {
            Function.Call(Hash.SET_ENTITY_ANIM_SPEED, entity.Handle, animationDictionary, animationName, speed);
        }

        /// <summary>
        /// Get's the animations current time as a value from 0.0 - 1.0.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="animationDictionary">The animation dictionary.</param>
        /// <param name="animationName">The animation name.</param>
        /// <returns></returns>
        public static float GetAnimationTime(this Entity entity, string animationDictionary,
            string animationName)
        {
            return Function.Call<float>(Hash.GET_ENTITY_ANIM_CURRENT_TIME, entity.Handle, animationDictionary, 
                animationName);
        }

        /// <summary>
        /// Set's an entities position without clearing the area around the coordinate.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="position">The position.</param>
        public static void SetCoordsSafely(this Entity entity, Vector3 position)
        {
            Function.Call(Hash.SET_ENTITY_COORDS, entity.Handle, position.X, position.Y, position.Z, 0, 0, 0, false);
        }

        /// <summary>
        /// Set's the current time in the animation specified. Value ranges from 0 - 1.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="animationDictionary">The animation dictionary.</param>
        /// <param name="animationName">The animation name.</param>
        /// <param name="time">The animation time from 0 - 1.</param>
        public static void SetAnimationTime(this Entity entity, string animationDictionary,
            string animationName, float time)
        {
            Function.Call(Hash.SET_ENTITY_ANIM_CURRENT_TIME, entity.Handle, animationDictionary, animationName, time);
        }

        /// <summary>
        /// Get's the type of entity.
        /// </summary>
        /// <param name="entity">The entity who's type we wish to get.</param>
        /// <returns></returns>
        public static EntityType GetEntityType(this Entity entity)
        {
            return (EntityType)Function.Call<int>(Hash.GET_ENTITY_TYPE, entity?.Handle ?? 0);
        }

        /// <summary>
        /// Applies force to an entity. Different from APPLY_FORCE_TO_ENTITY_CENTER_OF_MASS.
        /// </summary>
        /// <param name="entity">The entity to apply force to.</param>
        /// <param name="forceFlags">The force flags. 1-7</param>
        /// <param name="force">The amount of force and direction of force to apply to the entity.</param>
        /// <param name="offset">The offset from the entity's centre to apply the force.</param>
        /// <param name="boneIndex">The index of the entity bone that will be used as it's centre.</param>
        /// <param name="isDirectionRelative">True if the offset direction should be relative to the entity.</param>
        /// <param name="ignoreUpVector">If true the Z value of the force would be set to 0.</param>
        /// <param name="isForceRelative">If true the force will be relative to the entity.</param>
        public static void ApplyForce(this Entity entity, ForceFlags forceFlags, Vector3 force, Vector3 offset, int boneIndex, 
            bool isDirectionRelative, bool ignoreUpVector, bool isForceRelative)
        {
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, entity.Handle, (int)forceFlags, 
                force.X, force.Y, force.Z, 
                offset.X, offset.Y, offset.Z, 
                boneIndex, 
                isDirectionRelative, ignoreUpVector, isForceRelative, 0, 0);
        }

        /// <summary>
        /// Returns the last collision normal if <see cref="Entity.HasCollidedWithAnything"/> is true.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Vector3 GetLastCollisionNormal(this Entity entity)
        {
            return Function.Call<Vector3>(Hash.GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY, entity.Handle);
        }

        /// <summary>
        /// Attaches an entity to another entity.
        /// </summary>
        /// <param name="entity1">The entity to attach.</param>
        /// <param name="entity2">The entity who will be the parent.</param>
        /// <param name="boneIndex">The bone index on entity2 to attach entity1 to.</param>
        /// <param name="offset">The offset from the bone coordinate on entity2 at boneIndex.</param>
        /// <param name="rotationOffset">The rotation offset of entity1 when attached.</param>
        /// <param name="softPin">Allows for attachments to break.</param>
        /// <param name="collisions">Enables collisons on entity1.</param>
        /// <param name="isPed">If this is a ped, then set this to true.</param>
        /// <param name="vertexIndex">The vertex index of the bone weights.</param>
        /// <param name="lockRotation">Locking rotation means that entity1 cannot rotate.</param>
        public static void AttachToEntity(this Entity entity1, Entity entity2, int boneIndex, Vector3 offset,
            Vector3 rotationOffset, bool softPin, bool collisions, bool isPed, int vertexIndex, bool lockRotation)
        {
            if (entity1.IsAttached())
                entity1.Detach();

            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, entity1.Handle, entity2.Handle, boneIndex,
                offset.X, offset.Y, offset.Z, rotationOffset.X, rotationOffset.Y, rotationOffset.Z, false, softPin,
                collisions, isPed, vertexIndex, lockRotation);
        }

        /// <summary>
        /// Attaches an entity to another entity physically, allowing for both to have collisions and use ragdolling.
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <param name="boneIndex1"></param>
        /// <param name="boneIndex2"></param>
        /// <param name="offset1"></param>
        /// <param name="offset2"></param>
        /// <param name="rotation"></param>
        /// <param name="breakForce"></param>
        /// <param name="fixedRotation"></param>
        /// <param name="collision"></param>
        /// <param name="tethered"></param>
        public static void AttachToEntityPhysically(this Entity entity1, Entity entity2, int boneIndex1, int boneIndex2,
            Vector3 offset1, Vector3 offset2, Vector3 rotation, float breakForce = -1f, bool fixedRotation = false, 
            bool collision = true, bool tethered = false)
        {
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY_PHYSICALLY, entity1.Handle, entity2.Handle, boneIndex1, boneIndex2, 
                offset1.X, offset1.Y, offset1.Z, 
                offset2.X, offset2.Y, offset2.Z, 
                rotation.X, rotation.Y, rotation.Z,
                breakForce, fixedRotation, true, 
                collision, tethered, 2);
        }
    }
}
