using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Library.Extensions;

namespace SpiderMan.Library.Modding
{
    /// <summary>
    ///     A class that holds functions that utilizes other library functions.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        ///     Shoot's a ray from the camera position in the
        ///     direction of the camera, for the specified distance.
        /// </summary>
        /// <param name="distance">The shape test distance.</param>
        /// <param name="flags">The shapetest flags.</param>
        /// <param name="ignoreEntity">The entity to be ignored by the shapetest ray.</param>
        /// <returns></returns>
        public static ShapeTestResult GetCameraRay(float distance, ShapeTestFlags flags, Entity ignoreEntity = null)
        {
            var gameCamPos = GameplayCamera.Position;
            var dir = GameplayCamera.Direction * distance;
            var ray = WorldProbe.StartShapeTestRay(gameCamPos, gameCamPos + dir, flags, ignoreEntity);
            var result = ray.GetResult();
            return result;
        }

        /// <summary>
        ///     Shoot's a ray from the player position in the
        ///     direction of the player, for the specified distance.
        /// </summary>
        /// <param name="distance">The shape test distance.</param>
        /// <param name="flags">The shapetest flags.</param>
        /// <param name="ignoreEntity">The entity to be ignored by the shapetest ray.</param>
        /// <returns></returns>
        public static ShapeTestResult GetPlayerRay(float distance, ShapeTestFlags flags, Entity ignoreEntity = null)
        {
            var player = Game.Player.Character;
            var gameCamPos = player.Position;
            var dir = player.ForwardVector * distance;
            var ray = WorldProbe.StartShapeTestRay(gameCamPos, gameCamPos + dir, flags, ignoreEntity);
            var result = ray.GetResult();
            return result;
        }

        /// <summary>
        ///     Play's an animation that aims the player's hand at a position.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="target"></param>
        public static void PlayAimAnim(this Ped ped, Vector3 target)
        {
            // Clear the player tasks.
            ped.Task.ClearAllImmediately();

            // Get the direction to the entity.
            var directionToEntity = target - ped.Position;

            // Set the player's heading towards the target.
            ped.Heading = directionToEntity.ToHeading();

            // Play the web shoot animation.
            ped.Task.PlayAnimation("guard_reactions", "1hand_aiming_to_idle", 8.0f, -8.0f, 500,
                AnimationFlags.AllowRotation, 0.0f);
        }

        /// <summary>
        ///     Play's an animation that aims the player's hand at an entity.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="entity"></param>
        public static void PlayAimAnim(this Ped ped, Entity entity)
        {
            ped.PlayAimAnim(entity.Position);
        }

