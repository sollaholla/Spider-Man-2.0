﻿using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;
using Rope = SpiderMan.Library.Types.Rope;
using GTAGraphics = SpiderMan.Library.Types.GTAGraphics;

namespace SpiderMan.Abilities.SpecialAbilities.PlayerOnly
{
    public class WebSwing : SpecialAbility
    {
        // A timer that helps us cool down the web swing so
        // we can't do it constantly.
        private float _webSwingCooldown;

        /// <summary>
        ///     The main constructor.
        /// </summary>
        public WebSwing(SpiderManProfile profile) : base(profile)
        {
            // Request our animations.
            Streaming.RequestAnimationDictionary("skydive@parachute@");
            Streaming.RequestAnimationDictionary("missrappel");
            Streaming.RequestAnimationDictionary("swimming@swim");
            Streaming.RequestAnimationDictionary("move_fall");
            new Model("bmx").Request();
        }

        /// <summary>
        ///     Called each tick.
        /// </summary>
        public override void Update()
        {
            // Make sure the player is not grounded.
            if (Profile.LocalUser.GetConfigFlag(60))
                return;

            // Disable the parachute deploy control.
            Game.DisableControlThisFrame(2, Control.ParachuteDeploy);
            Game.DisableControlThisFrame(2, Control.Reload);

            if (Profile.LocalUser.HeightAboveGround > 15f && Game.IsDisabledControlJustPressed(2, Control.Reload))
            {
                var entity = Utilities.GetAimedEntity(250f, out var endCoords, out var entType);
                if (endCoords != Vector3.Zero && entType == EntityType.Vehicle && 
                    entity.Position.Z > Profile.LocalUser.Position.Z)
                {
                    var veh = new Vehicle(entity.Handle);
                    if (veh.Model.IsPlane || veh.Model.IsHelicopter || 
                        veh.FriendlyName == "Atomic Blimp")
                    {
                        DoSwing(veh.Position, veh, Control.Reload, false);
                    }
                }
            }

            // Count down the web swing cooldown.
            if (_webSwingCooldown > 0f)
            {
                _webSwingCooldown -= Time.DeltaTime;
                return;
            }

            // Disable the jump control.
            Game.DisableControlThisFrame(2, Control.Attack);

            // Adjust the height based on the player's velocity.
            var height = Math.Min(0, Math.Max(Profile.LocalUser.Velocity.Z, 1.5f));

            // 4 scans per pass at a 75 degree angle.
            var hitPoint = BuildingScan(9, 75f, 1 + height, 150f, 15f);

            // If the hit isn't empty then let's proceed...
            if (hitPoint == Vector3.Zero) return;
            if (!Game.IsDisabledControlPressed(2, Control.Attack) || !(Profile.LocalUser.HeightAboveGround > 15) ||
                Profile.LocalUser.IsGettingUp && !Profile.LocalUser.IsFalling) return;

            // Also create a prop at the hit point.
            var webHitHelper = World.CreateVehicle("bmx", hitPoint);
            // Set the props up for use.
            webHitHelper.HasCollision = false;
            webHitHelper.FreezePosition = true;
            webHitHelper.IsVisible = false;

            DoSwing(hitPoint, webHitHelper, Control.Attack);
            webHitHelper.Delete();
        }

