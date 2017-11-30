using System;
using spiderman.net.Library;
using GTA;
using System.Collections.Generic;
using GTA.Math;
using spiderman.net.Library.Extensions;
using spiderman.net.Library.Memory;
using System.Drawing;
using GTA.Native;
using spiderman.net.Scripts;

namespace spiderman.net.Abilities
{
    /// <summary>
    /// The melee ability is for having devistating attacks. Applying massive damage
    /// to anything the player hits.
    /// </summary>
    public class Melee : SpecialAbility
    {
        public delegate void OnDamagedEntity(object sender, EventArgs e, Entity entity, Vector3 hitCoords);

        /// <summary>
        /// Called when we melee attack any entity, after our punch landed.
        /// </summary>
        public static event OnDamagedEntity DamagedEntity;

        /// <summary>
        /// We'll use this to cache the attack direction
        /// for smoothing.
        /// </summary>
        private Vector3 _lastAttackDirection;

        /// <summary>
        /// The index of the current combo move.
        /// </summary>
        private int _comboIndex = 0;

        /// <summary>
        /// This is our cooldown timer for the combo moves.
        /// </summary>
        private float _comboCooldown;

        /// <summary>
        /// Helps us track what vehicle we've picked up.
        /// </summary>
        private Vehicle _currentPickupVehicle;

        /// <summary>
        /// The main constructor.
        /// </summary>
        public Melee()
        {
            Streaming.RequestAnimationDictionary("melee@unarmed@streamed_core");
            Streaming.RequestAnimationDictionary("anim@mp_snowball");
            Streaming.RequestAnimationDictionary("random@arrests");
            Streaming.RequestAnimationDictionary("swimming@swim");
            Streaming.RequestAnimationDictionary("weapons@projectile@");
            PlayerCharacter.BlockPermanentEvents = true;
        }

        /// <summary>
        /// Our update method.
        /// </summary>
        public override void Update()
        {
            // Here's where we're going to disable 
            // the attack / melee controls.
            DisableControls();

            // This is our attack abilities.
            Attack();

            // Here's where we're going to
            // add the ability to lift vehicles.
            LiftCars();
        }