        public static void PlayGrappleAnim(this Ped ped, float speed)
        {
            // Play the web grapple animation.
            ped.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand",
                8.0f, -4.0f, 250, AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 0.0f);
            var timer = 0.25f;
            while (!ped.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand")
                   && timer > 0f)
            {
                timer -= Time.UnscaledDeltaTime;
                Script.Yield();
            }
            ped.SetAnimationSpeed("weapons@projectile@", "throw_m_fb_stand", speed);
        }

        /// <summary>
        ///     Electricute a ped.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="damage"></param>
        public static void ShockPed(Ped ped, int damage)
        {
            var pedPos = ped.Position;
            var playerPos = Game.Player.Character.Position;
            var dir = playerPos - pedPos;
            dir.Normalize();
            var pos1 = pedPos + dir;
            Function.Call(Hash._0xE3A7742E0B7A2F8B,
                pos1.X, pos1.Y, pos1.Z,
                pedPos.X, ped.Position.Y, ped.Position.Z, damage, true, 0x3656C8C1, Game.Player.Character, false, true,
                500000f, Game.Player.Character);
        }

        /// <
        ///     summary>
        ///     Draws a marker at the specified position.
        /// </summary>
        /// <param name="position">The position.</param>
        public static void DrawSpriteAt(Vector3 position,
            string textureDict, string textureName, Size size, Color color)
        {
            Streaming.RequestTextureDictionary(textureDict);
            var timer = 0.25f;
            while (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, textureDict) && timer > 0f)
            {
                timer -= Time.UnscaledDeltaTime;
                Script.Yield();
            }
            var res = Game.ScreenResolution;
            var width = (float) size.Width / res.Width;
            var height = (float) size.Height / res.Height;

            Function.Call(Hash.SET_DRAW_ORIGIN, position.X, position.Y, position.Z, 0);
            Function.Call(Hash.DRAW_SPRITE, textureDict, textureName, 0.0f, 0.0f,
                width, height, 0, color.R, color.G, color.B, color.A);
            Function.Call(Hash.CLEAR_DRAW_ORIGIN);
        }

        /// <summary>
        ///     Create's a particle chain from point a to point b.
        /// </summary>
        /// <param name="pointA">The starting position.</param>
        /// <param name="pointB">The ending position.</param>
        /// <param name="rotationOffset">The offset of the particles rotation.</param>
        /// <param name="nodeSize">The node size (the distance*2 between each particle)</param>
        /// <param name="noiseMultiplier">The amount of noise to apply to the chain (0-1)</param>
        /// <param name="particleAsset">The asset name of the particle.</param>
        /// <param name="particleName">The name of the particle.</param>
        public static void CreateParticleChain(Vector3 pointA, Vector3 pointB, Vector3 rotationOffset,
            float nodeSize = 1f, float noiseMultiplier = 0.5f,
            string particleAsset = "core", string particleName = "ent_ray_prologue_elec_crackle_sp",
            float particleScale = 0.5f)
        {
            // Clamp the noise.
            noiseMultiplier = Maths.Clamp01(noiseMultiplier);

            // Get the direction to point b.
            var pointBDirection = pointB - pointA;
            pointBDirection.Normalize();

            // Get the distance between points.
            var totalDistance = Vector3.Distance(pointA, pointB);

            // Estimate the amount of nodes we need to use.
            var count = (int) (totalDistance / nodeSize);

            // Initialize variables.
            var currentPosition = pointA;
            var currentRotation =
                Quaternion.FromToRotation(Vector3.WorldUp, pointBDirection); // Calculate the initial rotation.
            var random = new Random();

            // Now we need to loop through each particle.
            for (var i = 0; i < count; i++)
            {
                // Calculate random direction...
                var randomDirection = new Vector2(
                    (float) random.RandomNumberBetween(-1, 1) * noiseMultiplier,
                    (float) random.RandomNumberBetween(-1, 1) * noiseMultiplier);
                randomDirection.Normalize();

                // Get the actual direction we need to move in.
                pointBDirection = currentPosition - pointB;
                pointBDirection.Normalize(); // Normalize this vector.

                // Calculate the rotation to point b.
                var newRotation = Quaternion.FromToRotation(Vector3.RelativeTop, pointBDirection);

                // Calculate the distance.
                var currentDistance = Vector3.Distance(currentPosition, pointB);
                var distancePercentage = currentDistance / totalDistance;

                // Calculate the amount of deviation from the regular direction.
                var dX = randomDirection.X * distancePercentage;
                var dY = randomDirection.Y * distancePercentage;
                var deviation = new Vector3(dX, dY, 1);

                // Get the direction to travel.
                var directionDelta = newRotation * -deviation;
                directionDelta.Normalize();

                // Add that to the current position.
                var nextPosition = currentPosition + directionDelta * nodeSize;
                var currentToNextDirection = nextPosition - currentPosition;
                currentToNextDirection.Normalize();

                // Get the desired rotation from current to next position.
                var desiredRotation = Quaternion.FromToRotation(Vector3.RelativeFront, currentToNextDirection) *
                                      Quaternion.Euler(rotationOffset);

                // Spawn the particle.
                GTAGraphics.StartParticle(particleAsset, particleName, currentPosition,
                    Vector3.Zero, particleScale, true, true, true);

                // Set the current position to the new position.
                currentPosition = nextPosition;
            }
        }

        public static void DestroyAllCameras()
        {
            Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, 250, false, false);
            Function.Call(Hash.DESTROY_ALL_CAMS, true);
        }

        /// <summary>
        ///     Finds a thing / point that the player is aimining at.
        ///     Returns null if nothing was found.
        /// </summary>
        /// <param name="rayMaxDist">The max distance of the raycast.</param>
        /// <param name="endCoords">The end coordinates if any.</param>
        /// <param name="entityType">The entity type.</param>
        /// <returns></returns>
        public static Entity GetAimedEntity(float rayMaxDist, out Vector3 endCoords, out EntityType entityType)
        {
            // Get the gameplay camera direction, and position
            // and shoot a raycast out to get the position that was hit.
            var gameCameraPos = GameplayCamera.Position;
            var distToPlayer = Vector3.Distance(gameCameraPos, Game.Player.Character.Position) + 0.5f;
            var gameCameraDir = GameplayCamera.Direction;

            // This is the end point of the ray.
            var rayEndPos = gameCameraPos + gameCameraDir * rayMaxDist;

            // Shoot the ray and get it's result, ignoring the player.
            var ray = WorldProbe.StartShapeTestRay(gameCameraPos + gameCameraDir * distToPlayer, rayEndPos,
                ShapeTestFlags.Everything, Game.Player.Character);
            var result = ray.GetResult(); // get the result.

            // Make sure the result hit something we want before performing any
            // other operations.
            if (result.Hit)
            {
                // Return the hit point.
                endCoords = result.EndCoords;

                // Also return the entity type.
                entityType = result.EntityType;

                // Return the new vehicle.
                return result.EntityHit;
            }

            // Make sure that the point is empty.
            endCoords = Vector3.Zero;

            // Make sure the entity type is none.
            entityType = EntityType.None;

            // Return null if nothing was found.
            return null;
        }

        /// <summary>
        ///     Get's the closest entity in the specified collection.
        /// </summary>
        /// <param name="otherEntities">The entities to check.</param>
        /// <returns></returns>
        public static Entity GetClosestEntity(Entity sourceEntity, List<Entity> otherEntities)
        {
            Entity tMin = null;
            var minDist = float.PositiveInfinity;
            var currentPos = sourceEntity.Position;
            for (var i = 0; i < otherEntities.Count; i++)
            {
                var t = otherEntities[i];
                var dist = Vector3.Distance(t.Position, currentPos);
                if (dist < minDist)
                {
                    tMin = t;
                    minDist = dist;
                }
            }

            return tMin;
        }
    }
}