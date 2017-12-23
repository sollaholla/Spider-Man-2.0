using System;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Memory;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;

namespace SpiderMan.Abilities.SpecialAbilities
{
    /// <summary>
    ///     The melee ability is for having devistating attacks. Applying massive damage
    ///     to anything the player hits.
    /// </summary>
    public class Melee : SpecialAbility
    {
        public delegate void OnDamagedEntity(object sender, EventArgs e, Entity entity, Vector3 hitCoords);

        /// <summary>
        ///     Called when we melee attack any entity, after our punch landed.
        /// </summary>
        public static event OnDamagedEntity DamagedEntity;

        /// <summary>
        ///     Helps us track what vehicle we've picked up.
        /// </summary>
        private Vehicle _currentPickupVehicle;

        private int _comboIndex;

        /// <summary>
        ///     The main constructor.
        /// </summary>
        public Melee(SpiderManProfile profile) : base(profile)
        {
            Streaming.RequestAnimationDictionary("melee@unarmed@streamed_core");
            Streaming.RequestAnimationDictionary("anim@mp_snowball");
            Streaming.RequestAnimationDictionary("random@arrests");
            Streaming.RequestAnimationDictionary("swimming@swim");
            Streaming.RequestAnimationDictionary("weapons@projectile@");
            Profile.LocalUser.BlockPermanentEvents = true;
        }

        public bool ThrowVehicle { get; set; }

        /// <summary>
        ///     Our update method.
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
        ///     Add the ability to lift vehicles.
        /// </summary>
        private void LiftCars()
        {
            if (!Profile.LocalUser.IsPlayer) return;

            // If we press the key to pickup...
            if (!Game.IsDisabledControlJustPressed(2, Control.Enter)) return;

            // Get the closest vehicle to the player.
            var vehicle = World.GetClosestVehicle(Profile.LocalUser.Position, 10f);
            LiftThrowVehicle(vehicle);
        }

