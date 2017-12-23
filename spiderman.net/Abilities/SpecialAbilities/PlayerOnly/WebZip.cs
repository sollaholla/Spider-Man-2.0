using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.SpecialAbilities.PlayerOnly
{
    /// <summary>
    ///     Allows the player to grapple onto vehicles,
    ///     or slingshot himself with a web.
    /// </summary>
    public class WebZip : SpecialAbility
    {
        public delegate void OnCatchLanding(object sender, EventArgs e, float fallHeight);

        /// <summary>
        /// Called once the player lands on the ground.
        /// </summary>
        public static event OnCatchLanding CaughtLanding;

        // A flag to help us know
        // when the player was falling.
        private static bool _falling;

        // Let's us know how high we where 
        // before we started falling.
        private static float _fallHeight;

        // This is to help with rotations
        // while we calculate the arc needed
        // to travel in order to land on a vehicle.
        private Entity _helperObj;

        /// <summary>
        ///     Called in the first tick of the main
        ///     script.
        /// </summary>
        public WebZip(SpiderManProfile profile) : base(profile)
        {
            // Make sure to request rope textures,
            // if they haven't already loaded.
            Rope.LoadTextures();

            // Request our animations.
            Streaming.RequestAnimationDictionary("weapons@projectile@");
            Streaming.RequestAnimationDictionary("move_fall");
            Streaming.RequestAnimationDictionary("swimming@swim");
            Streaming.RequestAnimationDictionary("amb@world_vehicle_police_carbase");
            Streaming.RequestAnimationDictionary("skydive@base");
            Streaming.RequestAnimationDictionary("move_crouch_proto");

            new Model("bmx").Request();
        }

        /// <summary>
        ///     Overrieds our internal fall height to the specified value.
        /// </summary>
        /// <param name="height"></param>
        public static void OverrideFallHeight(float height)
        {
            _fallHeight = height;
            _falling = true;
        }

        /// <summary>
        ///     Our update method.
        /// </summary>
        public override void Update()
        {
            // Disable the player's pain audio.
            Profile.LocalUser.DisablePainAudio(true);

            // Checks the player's fall height.
            CheckFallAndCatchLanding();

            // Draw the reticle for aiming purposes.
            UI.ShowHudComponentThisFrame(HudComponent.Reticle);

            // The max distance of our raycast.
            const float rayMaxDist = 250f;

            // Get the hit point of the raycast.
            var entity = Utilities.GetAimedEntity(rayMaxDist, out var hitPoint, out var entityType);

            // Now do the corresponding logical operation.
            switch (entityType)
            {
                case EntityType.None:
                    WorldGrapple(hitPoint);
                    break;
                case EntityType.Vehicle:
                    VehicleGrapple((Vehicle)entity);
                    break;
            }
        }

        /// <summary>
        ///     Catch our landing so we don't die from high falls.
        /// </summary>
        private bool CheckFallAndCatchLanding(Rope rope = null)
        {
            // If the player is falling then...
            if (Profile.LocalUser.IsFalling)
            {
                // Check the falling flag.
                if (!_falling)
                    _fallHeight = Profile.LocalUser.HeightAboveGround;

                // Set the flag regardless. We're just 
                // using it as a trigger of sorts.
                _falling = true;
            }
            else
            {
                // Like before, set it regardless.
                _falling = false;

                // Set the input vector before we start falling.
            }

            // Catch our landing at high falls.
            bool didCollide;
            if (!(didCollide = Profile.LocalUser.GetConfigFlag(60)) && !(Profile.LocalUser.HeightAboveGround < 2f)) return false;

            // Check the fall height, catch the landing,
            // and reset the fall height.
            const float maxFallHeight = 5f;
            if (_fallHeight > maxFallHeight)
            {
                if (!didCollide)
                {
                    var vel = Profile.LocalUser.Velocity;
                    Profile.LocalUser.SetCoordsSafely(Profile.LocalUser.Position - Vector3.WorldUp * Profile.LocalUser.HeightAboveGround);
                    Profile.LocalUser.Velocity = vel;
                }
                // Try to catch our landing.
                rope?.Delete();
                CatchLanding();
                CaughtLanding?.Invoke(this, new EventArgs(), _fallHeight);
                _fallHeight = 0f;
            }
            else
            {
                // If we should splat then let us, so that
                // the falling anim isn't constantly playing.
                Profile.LocalUser.Task.ClearAnimation("move_fall", "fall_high");
            }

            // This will return true since we landed.
            return true;

            // Nothing happened so we return false.
        }

        /// <summary>
        ///     Grapples the player towards a point on the map.
        /// </summary>
        private void WorldGrapple(Vector3 targetPoint)
        {
            // Make sure that this point is not 
            // empty just for safety.
            if (targetPoint == Vector3.Zero)
                return;

            // Disable the reload control for now.
            Game.DisableControlThisFrame(2, Control.Reload);

            // Now once we press the reload key, we want 
            // to grapple the player (also make sure
            // he's not on the ground already).
            if (Game.IsDisabledControlJustPressed(2, Control.Reload))
            {
                // Get the direction from the player to the point.
                var directionToPoint = targetPoint - Profile.LocalUser.Position;

                // If we're on the ground then move us upwards.
                if (Profile.LocalUser.GetConfigFlag(60))
                    directionToPoint += Vector3.WorldUp * 0.5f;
                directionToPoint.Normalize(); // Normalize the direcion vector.

                // Set the player's heading accordingly.
                Profile.LocalUser.Heading = directionToPoint.ToHeading();

                var speed = Vector3.Distance(Profile.LocalUser.Position, targetPoint);
                speed = Maths.Clamp(speed, 65f, 150f) * Profile.WebZipForceMultiplier;

                // Play the falling animation.
                // Reset the player's velocity if anything is left over.
                Profile.LocalUser.Task.ClearAllImmediately();
                Profile.LocalUser.Velocity = Vector3.Zero;
                Profile.LocalUser.Task.Jump();

                // Initialize our rope variable.
                Rope rope = null;

                var isOnGround = Profile.LocalUser.GetConfigFlag(60);
                var timer = 0.025f;

                if (isOnGround)
                {
                    // Wait until the player is no longer on the ground.
                    while (timer > 0f)
                    {
                        Profile.LocalUser.Velocity += Vector3.WorldUp * 500f * Time.DeltaTime;
                        timer -= Time.DeltaTime;
                        Script.Yield();
                    }
                    GameWaiter.Wait(150);
                }

                // Now we need to set player's velocity.
                Profile.LocalUser.Velocity = directionToPoint * speed;

                // Make sure we've left the ground.
                if (!Profile.LocalUser.GetConfigFlag(60))
                {
                    // Play the web grapple animation.
                    Profile.LocalUser.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand",
                        8.0f, -4.0f, 250, AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 0.0f);

                    timer = 0.5f;
                    while (!Profile.LocalUser.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand")
                        && timer > 0f)
                    {
                        timer -= Time.DeltaTime;
                        Script.Yield();
                    }
                    Profile.LocalUser.SetAnimationSpeed("weapons@projectile@", "throw_m_fb_stand", -1f);
                }

                timer = 0.7f;
                while (timer > 0f)
                {
                    if (Profile.LocalUser.HasCollidedWithAnything)
                        break;

                    if (CheckFallAndCatchLanding(rope))
                        break;

                    // Cache the players right hand coord.
                    var rHand = Profile.LocalUser.GetBoneCoord(Bone.SKEL_R_Hand);

                    // Create the rope.
                    if (rope == null)
                    {
                        // Get the inital distance to the target.
                        var initialDist = rHand.DistanceTo(targetPoint);
                        rope = Rope.AddRope(rHand, initialDist, GTARopeType.ThickRope, initialDist / 2, 0.1f, true,
                            false);
                    }

                    // Check if the player is playing the grapple animation.
                    if (Profile.LocalUser.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand"))
                    {
                        // Pin the rope vertices.
                        rope.PinVertex(0, rHand);
                        rope.PinVertex(rope.VertexCount - 1, targetPoint);

                        // Reverse the grapple animation.
                        Profile.LocalUser.SetAnimationSpeed("weapons@projectile@", "throw_m_fb_stand", -1f);
                    }
                    else
                    {
                        // Otherwise delete the rope.
                        rope.UnpinVertex(0);
                        rope.PinVertex(rope.VertexCount - 1, targetPoint);
                    }
                    timer -= Time.DeltaTime;
                    Script.Yield();
                }

                // Clear the throwing anim.
                Profile.LocalUser.Task.ClearAnimation("weapons@projectile@", "throw_m_fb_stand");

                // Destroy the rope here.
                if (Rope.Exists(rope))
                    rope?.Delete();

                //OverrideFallHeight(float.MaxValue);
            }
        }

        /// <summary>
        ///     If you call this as soon as the player
        ///     hit's the ground, it will recover him with a roll (or
        ///     if not moving just a plain landing animation).
        /// </summary>
        private void CatchLanding()
        {
            // Clear the player's tasks.
            Profile.LocalUser.ClearTasksImmediately();

            // Check our movement vector.
            var moveVector = Profile.GetInputDirection();

            // Play the movement dependant animations.
            if (moveVector.Length() > 0)
            {
                // Add a little bit of dust.
                GTAGraphics.StartParticle("core", "ent_dst_dust", Profile.LocalUser.Position - Vector3.WorldUp,
                    Vector3.Zero, 2f);

                // Play the rolling animation.
                Profile.LocalUser.Task.PlayAnimation("move_fall", "land_roll",
                    8.0f, -4.0f, 750, AnimationFlags.AllowRotation, 0.0f);
            }
            else
            {
                // Add a little bit of dust.
                GTAGraphics.StartParticle("core", "ent_dst_dust", Profile.LocalUser.Position - Vector3.WorldUp,
                    Vector3.Zero, 2f);

                // Play our super hero landing animation.
                Profile.LocalUser.Velocity = Vector3.Zero;
                Profile.LocalUser.Task.PlayAnimation("move_crouch_proto", "idle_intro", 6.0f, -4.0f, 450,
                    AnimationFlags.Loop, 0.0f);
                Profile.LocalUser.Task.PlayAnimation("skydive@base", "ragdoll_to_free_idle", 6.0f, -4.0f, 450,
                    AnimationFlags.AllowRotation |
                    AnimationFlags.UpperBodyOnly,
                    0.0f);
                var timer = 0.5f;
                while (!Profile.LocalUser.IsPlayingAnimation("skydive@base", "ragdoll_to_free_idle") &&
                    timer > 0f)
                {
                    timer -= Time.DeltaTime;
                    Script.Yield();
                }
                Profile.LocalUser.SetAnimationSpeed("skydive@base", "ragdoll_to_free_idle", 0f);
            }

            // Clear the falling animation.
            Profile.LocalUser.Task.ClearAnimation("move_fall", "fall_high");
            GameWaiter.Wait(500);
        }

        /// <summary>
        ///     Grapple onto the rooftops of moving / stationary land vehicles.
        /// </summary>
        private void VehicleGrapple(Vehicle vehicle)
        {
            if (vehicle == null)
                return;

            // Make sure we got a vehicle returned to us.
            if (vehicle.ClassType == VehicleClass.Helicopters || vehicle.ClassType == VehicleClass.Planes) return;

            // Disable the vehicle enter button.
            Game.DisableControlThisFrame(2, Control.Reload);

            // Check if the player has pressed the right button.
            if (Game.IsDisabledControlJustPressed(2, Control.Reload))
            {
                /////////////////////////////////////////////////////////////
                // Now we're going to launch the player towards the vehicle!
                /////////////////////////////////////////////////////////////

                // Clear the player's tasks completely, first.
                Profile.LocalUser.Task.ClearSecondary();
                Profile.LocalUser.ClearTasksImmediately();

                // A flag that let's the loop know if we've done a long distance jump.
                var twoHandedAnim = false;

                // Now we need to player our cool animations.
                if (Vector2.Distance(new Vector2(Profile.LocalUser.Position.X, Profile.LocalUser.Position.Y),
                        new Vector2(vehicle.Position.X, vehicle.Position.Y)) > 15f)
                {
                    // Add a little RNG for the animations.
                    // For one we'll do the forward dive recover, and 
                    // the other will be the backwards paddle.
                    var random = new Random();
                    var animType = random.Next(2);

                    // The first anim (back paddle).
                    if (animType == 1)
                    {
                        Profile.LocalUser.Task.PlayAnimation("swimming@swim", "recover_back_to_idle",
                            4.0f, -2.0f, 1150, AnimationFlags.StayInEndFrame, 0.0f);

                        // Our two handed grapple anim.
                        Profile.LocalUser.Task.PlayAnimation("amb@world_vehicle_police_carbase", "base",
                            8.0f, -8.0f, 150, AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 0.0f);
                        twoHandedAnim = true;
                    }
                    // Now the second anim.
                    else
                    {
                        Profile.LocalUser.Task.PlayAnimation("swimming@swim", "recover_flip_back_to_front",
                            4.0f, -2.0f, 1150, AnimationFlags.StayInEndFrame, 0.1f);

                        Profile.LocalUser.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand",
                            8.0f, -4.0f, 100, AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 0.0f);
                    }
                }
                // If it's not a long distance jump...
                else
                {
                    // Play the short distance jump animation combo.
                    Profile.LocalUser.Task.PlayAnimation("move_fall", "fall_med",
                        4.0f, -2.0f, 1150, AnimationFlags.StayInEndFrame, 0.0f);
                    Profile.LocalUser.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand",
                        8.0f, -4.0f, 250, AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 0.0f);
                }

                // Get the target direction.
                var targetDirection = vehicle.Position - Profile.LocalUser.Position;
                targetDirection.Normalize();

                // Setup our loop timer.
                var elapsedTime = 0f;

                // Create the helper obj.
                _helperObj = World.CreateVehicle("bmx", Profile.LocalUser.Position +
                                                        new Vector3(0, 100, 0));
                _helperObj.HasCollision = false;
                _helperObj.IsVisible = false;
                _helperObj.FreezePosition = true;
                _helperObj.Heading = targetDirection.ToHeading();

                // The initial direction to the target used as an offset
                // for our heading.
                var initialTargetDirection = vehicle.Position - Profile.LocalUser.Position;
                initialTargetDirection.Normalize();

                // The collection of rops we will use as webs.
                var ropes = new List<Rope>();

                // The delay that we use to delete the ropes.
                var ropeDeleteDelay = 0.75f;

                // Let's do our loop.
                while (GrappleToVehicle(vehicle, twoHandedAnim,
                    elapsedTime, initialTargetDirection, ropes, ref ropeDeleteDelay))
                {
                    Script.Yield();
                }

                // Despawn the ropes if for some reason they 
                // haven't.
                DespawnRopes(ropes);

                // Delete the helper object.
                _helperObj?.Delete();

                // Now we need to clear the player's tasks.
                Profile.LocalUser.Task.ClearAll();
            }
        }

        /// <summary>
        /// Grapple the player towards the specified vehicle.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="twoHandedAnim"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="initialTargetDirection"></param>
        /// <param name="ropes"></param>
        /// <param name="ropeDeleteDelay"></param>
        /// <returns></returns>
        private bool GrappleToVehicle(Vehicle vehicle, bool twoHandedAnim, float elapsedTime,
            Vector3 initialTargetDirection, List<Rope> ropes, ref float ropeDeleteDelay)
        {
            // Two handed web logic...
            ropeDeleteDelay = twoHandedAnim
                ? UpdateRopes1(vehicle, ropes, ropeDeleteDelay)
                : UpdateRopes2(vehicle, ropes, ropeDeleteDelay);

            // Get the target direction direction.
            var targetDirection = vehicle.Position - Profile.LocalUser.Position;
            targetDirection.Normalize();

            // Get the distance while we're at it.
            var targetDistance = Vector3.Distance(Profile.LocalUser.Position, vehicle.Position);

            // The angle of the arc.
            const float angle = 37f;

            // A degree to radians conversion.
            const float degToRad = 0.0174533f;

            // The amount of gravity for the jump.
            const float gravity = 50;

            // Set the player rotation.
            var targetRotation =
                Quaternion.FromToRotation(_helperObj.Quaternion * Vector3.RelativeFront, targetDirection) *
                _helperObj.Quaternion;
            _helperObj.Quaternion = targetRotation;

            // Calculate the speed needed to go from A to B.
            var targetVelocityMag = targetDistance / ((float)Math.Sin(2f * angle * degToRad) / gravity);

            // Extract the x and z of the velocity.
            var acceleration = GetAcceleration(elapsedTime, angle, degToRad, gravity, targetVelocityMag);

            // Set the players velocity and heading.
            Profile.LocalUser.Heading = (initialTargetDirection * 5).ToHeading();
            Profile.LocalUser.Velocity = acceleration * 25f + vehicle.Velocity;

            // reverse the grapple animation.
            if (Profile.LocalUser.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand"))
                Profile.LocalUser.SetAnimationSpeed("weapons@projectile@",
                    "throw_m_fb_stand", -1.0f); // set it to -1x the speed.

            // Check if the player has collided with anything,
            // and if so, then break the loop.
            if (Profile.LocalUser.HasCollidedWithAnything)
            {
                // Stop the grapple.
                StopGrapple();

                // If the player is touching the vehicle...
                if (!Profile.LocalUser.IsTouching(vehicle)) return false;

                // Get the collision normal and check to see
                // if we're on a flat surface.
                var collisionNormal = vehicle.GetLastCollisionNormal();
                if (!(Vector3.Dot(collisionNormal, Vector3.WorldUp) > 0.5f)) return false;

                // Now we're going to artificially attach the player
                // to the roof, with a nice little animation.
                Profile.LocalUser.Velocity = vehicle.Velocity;
                vehicle.SetDamage(Profile.LocalUser.Position - vehicle.Position, 3000f, 200f);
                Profile.LocalUser.Task.PlayAnimation("move_fall", "clamber_land_stand",
                    8.0f, -4.0f, 750, AnimationFlags.None, 0.0f);
                GameplayCamera.Shake(CameraShake.Jolt, 0.1f);
                OverrideFallHeight(0f);
                return false;
            }

            // Make sure the player doesn't think he's falling while we update.
            Profile.LocalUser.SetConfigFlag(60, true);
            return true;
        }

        /// <summary>
        /// Updates a 1 handed rope.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="ropes"></param>
        /// <param name="ropeDeleteDelay"></param>
        /// <returns></returns>
        private float UpdateRopes1(ISpatial vehicle, IList<Rope> ropes, float ropeDeleteDelay)
        {
            // Stop the upper body animation after some time, and move to
            // a falling animation.
            if (Profile.LocalUser.IsPlayingAnimation("swimming@swim", "recover_back_to_idle"))
                if (Profile.LocalUser.GetAnimationTime("swimming@swim", "recover_back_to_idle") > 0.2f)
                    Profile.LocalUser.Task.PlayAnimation("move_fall", "fall_med",
                        2.0f, -4.0f, -1, AnimationFlags.StayInEndFrame, 0.0f);

            // Get the hand coordinates.
            var lHand = Profile.LocalUser.GetBoneCoord(Bone.SKEL_L_Hand);
            var rHand = Profile.LocalUser.GetBoneCoord(Bone.SKEL_R_Hand);

            // Check if this player is playing the web pull animation.
            if (!Profile.LocalUser.IsPlayingAnimation("amb@world_vehicle_police_carbase", "base"))
            {
                // If the ropes are ready to be deleted...
                if (ropeDeleteDelay <= 0)
                {
                    // We need to despawn the dual webs.
                    DespawnRopes(ropes);
                }
                // Otherwise...
                else
                {
                    // Make sure we decrease the rope delay.
                    ropeDeleteDelay -= Time.DeltaTime;

                    // Unpin the vertex coordinates from
                    // our hands.
                    if (ropes.Count > 0)
                    {
                        ropes[0].UnpinVertex(0);
                        ropes[1].UnpinVertex(0);

                        // Also pin the vertices to the target
                        // until we're done here.
                        ropes[0].PinVertex(ropes[0].VertexCount - 1, vehicle.Position);
                        ropes[1].PinVertex(ropes[1].VertexCount - 1, vehicle.Position);
                    }
                }
            }
            // Otherwise we need to spawn them, 
            // and update them.
            else
            {
                // Spawn the ropes first.
                if (ropes.Count == 0)
                {
                    // Create the left and right hand ropes for two hand web jump.
                    var lHandDistance = Vector3.Distance(lHand, vehicle.Position);
                    var rHandDistance = Vector3.Distance(rHand, vehicle.Position);
                    var rope1 = Rope.AddRope(lHand, lHandDistance, GTARopeType.ThickRope,
                        lHandDistance / 2, 0.1f, true, false);
                    var rope2 = Rope.AddRope(rHand, rHandDistance, GTARopeType.ThickRope,
                        rHandDistance / 2, 0.1f, true, false);

                    // Add the ropes to the rope collection.
                    ropes.Add(rope1);
                    ropes.Add(rope2);
                }
                // Now we need to update the vertex coordinates.
                else
                {
                    // Pin the vertices.
                    ropes[0].PinVertex(0, lHand);
                    ropes[0].PinVertex(ropes[0].VertexCount - 1, vehicle.Position);
                    ropes[1].PinVertex(0, rHand);
                    ropes[1].PinVertex(ropes[1].VertexCount - 1, vehicle.Position);
                }
            }

            return ropeDeleteDelay;
        }

        /// <summary>
        /// Update's a 2 handed rope.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="ropes"></param>
        /// <param name="ropeDeleteDelay"></param>
        /// <returns></returns>
        private float UpdateRopes2(ISpatial vehicle, IList<Rope> ropes, float ropeDeleteDelay)
        {
            // Let's update the swimming anim for this if any.
            if (Profile.LocalUser.IsPlayingAnimation("swimming@swim", "recover_flip_back_to_front"))
                if (Profile.LocalUser.GetAnimationTime("swimming@swim", "recover_flip_back_to_front") > 0.25f)
                    Profile.LocalUser.Task.PlayAnimation("move_fall", "fall_med",
                        2.0f, -4.0f, -1, AnimationFlags.StayInEndFrame, 0.0f);

            // Get the right hand coord.
            var rHand = Profile.LocalUser.GetBoneCoord(Bone.SKEL_R_Hand);

            // Check if the player is playing the web pull anim on
            // the upper body.
            if (!Profile.LocalUser.IsPlayingAnimation("weapons@projectile@", "throw_m_fb_stand"))
            {
                if (ropeDeleteDelay <= 0)
                {
                    // Despawn the ropes at this point.
                    DespawnRopes(ropes);
                }
                else
                {
                    // Count down the rope delete delay.
                    ropeDeleteDelay -= Time.DeltaTime;

                    // If we've spawned the ropes...
                    if (ropes.Count > 0)
                    {
                        // Pin the vertices accordingly.
                        ropes[0].UnpinVertex(0); // Unpin the one from our hand.
                        ropes[0].PinVertex(ropes[0].VertexCount - 1, vehicle.Position);
                    }
                }
            }
            // The animation's not playing so let's
            // add the ropes in.
            else
            {
                // Make sure there are no ropes.
                if (ropes.Count <= 0)
                {
                    // The distance to the target
                    // will be used as the rope length.
                    var dist = Vector3.Distance(rHand, vehicle.Position);
                    var rope = Rope.AddRope(rHand, dist, GTARopeType.ThickRope, dist / 2, 0.1f, true, false);
                    ropes.Add(rope);
                }
                // The rope is there.
                else
                {
                    // Now let's pin the vertex coords.
                    ropes[0].PinVertex(0, rHand);
                    ropes[0].PinVertex(ropes[0].VertexCount - 1, vehicle.Position);
                }
            }

            return ropeDeleteDelay;
        }

        /// <summary>
        /// Get the acceleration arc needed to reach the target vehicle.
        /// </summary>
        /// <param name="elapsedTime"></param>
        /// <param name="angle"></param>
        /// <param name="degToRad"></param>
        /// <param name="gravity"></param>
        /// <param name="targetVelocityMag"></param>
        /// <returns></returns>
        private Vector3 GetAcceleration(float elapsedTime, float angle, float degToRad, float gravity, float targetVelocityMag)
        {
            var vx = (float)Math.Sqrt(targetVelocityMag) * (float)Math.Cos(angle * degToRad);
            var vy = (float)Math.Sqrt(targetVelocityMag) * (float)Math.Sin(angle * degToRad);

            // Get the rotation to the target.
            var rotation = _helperObj.Quaternion;

            // Calculate the velocity.
            var yVel = (vx - gravity * elapsedTime * 5) * Time.UnscaledDeltaTime;
            var zVel = vy * Time.UnscaledDeltaTime;
            var acceleration = rotation * new Vector3(0, yVel, zVel);
            acceleration.Normalize();
            return acceleration;
        }

        /// <summary>
        ///     Stops the player grapple.
        /// </summary>
        private void StopGrapple()
        {
            Profile.LocalUser.Task.ClearAll();
            Profile.LocalUser.SetConfigFlag(60, false); // Not grounded.
        }

        /// <summary>
        ///     Despawns the specified ropes.
        /// </summary>
        /// <param name="ropes">The ropes to despawn.</param>
        private static void DespawnRopes(IEnumerable<Rope> ropes)
        {
            foreach (var rope in ropes)
                if (rope != null && rope.Exists())
                    rope.Delete();
        }

        /// <summary>
        ///     Here's where the script stops.
        /// </summary>
        public override void Stop()
        {
            // Delete the helper object.
            _helperObj?.Delete();

            // Re-enable player pain audio.
            Profile.LocalUser.DisablePainAudio(false);

            // Clear our anim once we stop
            Profile.LocalUser.Task.ClearAnimation("move_fall", "fall_high");
        }
    }
}