        /// <summary>
        /// Add the ability to lift vehicles.
        /// </summary>
        private void LiftCars()
        {
            // If we press the key to pickup...
            if (Game.IsDisabledControlJustPressed(2, Control.Enter))
            {
                // Get the closest vehicle to the player.
                var vehicle = World.GetClosestVehicle(PlayerCharacter.Position, 10f);

                // We have our vehicle.
                if (vehicle != null && vehicle.Exists() && PlayerCharacter.IsTouching(vehicle))
                {
                    // The direction to the vehicle.
                    var directionToVehicle = vehicle.Position - PlayerCharacter.Position;
                    directionToVehicle.Normalize();

                    // Make sure we're not on top of this vehicle.
                    var dot = Vector3.Dot(directionToVehicle, Vector3.WorldUp);
                    if (dot < -0.4f)
                        return;

                    // Get the mass of the vehicle.
                    float weight = HandlingFile.GetHandlingValue(vehicle, 0x000C);

                    // The maximum weight spidey can lift,
                    // about 10 tons.
                    const float maxWeight = 9071.85f;

                    // If the weight is over the max then we can't lift it.
                    if (weight > maxWeight)
                    {
                        UI.Notify("This vehicle is FAR to heavy for spidey to lift.");
                        return;
                    }

                    // Lifting: 
                    // amb@world_human_yoga@male@base => base_b

                    // Pickup:
                    // anim@mp_snowball => pickup_snowball

                    // Set the current pickup vehicle.
                    _currentPickupVehicle = vehicle;

                    // Clear the player tasks and make him play
                    // the pickup animation.
                    PlayerCharacter.ClearTasksImmediately();
                    PlayerCharacter.Heading = directionToVehicle.ToHeading();
                    PlayerCharacter.Task.PlayAnimation("anim@mp_snowball", "pickup_snowball", 8.0f, -8.0f,
                        500, AnimationFlags.StayInEndFrame, 0.0f);

                    // Let's us keep track of whether or not we played the animation.
                    var hasPlayedAnimation = false;

                    // Set the vehicle alpha to transparent so the
                    // player can see where he's going to throw.
                    vehicle.Alpha = 100;

                    GameWaiter.DoWhile(() =>
                    {
                        Controls.DisableControlsKeepRecording(2);
                        Game.EnableControlThisFrame(2, Control.ReplayStartStopRecording);
                        Game.EnableControlThisFrame(2, Control.MoveLeftRight);
                        Game.EnableControlThisFrame(2, Control.MoveUpDown);
                        Game.EnableControlThisFrame(2, Control.LookLeftRight);
                        Game.EnableControlThisFrame(2, Control.LookUpDown);
                        Game.EnableControlThisFrame(2, Control.NextCamera);

                        if (weight < (maxWeight / 2))
                        {
                            Game.EnableControlThisFrame(2, Control.Jump);
                            Game.EnableControlThisFrame(2, Control.Sprint);
                        }

                        if (PlayerCharacter.IsRagdoll || PlayerCharacter.IsBeingStunned ||
                            PlayerCharacter.IsDead || PlayerCharacter.IsInVehicle() ||
                            PlayerCharacter.IsGettingUp)
                            return false;

                        // Play our pickup animation sequence.
                        if (PlayerCharacter.IsPlayingAnimation("anim@mp_snowball", "pickup_snowball"))
                        {
                            hasPlayedAnimation = true;
                            vehicle.Velocity = Vector3.Zero;
                            Script.Yield();
                            return true;
                        }
                        // Now that we've played that animation let's continue.
                        else if (hasPlayedAnimation)
                        {
                            PlayerCharacter.ClearTasksImmediately();

                            PlayerCharacter.Task.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 8.0f, -8.0f, -1,
                                AnimationFlags.Loop |
                                AnimationFlags.AllowRotation |
                                AnimationFlags.UpperBodyOnly, 0.06f);

                            var model = vehicle.Model;
                            var d = model.GetDimensions();
                            d.X = Math.Max(1.25f, d.X);
                            var height = d.X * 0.8f;

                            // Attack the vehicle to the player.
                            vehicle.AttachToEntity(PlayerCharacter, -1,
                                PlayerCharacter.UpVector * height,
                                new Vector3(0, 90, 90), false, false, false, 0, true);

                            // Make sure to only do this once.
                            hasPlayedAnimation = false;
                        }

                        if (PlayerCharacter.GetConfigFlag(60))
                            PlayerCharacter.Velocity += Vector3.WorldDown * ((weight * .002f) * Time.UnscaledDeltaTime);

                        // Set the animation speed to 0 so it loops in that pose.
                        PlayerCharacter.SetAnimationSpeed("random@arrests", "kneeling_arrest_get_up", 0.0f);

                        // Once we're playing this animation we want to have the car attached to the player.
                        //PlayerCharacter.SetIKTarget(IKIndex.LeftArm, vehicle.GetOffsetInWorldCoords(new Vector3(0, 0.5f, -0.2f)), -1, 0);
                        //PlayerCharacter.SetIKTarget(IKIndex.RightArm, vehicle.GetOffsetInWorldCoords(new Vector3(0, -0.5f, -0.2f)), -1, 0);

                        // If we press vehicle enter again, then let's set down the vehicle.
                        if (Game.IsDisabledControlJustReleased(2, Control.Enter))
                        {
                            PlayerCharacter.Task.ClearAll();
                            PlayerCharacter.Task.PlayAnimation("anim@mp_snowball", "pickup_snowball", 8.0f, -4.0f, 500, AnimationFlags.AllowRotation, 0.0f);
                            GameWaiter.Wait(250);
                            vehicle.Detach();
                            vehicle.Position = PlayerCharacter.Position + PlayerCharacter.ForwardVector * 2.5f;
                            return false;
                        }

                        if (Game.IsDisabledControlJustReleased(2, Control.Attack))
                        {
                            PlayerCharacter.Task.ClearAll();
                            PlayerCharacter.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand", 8.0f, -4.0f, 500, AnimationFlags.AllowRotation, 0.1f);
                            vehicle.Detach();
                            vehicle.Velocity += PlayerCharacter.Velocity;
                            vehicle.Velocity = GameplayCamera.Direction * 25000 / weight;
                            PlayerCharacter.Velocity = Vector3.Zero;
                            return false;
                        }

                        return true;
                    }, 
                    null);

                    // Detach the vehicle if needed.
                    if (vehicle.IsAttached())
                    {
                        vehicle.Detach();
                    }
                    vehicle.ResetAlpha();

                    // Clear this animation from the player if it's still playing.
                    PlayerCharacter.Task.ClearAnimation("random@arrests", "kneeling_arrest_get_up");

                    // Make sure to clear the current pickup vehicle.
                    _currentPickupVehicle = null;

                    var timer = DateTime.Now + new TimeSpan(0, 0, 0, 0, 600);
                    while (DateTime.Now < timer)
                    {
                        vehicle.SetNoCollision(PlayerCharacter, true);
                        Script.Yield();
                    }
                    vehicle.SetNoCollision(PlayerCharacter, false);
                }
            }
        }