        private void LiftThrowVehicle(Vehicle vehicle)
        {
            // We have our vehicle.
            if (vehicle == null || !vehicle.Exists() || !Profile.LocalUser.IsTouching(vehicle)) return;

            // The direction to the vehicle.
            var directionToVehicle = vehicle.Position - Profile.LocalUser.Position;
            directionToVehicle.Normalize();

            // Make sure we're not on top of this vehicle.
            var dot = Vector3.Dot(directionToVehicle, Vector3.WorldUp);
            if (dot < -0.4f)
                return;

            // Get the mass of the vehicle.
            var weight = HandlingFile.GetHandlingValue(vehicle, 0x000C);

            // The maximum weight spidey can lift,
            // about 10 tons.
            const float maxWeight = 9071.85f;

            // If the weight is over the max then we can't lift it.
            if (weight > maxWeight)
            {
                if (Profile.LocalUser.IsPlayer)
                {
                    UI.Notify("This vehicle is FAR to heavy for spidey to lift.");
                }
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
            Profile.LocalUser.ClearTasksImmediately();
            Profile.LocalUser.Heading = directionToVehicle.ToHeading();
            Profile.LocalUser.Task.PlayAnimation("anim@mp_snowball", "pickup_snowball", 8.0f, -8.0f,
                500, AnimationFlags.StayInEndFrame, 0.0f);

            // Let's us keep track of whether or not we played the animation.
            var hasPlayedAnimation = false;

            // Set the vehicle alpha to transparent so the
            // player can see where he's going to throw.
            vehicle.Alpha = 100;

            GameWaiter.Wait(100);

            while (true)
            {
                Profile.DisableControls();

                if (Profile.LocalUser.IsPlayer)
                {
                    Game.EnableControlThisFrame(2, Control.ReplayStartStopRecording);
                    Game.EnableControlThisFrame(2, Control.MoveLeftRight);
                    Game.EnableControlThisFrame(2, Control.MoveUpDown);
                    Game.EnableControlThisFrame(2, Control.LookLeftRight);
                    Game.EnableControlThisFrame(2, Control.LookUpDown);
                    Game.EnableControlThisFrame(2, Control.NextCamera);
                }

                if (weight < maxWeight / 2)
                {
                    if (Profile.LocalUser.IsPlayer)
                    {
                        Game.EnableControlThisFrame(2, Control.Jump);
                        Game.EnableControlThisFrame(2, Control.Sprint);
                    }
                }

                if (Profile.LocalUser.IsRagdoll || Profile.LocalUser.IsBeingStunned ||
                    Profile.LocalUser.IsDead || Profile.LocalUser.IsInVehicle() ||
                    Profile.LocalUser.IsGettingUp)
                    break;

                // Play our pickup animation sequence.
                if (Profile.LocalUser.IsPlayingAnimation("anim@mp_snowball", "pickup_snowball"))
                {
                    hasPlayedAnimation = true;
                    vehicle.Velocity = Vector3.Zero;
                }
                // Now that we've played that animation let's continue.
                else if (hasPlayedAnimation)
                {
                    Profile.LocalUser.ClearTasksImmediately();
                    Profile.LocalUser.Task.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 8.0f,
                        -8.0f, -1,
                        AnimationFlags.Loop |
                        AnimationFlags.AllowRotation |
                        AnimationFlags.UpperBodyOnly, 0.06f);

                    var model = vehicle.Model;
                    var d = model.GetDimensions();
                    d.X = Math.Max(1.25f, d.X);
                    var height = d.X * 0.8f;

                    // Attack the vehicle to the player.
                    vehicle.AttachToEntity(Profile.LocalUser, -1,
                        Profile.LocalUser.UpVector * height,
                        new Vector3(0, 90, 90), false, false, false, 0, true);

                    // Make sure to only do this once.
                    hasPlayedAnimation = false;
                }

                if (Profile.LocalUser.GetConfigFlag(60))
                    Profile.LocalUser.Velocity +=
                        Vector3.WorldDown * (weight * .002f * Time.UnscaledDeltaTime);

                // Set the animation speed to 0 so it loops in that pose.
                Profile.LocalUser.SetAnimationSpeed("random@arrests", "kneeling_arrest_get_up", 0.0f);

                // If we press vehicle enter again, then let's set down the vehicle.
                if (Profile.LocalUser.IsPlayer && Game.IsDisabledControlJustPressed(2, Control.Enter))
                {
                    Profile.LocalUser.Task.ClearAll();
                    Profile.LocalUser.Task.PlayAnimation("anim@mp_snowball", "pickup_snowball", 8.0f, -4.0f,
                        500, AnimationFlags.AllowRotation, 0.0f);
                    GameWaiter.Wait(250);
                    vehicle.Detach();
                    vehicle.SetCoordsSafely(Profile.LocalUser.Position + Profile.LocalUser.ForwardVector * 2.5f);
                    break;
                }

                if (GetCanThrow())
                {
                    Profile.LocalUser.Task.ClearAll();
                    Profile.LocalUser.Task.PlayAnimation("weapons@projectile@", "throw_m_fb_stand", 8.0f,
                        -4.0f, 500, AnimationFlags.AllowRotation, 0.1f);
                    vehicle.Detach();
                    vehicle.Velocity += Profile.LocalUser.Velocity;
                    vehicle.Velocity = Profile.GetCameraDirection() * 25000 / weight;
                    Profile.LocalUser.Velocity = Vector3.Zero;
                    ThrowVehicle = false;
                    break;
                }

                Script.Yield();
            }

            // Detach the vehicle if needed.
            if (vehicle.IsAttached())
                vehicle.Detach();
            vehicle.ResetAlpha();

            // Clear this animation from the player if it's still playing.
            Profile.LocalUser.Task.ClearAnimation("random@arrests", "kneeling_arrest_get_up");

            // Make sure to clear the current pickup vehicle.
            _currentPickupVehicle = null;

            var timer = DateTime.Now + new TimeSpan(0, 0, 0, 0, 600);
            while (DateTime.Now < timer)
            {
                vehicle.SetNoCollision(Profile.LocalUser, true);
                Script.Yield();
            }
            vehicle.SetNoCollision(Profile.LocalUser, false);
        }

        private bool GetCanThrow()
        {
            return Profile.LocalUser.IsPlayer ? Game.IsDisabledControlJustReleased(2, Control.Attack) : ThrowVehicle;
        }

