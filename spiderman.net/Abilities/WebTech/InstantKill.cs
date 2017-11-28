using GTA;
using GTA.Math;
using GTA.Native;
using spiderman.net.Abilities.Attributes;
using spiderman.net.Abilities.Types;
using spiderman.net.Library;
using spiderman.net.Library.Extensions;
using spiderman.net.Scripts;
using System;
using System.Collections.Generic;

namespace spiderman.net.Abilities.WebTech
{
    /// <summary>
    /// A ultimate brute mode for spidey's suit.
    /// </summary>
    [WebTech("Suit Mode")]
    public class InstantKill : Tech
    {
        public InstantKill()
        {
            Streaming.RequestAnimationDictionary("melee@unarmed@base");

            // Register our functions for updating physics.
            Melee.DamagedEntity += OnDamagedEntity;
        }

        /// <summary>
        /// Holds a list of particles we've spawned with this class.
        /// </summary>
        private List<ParticleLooped> _particles = new List<ParticleLooped>();

        /// <summary>
        /// The name of this tech.
        /// </summary>
        public override string Name => "Instant Kill";

        /// <summary>
        /// The tech abilities description.
        /// </summary>
        public override string Description => "Super-charges your fists for an ultimate brute mode, using tech collected from the Shocker's glove.";
        
        /// <summary>
        /// Play's particle effects from one bone to another.
        /// </summary>
        /// <param name="startBone">The starting bone.</param>
        /// <param name="endBone">The ending bone.</param>
        /// <param name="numParticles">The number of particles.</param>
        /// <param name="scale">The scale of the particles.</param>
        /// <param name="direction">The direction to move each itteration.</param>
        /// <returns></returns>
        private List<ParticleLooped> PlayElectricityBoneToBone(Bone startBone, Bone endBone, int numParticles, float scale, Vector3 direction)
        {
            // "core", "ent_amb_elec_crackle"

            var boneCoord1 = PlayerCharacter.GetBoneCoord(startBone);
            var boneCoord2 = PlayerCharacter.GetBoneCoord(endBone);
            float distance = Vector3.Distance(boneCoord1, boneCoord2);
            float distanceToTravel = distance / numParticles;
            var particles = new List<ParticleLooped>();

            for (int i = 0; i < numParticles; i++)
            {
                var p = PlayerCharacter.StartLoopedParticle("core", "ent_ray_prologue_elec_crackle_sp",
                    direction * (distanceToTravel * i), Vector3.Zero, startBone, scale);
                particles.Add(p);
            }

            return particles;
        }

        /// <summary>
        /// Called when the player melee hits an entity. Entity may return null if it was a freeform punch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="entity"></param>
        /// <param name="hitCoords"></param>
        private void OnDamagedEntity(object sender, System.EventArgs e, Entity entity, Vector3 hitCoords)
        {
            // Make sure this wasn't a freeform punch.
            if (entity == null)
                return;

            // Get the direction to the entity.
            var dir = entity.Position - PlayerCharacter.Position;
            dir.Normalize();

            // If the entity is a vehicle...
            if (entity is Vehicle vehicle)
            {
                // Apply a significant amount of force.
                vehicle.ApplyForce(dir * 750 * Time.UnscaledDeltaTime);

                // Apply damage to the hull.
                vehicle.SetDamage(vehicle.GetOffsetFromWorldCoords(PlayerCharacter.Position), 10000, 7500);

                // Make sure to put this vehicle out of commision.
                vehicle.EngineRunning = false;
                vehicle.EngineHealth = 0f;
                vehicle.EngineCanDegrade = false; // Make sure the vehicle doesn't explode.
            }
            // If this entity is a ped...
            else if (entity is Ped ped)
            {
                // Apply a significant force to the ped.
                ped.Velocity = dir * 150f;

                // Shock the ped.
                Utilities.ShockPed(ped, 200);
            }

            // Set the amount of time for slowmo.
            var time = 0.1f;

            // Shake the camera as if this was an extreme blow.
            //GameplayCamera.Shake(CameraShake.MediumExplosion, 0.1f);

            // Stop the player from moving.
            PlayerCharacter.Velocity = Vector3.Zero;

            // Loop slowmotion and add a electric shock effect around the player.
            // We'll use the count variable to track when we should spawn the electricity.
            int count = 0;

            // Init a new random object.
            Random rand = new Random();

            var burstShock = false;

            // Loop until the time is finished.
            while (time > 0f)
            {
                // Every 4 loops spawn a bolt of electricity.
                if (count % 4 == 0 && hitCoords != Vector3.Zero)
                {
                    if (!burstShock)
                    {
                        // Play a shock particle on nearby entities.
                        var entities = World.GetNearbyEntities(PlayerCharacter.Position, 15);
                        for (int i = 0; i < entities.Length; i++)
                        {
                            var ent = entities[i];
                            if (ent == entity)
                                continue;

                            Utilities.CreateParticleChain(hitCoords, ent.Position, Vector3.Zero, 0.1f, particleScale: 0.2f);

                            if (ent.GetEntityType() == EntityType.Ped)
                            {
                                var ped = new Ped(ent.Handle);
                                if (ped.IsPlayer)
                                    continue;

                                ped.SetToRagdoll(5);
                                Utilities.ShockPed(ped, 5);
                            }
                            else if (ent.GetEntityType() == EntityType.Vehicle)
                            {
                                var v = new Vehicle(ent.Handle);
                                v.EngineHealth = 0f;
                                v.EngineCanDegrade = false;
                            }
                        }
                        burstShock = true;
                    }
                }

                // Increment the count.
                count++;

                // Set the game in slowmo.
                //Game.TimeScale = 0.1f;

                // Decrease the time in this loop.
                time -= Time.DeltaTime;

                // Yield the script.
                Script.Yield();
            }
        }

        /// <summary>
        /// Called when the tech is initialized.
        /// </summary>
        public override void Activate()
        {
            // Play the instant kill audio.
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "InstantKill.wav", 1f);

            // Start the rampage screen effect.
            //Graphics.StartScreenEffect(ScreenEffect.Rampage);

            // Spawn particles on the player's hands.
            _particles.AddRange(PlayElectricityBoneToBone(Bone.SKEL_R_Hand, Bone.MH_R_Elbow, 3, 0.6f, Vector3.RelativeLeft));
            _particles.AddRange(PlayElectricityBoneToBone(Bone.SKEL_L_Hand, Bone.MH_L_Elbow, 3, 0.6f, Vector3.RelativeLeft));

            // Also make him play a "ready" animation.
            if (PlayerCharacter.GetConfigFlag(60))
                PlayerCharacter.Task.PlayAnimation("melee@unarmed@base", "melee_intro_plyr", 8.0f, -4.0f, 1250, AnimationFlags.AllowRotation, 0.0f);

            // Enhance the suit.
            PlayerCharacter.IsExplosionProof = true;
            PlayerCharacter.IsFireProof = true;
        }

        /// <summary>
        /// Called when the tech is deactivated.
        /// </summary>
        public override void Deactivate()
        {
            // Make sure we unsubscribe from these events...
            Melee.DamagedEntity -= OnDamagedEntity;

            // Reset the player's suit.
            PlayerCharacter.IsExplosionProof = false;
            PlayerCharacter.IsFireProof = true;

            // Make sure to stop the screen effect.
            //Library.Graphics.StopScreenEffect(ScreenEffect.Rampage);

            // Remove the particles we've created.
            foreach (var particle in _particles)
                particle.Remove();
            _particles.Clear();
        }

        /// <summary>
        /// Called while the tech is being updated.
        /// </summary>
        public override void Process()
        {
        }
    }
}
