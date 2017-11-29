using GTA;
using GTA.Math;
using spiderman.net.Library;
using System.Drawing;
using System;
using spiderman.net.Library.Extensions;
using spiderman.net.Scripts;
using System.Collections.Generic;

namespace spiderman.net.Abilities
{
    /// <summary>
    /// This is used for jumping and greater speed.
    /// </summary>
    public class Agility : SpecialAbility
    {
        /// <summary>
        /// The state of the player's movement.
        /// </summary>
        enum PlayerState
        {
            None,
            Run,
            Sprint
        }

        /// <summary>
        /// The state of the player is used to determine whether or not the 
        /// player is walking / running / or spirning.
        /// </summary>
        private PlayerState _playerState;

        /// <summary>
        /// Our main constructor.
        /// </summary>
        public Agility()
        {
            // These are so that spider-man is more stable
            // while he's falling / running into traffic.
            PlayerCharacter.IsCollisionProof = true;
        }

        /// <summary>
        /// The update / tick method for our ability.
        /// </summary>
        public override void Update()
        {
            // Loop this.
            PlayerCharacter.CanRagdoll = false;

            // DarkSouls-style roll.
            HandleRoll();

            // Set the player state accordingly.
            SetPlayerState();

            // Here's where we're going to set the player's super jump.
            Game.Player.SetSuperJumpThisFrame();

            // Check if we hit the ground, and make sure there are no cancelation flags.
            if (GroundRay(out var normal) && !SprintCancelationFlags())
            {
                ////////////////////////////////////////////////////////////
                // Now we need to speed up the player by the speed we want.
                // For now we'll use a value of 300% normal speed. Or about 20/ms.
                ////////////////////////////////////////////////////////////

                // First we need to get the direction we want.
                var direction = Vector3.Cross(-PlayerCharacter.RightVector, normal);
                direction.Normalize(); // We'll have to normalize this, just in case.

                // We're going to do some debugging.
                //GameGraphics.DrawLine(PlayerCharacter.Position, PlayerCharacter.Position + direction * 5f, Color.Red);

                // For now we'll use a constant for the desired speed.
                const float desiredSpeed = 20f;

                //Now for our switch case we're going to see 
                //what state the player is in.
                switch (_playerState)
                {
                    // Let's set the velocity to the direction 
                    // multiplied by our desired speed.
                    case PlayerState.Run:
                        PlayerCharacter.Velocity = direction * desiredSpeed / 2; // dividing by 2 so running is slower.
                        break;
                    case PlayerState.Sprint:
                        PlayerCharacter.Velocity = direction * desiredSpeed;
                        break;
                }
            }
        }

        /// <summary>
        /// Allows the player to do a evasive roll.
        /// </summary>
        private void HandleRoll()
        {
            if (!Game.IsDisabledControlJustPressed(2, Control.LookBehind) || !PlayerCharacter.GetConfigFlag(60))
                return;

            PlayerCharacter.Task.ClearAllImmediately();

            // Play the rolling animation.
            PlayerCharacter.Task.PlayAnimation("move_fall", "land_roll",
                8.0f, -8.0f, 750, AnimationFlags.AllowRotation, 0f);

            bool wasInv = PlayerCharacter.IsInvincible;
            bool wasColP = PlayerCharacter.IsCollisionProof;
            bool wasMelP = PlayerCharacter.IsMeleeProof;
            bool wasBP = PlayerCharacter.IsBulletProof;
            PlayerCharacter.IsInvincible = true;
            PlayerCharacter.IsCollisionProof = true;
            PlayerCharacter.IsMeleeProof = true;
            PlayerCharacter.IsBulletProof = true;
            var timer = 0.5f;
            while (!PlayerCharacter.IsPlayingAnimation("move_fall", "land_roll") && timer > 0)
            {
                timer -= Time.UnscaledDeltaTime;
                Script.Yield();
            }
            var peds = World.GetNearbyPeds(PlayerCharacter, 50f);
            var acc = new List<int>();
            while (PlayerCharacter.IsPlayingAnimation("move_fall", "land_roll"))
            {
                PlayerCharacter.Velocity = PlayerCharacter.ForwardVector * 30f;
                for (int i = 0; i < peds.Length; i++)
                {
                    var ped = peds[i];
                    acc.Add(ped.Accuracy);
                    ped.Accuracy = 0;
                }
                Script.Yield();
            }
            for (int i = 0; i < peds.Length; i++)
            {
                var ped = peds[i];
                ped.Accuracy = acc[i];
            }

            PlayerCharacter.IsInvincible = wasInv;
            PlayerCharacter.IsCollisionProof = wasColP;
            PlayerCharacter.IsMeleeProof = wasMelP;
            PlayerCharacter.IsBulletProof = wasBP;

        }

        /// <summary>
        /// If a ray (starting at the characters middle, and down to the floor) hit's anything,
        /// then we return true; otherwise, we return false.
        /// </summary>
        /// <param name="normal">Returns the normal vector of the ray hit.</param>
        /// <returns></returns>
        private bool GroundRay(out Vector3 normal)
        {
            var startCoords = PlayerCharacter.Position;
            var endCoords = startCoords - Vector3.WorldUp * 1.1f;
            var ray = WorldProbe.StartShapeTestRay(startCoords, endCoords, ShapeTestFlags.IntersectMap, PlayerCharacter);
            var result = ray.GetResult();
            normal = result.SurfaceNormal;
            return result.Hit;
        }

        /// <summary>
        /// A series of checks that result in a value 
        /// indicating if we can use our sprint ability or not.
        /// </summary>
        /// <returns></returns>
        private bool SprintCancelationFlags()
        {
            return !PlayerCharacter.GetConfigFlag(104) || !PlayerCharacter.GetConfigFlag(60);
        }

        /// <summary>
        /// Set's the _playerState variable to the 
        /// correct movement state of the player.
        /// </summary>
        private void SetPlayerState()
        {
            // Set the player state to determine if he's running walking etc.
            if (PlayerCharacter.IsRunning)
                _playerState = PlayerState.Run;
            else if (PlayerCharacter.IsSprinting)
                _playerState = PlayerState.Sprint;

            // If the player's not either running or sprinting then we
            // don't want to activate our speed.
            else _playerState = PlayerState.None;
        }

        /// <summary>
        /// Called when the mod is stopped.
        /// </summary>
        public override void Stop()
        {
            // We need to reset these flags when the mod stops.
            PlayerCharacter.CanRagdoll = true;
            PlayerCharacter.IsCollisionProof = false;
        }

        /// <summary>
        /// Returns the movement vector as a vector2.
        /// </summary>
        /// <returns></returns>
        private Vector2 GetMovementVector()
        {
            return new Vector2(Game.GetControlNormal(2, Control.MoveLeftRight),
                -Game.GetControlNormal(2, Control.MoveUpDown));
        }
    }
}