        /// <summary>
        ///     Add the ability to do strong melee attacks.
        /// </summary>
        private void Attack()
        {
            if (!Profile.LocalUser.IsPlayer)
                return;

            // Check if we pressed the attack button.
            DisableControls();

            // Now get the closest entity and validate it's type.
            var targetEntity = GetClosestTarget(8);
            //EntityType t;
            if (targetEntity == null || targetEntity.GetEntityType() == EntityType.Object) return;

            var bounds = targetEntity.Model.GetDimensions();
            var z = bounds.Z / 2;

            World.DrawMarker(MarkerType.UpsideDownCone, targetEntity.Position + Vector3.WorldUp * z * 2f, Vector3.Zero, Vector3.Zero,
                new Vector3(0.3f, 0.3f, 0.3f), Color.DarkRed);

            if (!Game.IsDisabledControlPressed(2, Control.MeleeAttackAlternate))
                return;

            PunchEntity(targetEntity);
        }

        private void PunchEntity(Entity targetEntity)
        {
            //int ms = 0;
            Profile.LocalUser.IsCollisionProof = true;
            Profile.LocalUser.IsMeleeProof = true;
            Profile.LocalUser.Task.ClearAll();
            var m = Profile.LocalUser.Model.GetDimensions();
            Profile.LocalUser.SetCoordsSafely(Profile.LocalUser.Position + Vector3.WorldDown * m.Z / 2);
            Profile.LocalUser.Velocity = Vector3.Zero;
            Function.Call(Hash.SET_ENTITY_RECORDS_COLLISIONS, Profile.LocalUser.Handle, false);

            GetComboMoveData(out var dict, out var anim, out var duration, out var punchTime, out var bone);

            Profile.LocalUser.Task.PlayAnimation(dict, anim,
                8.0f, -4.0f, duration, AnimationFlags.AllowRotation, 0.0f);

            var timer = 1f;
            while (timer > 0f)
            {
                timer -= Time.DeltaTime;

                if (Profile.LocalUser.IsRagdoll)
                    break;

                // Continue to disable the controls.
                Profile.DisableControls();
                if (Profile.LocalUser.IsPlayer)
                {
                    Game.EnableControlThisFrame(2, Control.LookLeftRight);
                    Game.EnableControlThisFrame(2, Control.LookUpDown);
                }

                // Cache some variables.
                var direction = targetEntity.Position - Profile.LocalUser.Position;

                Profile.LocalUser.SetAnimationSpeed(dict, anim, 1.25f);

                // Set the player heading towards the entity.
                Profile.LocalUser.Heading = direction.ToHeading();
                Profile.LocalUser.Velocity = direction.Normalized * direction.Length() * 2 / 0.25f/* + targetEntity.Velocity*/;
                Profile.LocalUser.SetConfigFlag(60, true);
                Profile.LocalUser.SetConfigFlag(104, true);

                //ms++;
                //UI.ShowSubtitle(ms + "\n" + 
                //    PlayerCharacter.GetAnimationTime("melee@unarmed@streamed_core", "heavy_finishing_punch"));

                var boneCoord = Profile.LocalUser.GetBoneCoord(bone);
                if (Profile.LocalUser.GetAnimationTime(dict, anim) > punchTime)
                {
                    var ray = World.RaycastCapsule(boneCoord, boneCoord, 1f,
                        (IntersectOptions)(int)(ShapeTestFlags.IntersectObjects |
                                                  ShapeTestFlags.IntersectPeds |
                                                  ShapeTestFlags.IntersectVehicles), Profile.LocalUser);
                    if (ray.DitHitEntity)
                    {
                        var entity = ray.HitEntity;
                        var type = entity.GetEntityType();
                        ApplyDamage(direction, entity, type, boneCoord);
                    }
                    else if (Profile.LocalUser.IsTouching(targetEntity))
                    {
                        ApplyDamage(direction, targetEntity, targetEntity.GetEntityType(), boneCoord);
                    }
                    break;
                }

                Script.Yield();
            }

            Profile.LocalUser.SetConfigFlag(60, false);
            Profile.LocalUser.IsMeleeProof = false;
            Profile.LocalUser.IsCollisionProof = false;
            Function.Call(Hash.SET_ENTITY_RECORDS_COLLISIONS, Profile.LocalUser.Handle, true);
            GameWaiter.Wait(75);
        }