        private void DoSwing(Vector3 hitPoint, Entity webHitHelper, Control webControl, bool checkMinAngle = true)
        {
            // Create a helper prop for rotations.
            var rotationHelper = World.CreateVehicle("bmx", Profile.LocalUser.Position);
            rotationHelper.Alpha = 0;

            // Store the initial velocity.
            var initialVelocity = Profile.LocalUser.Velocity;

            // Clear the player tasks.
            Profile.LocalUser.Task.ClearAll();

            // Attach the player to the helper prop.
            Profile.LocalUser.AttachToEntity(rotationHelper, 0, new Vector3(0, -0.25f, -1.15f),
                Vector3.Zero, false, false, true, 0, true);

            var t = 0.5f;
            while (!Profile.LocalUser.IsAttached() && t > 0)
            {
                t -= Time.UnscaledDeltaTime;
                Script.Yield();
            }

            // We should play the base animation initially for good blending
            Profile.LocalUser.Task.PlayAnimation("move_fall", "fall_high",
                8.0f, -4.0f, -1, AnimationFlags.StayInEndFrame, 0.0f);

            // The rappel anim!
            Profile.LocalUser.Task.PlayAnimation("missrappel", "rappel_idle", 4.0f, -4.0f, -1,
                AnimationFlags.Loop | AnimationFlags.AllowRotation | AnimationFlags.UpperBodyOnly,
                0.0f);

            // Create our rope.
            var initialLength = Vector3.Distance(rotationHelper.Position, hitPoint);
            if (initialLength <= 0)
                initialLength = 1;
            var rope = Rope.AddRope(rotationHelper.Position, initialLength,
                GTARopeType.ThickRope, initialLength, 0.1f, true, false);

            // Attach the two helpers.
            rope.AttachEntities(rotationHelper, Vector3.Zero, webHitHelper, Vector3.Zero, initialLength);
            rope.ActivatePhysics();

            // Init a rotation.
            var initalDirection = Vector3.ProjectOnPlane(Profile.GetCameraDirection(),
                (webHitHelper.Position - rotationHelper.Position).Normalized);

            // Apply an initial force to the rotation helper.
            rotationHelper.Velocity = initalDirection * initialVelocity.Length() * 1.5f;

            // A bool to help us determine if we've played our forward swing
            // animation or not.
            var forwardSwing = false;

            // The amount of momentum we build.
            var momentumBuild = 1f;

            // Here's our loop.
            while (true)
            {
                // Disable the parachute deploy control.
                Game.DisableControlThisFrame(2, Control.ParachuteDeploy);

                // Disable the jump control.
                Game.DisableControlThisFrame(2, webControl);

                if (rotationHelper.HasCollidedWithAnything)
                {
                    var normal = rotationHelper.GetLastCollisionNormal();
                    var dir = Vector3.Reflect(rotationHelper.Velocity, normal);
                    dir.Normalize();
                    Profile.LocalUser.Detach();
                    Profile.LocalUser.Heading = dir.ToHeading();
                    Profile.LocalUser.Velocity = dir * rotationHelper.Velocity.Length() / 2;
                    Profile.LocalUser.Task.Skydive();
                    Profile.LocalUser.Task.PlayAnimation("swimming@swim", "recover_flip_back_to_front",
                        4.0f, -2.0f, 1150, (AnimationFlags)40, 0.0f);
                    Profile.LocalUser.Quaternion.Normalize();
                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(Profile.LocalUser,
                        "DLC_Exec_Office_Non_Player_Footstep_Mute_group_MAP"));
                    GTAGraphics.StartParticle("core", "ent_dst_dust", Profile.LocalUser.Position,
                        Profile.LocalUser.Rotation, 1f);
                    break;
                }

                // If we're near the ground then stop.
                if (Profile.LocalUser.HeightAboveGround <= 2f)
                {
                    // The swing was canceled.
                    break;
                }

                // If we release the attach control then
                // we should start the skydive.
                if (Game.IsDisabledControlJustReleased(2, webControl))
                {
                    ReleaseWebToAir(-1, rotationHelper);
                    break;
                }

                // Set the hand ik positions.
                var ropeCoord = rope[0];
                var boneOffset = Profile.LocalUser.RightVector * 0.05f;
                Profile.LocalUser.SetIKTarget(IKIndex.LeftArm, ropeCoord - boneOffset, -1f, 0f);
                Profile.LocalUser.SetIKTarget(IKIndex.RightArm, ropeCoord + boneOffset, -1f, 0f);

                // Swing the player's legs based on his forward direction.
                var dot = Vector3.Dot(Vector3.WorldUp, Profile.LocalUser.ForwardVector);

                // Swinging down.
                if (dot < 0)
                {
                    // Check our flag.
                    if (!forwardSwing)
                    {
                        // Play the down swinging anim.
                        Profile.LocalUser.Task.PlayAnimation("skydive@parachute@", "chute_idle_alt",
                            1.0f, -4.0f, -1, AnimationFlags.Loop, 0.0f);

                        // Set our flag.
                        forwardSwing = true;
                    }
                }
                // Swinging up.
                else if (dot > 0)
                {
                    // Check our flag.
                    if (forwardSwing)
                    {
                        // Play the up swinging anim.
                        Profile.LocalUser.Task.PlayAnimation("skydive@parachute@", "chute_forward",
                            1.0f, -4.0f, -1, AnimationFlags.Loop, 0.0f);

                        // Set our flag.
                        forwardSwing = false;
                    }
                }

                // Get the desired rotation for our
                // rotation helper.
                var directionToWebHit = webHitHelper.Position - rotationHelper.Position;
                directionToWebHit.Normalize();
                var dirVel = Vector3.ProjectOnPlane(rotationHelper.Velocity, directionToWebHit);
                var rotation = Maths.LookRotation(dirVel, directionToWebHit);
                rotationHelper.Quaternion = rotation;

                // Get the direction of input.
                var moveDir = Profile.GetInputDirection();
                var relativeDir = Quaternion.Euler(Profile.GetCameraRotation()) * moveDir;
                relativeDir = Vector3.ProjectOnPlane(relativeDir, directionToWebHit);

                // Apply force of input.
                if (momentumBuild < 2.5f)
                    momentumBuild += Time.DeltaTime;
                rotationHelper.ApplyForce(relativeDir * momentumBuild * 50f * Time.UnscaledDeltaTime);
                rotationHelper.ApplyForce(Vector3.WorldDown * 15f * Time.UnscaledDeltaTime);

                if (checkMinAngle)
                {
                    var angle = Vector3.Angle(directionToWebHit, Vector3.WorldUp);
                    if (angle > 70)
                    {
                        ReleaseWebToAir(0, rotationHelper);
                        break;
                    }
                }

                // Yield the script.
                Script.Yield();
            }