        /// <summary>
        /// Add the ability to do strong melee attacks.
        /// </summary>
        private void Attack()
        {
            // We'll use this as the amount of 
            // time we have to wait between attacks.
            const float cooldown = 0.3f;

            // Countdown the combo cooldown.
            if (_comboCooldown < cooldown)
            {
                _comboCooldown += Time.DeltaTime;
                return;
            }

            if (PlayerCharacter.IsRagdoll || PlayerCharacter.IsGettingUp ||
                PlayerCharacter.IsInVehicle() || PlayerCharacter.IsGettingIntoAVehicle)
                return;

            // Now we're going to get the closest entity to the player
            // within our line of sight.
            const float checkRadius = 10f; // We'll do 10 meters by default.
            var entities = World.GetNearbyEntities(PlayerCharacter.Position, checkRadius);

            // This will be our attack direction.
            _lastAttackDirection = GetAttackDirection();

            // This is the maximum dot product that the entity delta can
            // be from the attack direction.
            const float minDotProduct = 0.5f;

            // Draw a line to represent the attach direction.
            //GameGraphics.DrawLine(PlayerCharacter.Position, PlayerCharacter.Position + _lastAttackDirection, Color.Blue);

            // Make sure there where actually entities found.
            if (entities.Length > 0)
            {
                // Now we're going to trim out the entities.
                var closeEntities = GetEntitesThatCanBeAttacked(entities, minDotProduct);

                // Let's make sure there's entities that
                // are in the list first.
                if (closeEntities.Count > 0)
                {
                    // Now let's get the closest entity.
                    var closest = Utilities.GetClosestEntity(PlayerCharacter, closeEntities);

                    // Make sure that the closest entity exists.
                    if (closest != null)
                    {
                        //////////////////////////////////////
                        // Now we have the closest entity! :D
                        //////////////////////////////////////

                        // Draw a graphic to show the entity.
                        //GameGraphics.DrawMarker(GTAMarkerType.MarkerTypeUpsideDownCone, tMin.Position,
                        //    Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1), Color.Red, false, false, false, false);

                        // If we press the melee attack key, then initiate the attack.
                        if (Game.IsDisabledControlPressed(2, Control.MeleeAttackAlternate))
                        {
                            // Initiate the attack.
                            InitiateAttack(closest, Vector3.Zero);
                        }
                    }
                }
                else if (Game.IsDisabledControlPressed(2, Control.MeleeAttackAlternate) && PlayerCharacter.GetConfigFlag(60))
                {
                    var attackPos = PlayerCharacter.Position + _lastAttackDirection * 50f;
                    InitiateAttack(null, attackPos);
                }
            }
        }