        private void GetComboMoveData(out string animDict, out string animName, out int animDuration, out float punchTime, out Bone bone)
        {
            // melee@unarmed@streamed_core
            // counter_attack_r - 0.15 & 600ms
            // plyr_takedown_rear_lefthook - 0.3 & 389
            // heavy_punch_b - 0.27 & 551
            // heavy_finishing_punch - 0.3 & 521
            animDict = "melee@unarmed@streamed_core";
            animName = string.Empty;
            animDuration = 0;
            punchTime = 0f;
            bone = 0x0;

            switch (_comboIndex)
            {
                case 0:
                    animName = "counter_attack_r";
                    animDuration = 1000;
                    punchTime = 0.15f;
                    bone = Bone.SKEL_R_Hand;
                    break;
                case 1:
                    animName = "plyr_takedown_rear_lefthook";
                    animDuration = 718;
                    punchTime = 0.3f;
                    bone = Bone.SKEL_L_Hand;
                    break;
                case 2:
                    animName = "heavy_punch_b";
                    animDuration = 1000;
                    punchTime = 0.27f;
                    bone = Bone.SKEL_R_Hand;
                    break;
                case 3:
                    animName = "heavy_finishing_punch";
                    animDuration = 1000;
                    punchTime = 0.3f;
                    bone = Bone.SKEL_L_Hand;
                    break;
            }

            _comboIndex = (_comboIndex + 1) % 4;
        }

        private Entity GetClosestTarget(float radius)
        {
            var closestVehicle = World.GetClosestVehicle(Profile.LocalUser.Position, radius);
            var closePeds = World.GetNearbyPeds(Profile.LocalUser, radius);
            var closestPed = Utilities.GetClosestEntity(Profile.LocalUser, closePeds.Select(x => (Entity)x).Where(x => !x.IsDead).ToList());

            if (closestPed == null || closestVehicle == null) return closestPed ?? closestVehicle;
            var dist1 = closestPed.Position.DistanceToSquared(Profile.LocalUser.Position);
            var dist2 = closestVehicle.Position.DistanceToSquared(Profile.LocalUser.Position);
            return dist1 <= dist2 && !((Ped)closestPed).IsInVehicle() ? closestPed : closestVehicle;
        }

        private void ApplyDamage(Vector3 direction, Entity entity, EntityType type, Vector3 hitCoords)
        {

            switch (type)
            {
                case EntityType.Vehicle:
                    var veh = new Vehicle(entity.Handle);
                    var offset = veh.GetOffsetFromWorldCoords(hitCoords);
                    veh.SetDamage(offset, 5000f, 7000f);
                    veh.Health -= (int)(45 * Profile.PunchDamageMultiplier);
                    veh.ApplyForce(ForceFlags.StrongForce, Profile.LocalUser.ForwardVector * 30000f * Profile.PunchForceMultiplier, direction, 0, false, false, false);
                    GTAGraphics.StartParticle("core", "bul_carmetal", hitCoords, Vector3.Zero, 1.0f);
                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(entity, "KNUCKLE_DUSTER_COMBAT_PED_HIT_BODY"));
                    break;
                case EntityType.Ped:
                    var ped = new Ped(entity.Handle);
                    ped.SetToRagdoll(0);
                    ped.Velocity = direction * 35 * Profile.PunchForceMultiplier;
                    ped.ApplyDamage((int)(50 * Profile.PunchDamageMultiplier));
                    Audio.ReleaseSound(Audio.PlaySoundFromEntity(entity, "FLASHLIGHT_HIT_BODY"));
                    break;
            }
            DamagedEntity?.Invoke(this, null, entity, hitCoords);
        }

        /// <summary>
        ///     Disables all melee / attack controls.
        /// </summary>
        private void DisableControls()
        {
            if (!Profile.LocalUser.IsPlayer)
                return;

            Game.DisableControlThisFrame(2, Control.MeleeAttack1);
            Game.DisableControlThisFrame(2, Control.MeleeAttack2);
            Game.DisableControlThisFrame(2, Control.MeleeAttackAlternate);
            Game.DisableControlThisFrame(2, Control.MeleeAttackHeavy);
            Game.DisableControlThisFrame(2, Control.MeleeAttackLight);
            Game.DisableControlThisFrame(2, Control.MeleeBlock);
            Game.DisableControlThisFrame(2, Control.Enter);
        }

        /// <summary>
        ///     Called when the script stops.
        /// </summary>
        public override void Stop()
        {
            _currentPickupVehicle?.Delete();
        }
    }
}