            // Reset the cooldown!
            _webSwingCooldown = 0.7f;

            Profile.LocalUser.Task.ClearAnimation("skydive@parachute@", "chute_idle_alt");
            Profile.LocalUser.Task.ClearAnimation("skydive@parachute@", "chute_forward");
            Profile.LocalUser.Task.ClearAnimation("missrappel", "rappel_idle");

            // Delete the helper props.
            rotationHelper.Delete();

            // Delete the rope.
            rope.Delete();
        }

        /// <summary>
        ///     Release the web to air.
        /// </summary>
        /// <param name="animType">The type of animation to play, 0 is default.</param>
        /// <param name="rotationHelper"></param>
        private void ReleaseWebToAir(int animType, Entity rotationHelper)
        {
            var velocity = rotationHelper.Velocity;
            Profile.LocalUser.Detach();
            Profile.LocalUser.Task.ClearAll();
            Profile.LocalUser.Task.Skydive();
            Profile.LocalUser.Velocity = velocity;

            switch (animType)
            {
                case 0:
                    Profile.LocalUser.Task.PlayAnimation("swimming@swim", "recover_back_to_idle",
                        2.0f, -2.0f, 1150, AnimationFlags.AllowRotation, 0.0f);
                    break;
            }

            Profile.LocalUser.Quaternion.Normalize();
        }

        /// <summary>
        ///     Called when the script stops.
        /// </summary>
        public override void Stop()
        {
        }

        /// <summary>
        ///     Does a scan from left to right from the player's forward direction.
        /// </summary>
        /// <param name="scanCount">The amount of scans per pass. (2 passes left and right)</param>
        /// <param name="maxAngle">The maximum angle of the scan. From 0-90.</param>
        /// <param name="steepness">The steepness or height of the scan.</param>
        /// <param name="distance">The distance of the scan.</param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        private Vector3 BuildingScan(int scanCount, float maxAngle, float steepness, float distance,
            float minDistance)
        {
            // Make sure to icrease the scan count by 1, since we 
            // skip the first check in each pass.
            scanCount = scanCount + 1;

            // The amount to increment in the loops.
            var scanCounter = maxAngle / scanCount;

            // The vertical steepness.
            var vSteep = Vector3.WorldUp * steepness;

            var hits = new List<Vector3>();

            // Our left scan.
            for (float f = 0; f < maxAngle; f += scanCounter)
            {
                // Skip the first scan.
                if (Math.Abs(f) < 0.0001f)
                    continue;

                var result = GetScanResult(distance, vSteep, f);

                // If the result hit then we need to return the new vector.
                if (result.Hit &&
                    Vector3.Distance(result.EndCoords, Profile.LocalUser.Position) >= minDistance)
                    hits.Add(result.EndCoords);
            }

            // Our right scan.
            for (float f = 0; f < maxAngle; f += scanCounter)
            {
                // Skip the first scan.
                if (Math.Abs(f) < 0.0001f)
                    continue;

                var result = GetScanResult(distance, vSteep, -f);

                // If the result hit then we need to return the new vector.
                if (result.Hit &&
                    Vector3.Distance(result.EndCoords, Profile.LocalUser.Position) >= minDistance)
                    hits.Add(result.EndCoords);
            }

            // Get the closest hit point.
            var tMin = Vector3.Zero;
            var minDist = float.MaxValue;
            var currentPos = Profile.LocalUser.Position;
            foreach (var t in hits)
            {
                var dist = Vector3.Distance(t, currentPos);
                if (!(dist < minDist)) continue;
                tMin = t;
                minDist = dist;
            }

            // Return the closest hit.
            return tMin;
        }

        /// <summary>
        ///     Get's a scan result from the building scan check.
        /// </summary>
        /// <param name="distance">The distance of the scan.</param>
        /// <param name="vSteep">The vertical steepness (up direction) of the scan.</param>
        /// <param name="f">The scan's angle from the player's forward direction.</param>
        /// <returns></returns>
        private ShapeTestResult GetScanResult(float distance, Vector3 vSteep, float f)
        {
            // The rotation of the angle relative to the player.
            var rotation = Quaternion.Euler(0, 0, f);

            // Get the camera direction.
            var cameraDirection = Profile.GetCameraDirection();

            // Project the camera direction onto the world up vector.
            var cameraDirProject = Vector3.ProjectOnPlane(cameraDirection, Vector3.WorldUp);

            // The direction of the ray.
            var direction = rotation * cameraDirProject + vSteep;
            direction.Normalize();

            // Draw a line to help us visualize this.
            //GameGraphics.DrawLine(PlayerCharacter.Position, PlayerCharacter.Position + direction * distance, Color.Red);

            // Start the ray.
            var ray = WorldProbe.StartShapeTestRay(Profile.LocalUser.Position,
                Profile.LocalUser.Position + direction * distance,
                ShapeTestFlags.IntersectMap, Profile.LocalUser);

            // Get the ray's result.
            var result = ray.GetResult();
            return result;
        }
    }
}