        /// <summary>
        /// Checks an array of entities and returns a list of entities that can be attacked.
        /// </summary>
        /// <param name="entities">The initial entities to check.</param>
        /// <param name="minDotProduct">The minimum dot product from the players forward vector, to the entity delta.</param>
        /// <returns></returns>
        private List<Entity> GetEntitesThatCanBeAttacked(Entity[] entities, float minDotProduct)
        {
            var closeEntities = new List<Entity>();
            for (int i = 0; i < entities.Length; i++)
            {
                // Let's grab the entity at this index in the original array.
                var entity = entities[i];

                // Make sure this entity is not the player.
                if (entity.Handle == PlayerCharacter.Handle)
                    continue;

                // Get the entity type, and make sure it's a ped / vehicle.
                var entityType = entity.GetEntityType();
                if (entityType == EntityType.Object || entityType == EntityType.None)
                    continue;

                // Now let's check if the entity is within line of sight.
                var entityDelta = entity.Position - PlayerCharacter.Position;

                // Normalize the vector.
                entityDelta.Normalize();

                // Check the dot product.
                if (Vector3.Dot(_lastAttackDirection, entityDelta) > minDotProduct)
                {
                    // Add this entity to the close entities.
                    closeEntities.Add(entity);
                }
            }

            // Return the entities that are valid.
            return closeEntities;
        }

        /// <summary>
        /// Initiates an attack on the specified entity.
        /// </summary>
        /// <param name="entity"></param>
        private void InitiateAttack(Entity entity, Vector3 referencePosition)
        {
            // Get a random combonation (combo) move.
            _comboIndex = (_comboIndex + 1) % 2;

            // This the attack animation we're going to use.
            var animationName = string.Empty;
            var animationDict = string.Empty;
            var bone = Bone.SKEL_R_Hand;
            float attackStart = GetAnimationForComboMove(_comboIndex, ref animationName,
                ref animationDict, ref bone);
            //UI.ShowSubtitle(combonation.ToString());

            // This is the max time we can be in this loop.
            const float maxLoopTime = 0.5f;

            // This is our timer variable that
            // we will use for the countdown.
            var loopTimer = 0f;

            // The meters per second acceleration that
            // the player will move towards the entity.
            const float acceleration = 30f;

            var wasInAir = false;

            PlayerCharacter.Task.ClearAll();

            // Clear the player tasks.
            if (!PlayerCharacter.GetConfigFlag(60))
                wasInAir = true;
            else PlayerCharacter.Velocity = Vector3.Zero;

            // The player is going to play the attack animation and freeze at the 
            // attackEndPosition.
            PlayerCharacter.Task.PlayAnimation(animationDict, animationName, 8.0f, -4.0f, 500,
                AnimationFlags.AllowRotation | AnimationFlags.StayInEndFrame, attackStart);

            // Reset the combo cooldown.
            _comboCooldown = 0f;

            // Cache the entities position.
            var entityPosition = entity?.Position ?? referencePosition;

            // The initial direction to the target.
            // We'll use this so that the player doesn't unrealistically turn around and attack
            // the entity.
            var initialEntityDirection = entityPosition - PlayerCharacter.Position;
            initialEntityDirection.Normalize(); // We need to normalize this vector for a correct calculation.

            // If the timer exceeds the max time then we drop out of the loop.
            while (loopTimer < maxLoopTime)
            {
                // Make the player think he's on the ground.
                PlayerCharacter.SetConfigFlag(60, true);

                // Up the loop timer.
                loopTimer += Time.DeltaTime;

                // Get the direction we need.
                entityPosition = entity?.Position ?? referencePosition;

                // Make sure that while we're in this loop, 
                // that we disable the controls as needed.
                DisableControls();

                // Get the current direction.
                var currentDirection = entityPosition - PlayerCharacter.Position;
                currentDirection.Normalize();

                if (entity == null)
                    currentDirection = initialEntityDirection;

                // Set the character's heading.
                PlayerCharacter.Heading = currentDirection.ToHeading();

                // Check if we're playing the combo move.
                if (PlayerCharacter.IsPlayingAnimation(animationDict, animationName))
                {
                    // Set the animation speed.
                    //PlayerCharacter.SetAnimationSpeed(animationDict, animationName, 1.2f);

                    // Check if we're ready to do a raycast.
                    if (PlayerCharacter.GetAnimationTime(animationDict, animationName) >= attackStart)
                    {
                        // Get the hand coord.
                        var handPos = PlayerCharacter.GetBoneCoord(bone);

                        // Start the shape test and get the result.
                        var shapeTest = WorldProbe.StartShapeTestCapsule(handPos, handPos, 1f,
                            ShapeTestFlags.IntersectPeds |
                            ShapeTestFlags.IntersectVehicles |
                            ShapeTestFlags.IntersectObjects,
                            PlayerCharacter);
                        var result = shapeTest.GetResult();

                        var inRange = entity != null && (PlayerCharacter.IsTouching(entity) || Vector3.Distance(PlayerCharacter.Position, entity.Position) < 2f);

                        // Check the result.
                        if (result.Hit || inRange)
                        {
                            var entityType = result.EntityType;
                            Vector3 offset;
                            Entity entityHit = result.EntityHit;
                            if (inRange)
                            {
                                entityType = entity.GetEntityType();
                                entityHit = entity;
                                offset = entity.Position - PlayerCharacter.Position;
                            }
                            else offset = entityHit?.Position - result.EndCoords ?? Vector3.Zero;
                            switch (entityType)
                            {
                                case EntityType.Object:
                                    entityHit.Velocity = currentDirection * 15f;
                                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(entity, "FLASHLIGHT_HIT_BODY"));
                                    break;
                                case EntityType.Ped:
                                    var ped = (Ped)entityHit;
                                    const int damage = (int)(5000 / 70);
                                    ped.ApplyDamage(damage);
                                    if (!ped.IsDead)
                                        ped.Task.ClearAllImmediately();
                                    ped.SetToRagdoll(0);
                                    var timer = 0.2f;
                                    while (!ped.IsRagdoll && timer > 0)
                                    {
                                        timer -= Time.UnscaledDeltaTime;
                                        Script.Yield();
                                    }
                                    ped.Velocity = currentDirection * 15f;
                                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(entity, "FLASHLIGHT_HIT_BODY"));
                                    break;
                                case EntityType.Vehicle:
                                    var vehicle = (Vehicle)entityHit;
                                    vehicle.ApplyForce(ForceFlags.StrongForce, currentDirection * 17808.69f,
                                        offset, 0, false, false, false);
                                    var vehOffset = vehicle.GetOffsetFromWorldCoords(result.EndCoords);
                                    vehicle.SetDamage(vehOffset, 2500, 5000);
                                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(entity, "KNUCKLE_DUSTER_COMBAT_PED_HIT_BODY"));
                                    break;
                            }
                            DamagedEntity?.Invoke(this, new EventArgs(), entity, entity?.Position + offset ?? Vector3.Zero);
                            PlayerCharacter.Velocity /= 4;
                            break;
                        }
                    }
                }

                // Set the player's velocity.
                PlayerCharacter.Velocity = currentDirection * acceleration + (entity?.Velocity ?? Vector3.Zero);

                // Yield so that we're not doing a constant loop in 1 frame.
                Script.Yield();
            }

            // If we didn't get to finish the attack, then we're going to clear
            // the player tasks so he's not stuck in an animation.
            //PlayerCharacter.Task.ClearAll();

            if (wasInAir)
                GameWaiter.Wait(250);
        }

        /// <summary>
        /// Get's an animation for a specific combo move index (0 - 2 inclusive) and
        /// returns the point in the animation where the player lands the hit.
        /// </summary>
        /// <param name="combonationIndex">The combonation index.</param>
        /// <param name="animationName">The returned animation name.</param>
        /// <param name="animationDict">The returned animation dictionary.</param>
        /// <returns>Returns the point in the animation where the player lands the hit.</returns>
        private float GetAnimationForComboMove(int combonationIndex, ref string animationName,
            ref string animationDict, ref Bone bone)
        {

            // The end position of the attack animation. (when the player lands the hit)
            float attackStart = 0.15f;

            // Now set our combo moves animation based on 
            // our combo number.
            switch (combonationIndex)
            {
                case 0:
                    if (!PlayerCharacter.GetConfigFlag(60))
                    {
                        animationDict = "swimming@swim";
                        animationName = "melee_underwater_moving_unarmed";
                        attackStart = 0.23f;
                    }
                    else
                    {
                        animationDict = "melee@unarmed@streamed_core";
                        animationName = "heavy_finishing_punch";
                        attackStart = 0.25f;
                    }
                    bone = Bone.SKEL_R_Hand;
                    break;
                case 1:
                    if (!PlayerCharacter.GetConfigFlag(60))
                    {
                        animationDict = "swimming@swim";
                        animationName = "melee_surface_still";
                        attackStart = 0.1f;
                    }
                    else
                    {
                        animationDict = "melee@unarmed@streamed_core";
                        animationName = "plyr_takedown_front_elbow";

                    }
                    bone = Bone.MH_R_Elbow;
                    break;
                case 2:
                    if (!PlayerCharacter.GetConfigFlag(60))
                    {
                        animationDict = "swimming@swim";
                        animationName = "melee_surface_still";
                        attackStart = 0.1f;
                    }
                    else
                    {
                        animationDict = "melee@unarmed@streamed_core";
                        animationName = "plyr_takedown_front_elbow";
                        attackStart = 0.2f;
                    }
                    bone = Bone.SKEL_L_Hand;
                    break;
            }

            // Return the attack end position.
            return attackStart;
        }

        /// <summary>
        /// Returns the direction that the player should attack based on input.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetAttackDirection()
        {
            Vector3 attackDirection;

            // Store some constant variables where we'll estimate some
            // things about the player's attacks.
            const float minInputLength = 0.1f;

            // We'll get the local direction of input. This will be the main
            // direction of our attack.
            var cameraRotation = Quaternion.Euler(GameplayCamera.Rotation);
            var xInput = Game.GetControlNormal(2, Control.MoveLeftRight);
            var yInput = Game.GetControlNormal(2, Control.MoveUpDown);
            var inputDirection = new Vector3(xInput, -yInput, 0f);
            var localInput = Vector3.ProjectOnPlane(cameraRotation * inputDirection, Vector3.WorldUp); // Make sure this is flat.

            // We'll use this direction if there's no input direction.
            var noInputDirection = Vector3.ProjectOnPlane(PlayerCharacter.ForwardVector, Vector3.WorldUp);

            // If the input direction is actually 
            // not empty then we're going to use that as the
            // attack direction.
            if (localInput.Length() > minInputLength)
            {
                attackDirection = localInput;
            }
            // If not, then we'll use the player
            // direction.
            else
            {
                attackDirection = noInputDirection;
            }

            // Return the normalized vector.
            return attackDirection.Normalized;
        }

        /// <summary>
        /// Disables all melee / attack controls.
        /// </summary>
        private void DisableControls()
        {
            Game.DisableControlThisFrame(2, Control.MeleeAttack1);
            Game.DisableControlThisFrame(2, Control.MeleeAttack2);
            Game.DisableControlThisFrame(2, Control.MeleeAttackAlternate);
            Game.DisableControlThisFrame(2, Control.MeleeAttackHeavy);
            Game.DisableControlThisFrame(2, Control.MeleeAttackLight);
            Game.DisableControlThisFrame(2, Control.MeleeBlock);
            Game.DisableControlThisFrame(2, Control.Enter);
        }

        /// <summary>
        /// Called when the script stops.
        /// </summary>
        public override void Stop()
        {
            // Clear this animation from the player if it's still playing.
            PlayerCharacter.Task.ClearAnimation("random@arrests", "kneeling_arrest_get_up");

            // Delete this vehicle if it exists.
            _currentPickupVehicle?.Delete();
        }
    }
}
