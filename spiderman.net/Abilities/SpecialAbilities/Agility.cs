using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;

namespace SpiderMan.Abilities.SpecialAbilities
{
    /// <summary>
    ///     This is used for jumping and greater speed.
    /// </summary>
    public class Agility : SpecialAbility
    {
        /// <summary>
        ///     The state of the player is used to determine whether or not the
        ///     player is walking / running / or spirning.
        /// </summary>
        private PlayerState _playerState;

        private float _desiredSpeed;

        /// <summary>
        ///     Our main constructor.
        /// </summary>
        public Agility(SpiderManProfile profile) : base(profile)
        {
            // These are so that spider-man is more stable
            // while he's falling / running into traffic.
            Profile.LocalUser.IsCollisionProof = true;
        }

        /// <summary>
        ///     The update / tick method for our ability.
        /// </summary>
        public override void Update()
        {
            // Loop this.
            if (Profile.LocalUser.IsRagdoll)
            {
                GameWaiter.Wait(300);
                Profile.LocalUser.Task.ClearAllImmediately();
            }
            Profile.LocalUser.CanRagdoll = false;

            // DarkSouls-style roll.
            HandleRoll();

            // Set the player state accordingly.
            SetPlayerState();

            // Check if we hit the ground, and make sure there are no cancelation flags.
            if (GroundRay(out var normal) && !SprintCancelationFlags())
            {
                ////////////////////////////////////////////////////////////
                // Now we need to speed up the player by the speed we want.
                // For now we'll use a value of 300% normal speed. Or about 20/ms.
                ////////////////////////////////////////////////////////////

                // First we need to get the direction we want.
                var direction = Vector3.Cross(-Profile.LocalUser.RightVector, normal);
                direction.Normalize(); // We'll have to normalize this, just in case.

                // We're going to do some debugging.
                //GameGraphics.DrawLine(PlayerCharacter.Position, PlayerCharacter.Position + direction * 5f, Color.Red);

                // For now we'll use a constant for the desired speed.
                var velocity = direction * _desiredSpeed * Profile.RunSpeedMultiplier;

                _desiredSpeed = _playerState != PlayerState.None ? Maths.Lerp(_desiredSpeed, 26.8224f, Time.UnscaledDeltaTime * 0.4f) : Maths.Lerp(_desiredSpeed, 10f, Time.UnscaledDeltaTime * 5f);

                //Now for our switch case we're going to see 
                //what state the player is in.
                switch (_playerState)
                {
                    // Let's set the velocity to the direction 
                    // multiplied by our desired speed.
                    case PlayerState.Run:
                        Profile.LocalUser.Velocity =
                            velocity / 2; // dividing by 2 so running is slower.
                        break;
                    case PlayerState.Sprint:
                        Profile.LocalUser.Velocity = velocity;
                        break;
                }
            }
            else
            {
                _desiredSpeed = Maths.Lerp(_desiredSpeed, 26.8224f, Time.UnscaledDeltaTime * 5f);
            }

            //UI.ShowSubtitle(_desiredSpeed.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Allows the player to do a evasive roll.
        /// </summary>
        private void HandleRoll()
        {
            if (!GetCanRoll() || !Profile.LocalUser.GetConfigFlag(60)
                || !Profile.LocalUser.IsRunning && !Profile.LocalUser.IsSprinting && !Profile.LocalUser.IsGettingUp)
                return;
            DoRoll();
        }

        private bool GetCanRoll()
        {
            return Profile.LocalUser.IsPlayer && Game.IsDisabledControlJustPressed(2, Control.LookBehind);
        }

        private void DoRoll()
        {
            if (Profile.LocalUser.IsGettingUp || Profile.LocalUser.IsRagdoll)
                Profile.LocalUser.Task.ClearAllImmediately();
            else Profile.LocalUser.Task.ClearAll();

            // Play the rolling animation.
            Profile.LocalUser.Task.PlayAnimation("move_fall", "land_roll",
                8.0f, -4.0f, 750, AnimationFlags.AllowRotation, 0.0f);

            Profile.LocalUser.Heading = Vector3.ProjectOnPlane(
                Quaternion.Euler(Profile.GetCameraRotation()) * Profile.GetInputDirection(),
                Vector3.WorldUp).ToHeading();

            var wasInv = Profile.LocalUser.IsInvincible;
            var wasColP = Profile.LocalUser.IsCollisionProof;
            var wasMelP = Profile.LocalUser.IsMeleeProof;
            var wasBp = Profile.LocalUser.IsBulletProof;

            Profile.LocalUser.IsInvincible = true;
            Profile.LocalUser.IsCollisionProof = true;
            Profile.LocalUser.IsMeleeProof = true;
            Profile.LocalUser.IsBulletProof = true;

            var timer = 0.5f;
            while (!Profile.LocalUser.IsPlayingAnimation("move_fall", "land_roll") &&
                timer > 0f)
            {
                timer -= Time.DeltaTime;
                Script.Yield();
            }
            while (true)
            {
                var onGround = GroundRay(out var normal) && Profile.LocalUser.IsPlayingAnimation("move_fall", "land_roll");
                if (!onGround)
                    break;

                // First we need to get the direction we want.
                var direction = Vector3.Cross(-Profile.LocalUser.RightVector, normal);
                direction.Normalize(); // We'll have to normalize this, just in case.
                Profile.LocalUser.Velocity = direction * 25f;

                Script.Yield();
            }

            Profile.LocalUser.IsInvincible = wasInv;
            Profile.LocalUser.IsCollisionProof = wasColP;
            Profile.LocalUser.IsMeleeProof = wasMelP;
            Profile.LocalUser.IsBulletProof = wasBp;
        }

        /// <summary>
        ///     If a ray (starting at the characters middle, and down to the floor) hit's anything,
        ///     then we return true; otherwise, we return false.
        /// </summary>
        /// <param name="normal">Returns the normal vector of the ray hit.</param>
        /// <returns></returns>
        private bool GroundRay(out Vector3 normal)
        {
            var startCoords = Profile.LocalUser.Position;
            var endCoords = startCoords - Vector3.WorldUp * 1.1f;
            var ray = WorldProbe.StartShapeTestRay(startCoords, endCoords, ShapeTestFlags.IntersectMap,
                Profile.LocalUser);
            var result = ray.GetResult();
            normal = result.SurfaceNormal;
            return result.Hit;
        }

        /// <summary>
        ///     A series of checks that result in a value
        ///     indicating if we can use our sprint ability or not.
        /// </summary>
        /// <returns></returns>
        private bool SprintCancelationFlags()
        {
            return !Profile.LocalUser.GetConfigFlag(104) || !Profile.LocalUser.GetConfigFlag(60);
        }

        /// <summary>
        ///     Set's the _playerState variable to the
        ///     correct movement state of the player.
        /// </summary>
        private void SetPlayerState()
        {
            // Set the player state to determine if he's running walking etc.
            if (Profile.LocalUser.IsRunning)
                _playerState = PlayerState.Run;
            else if (Profile.LocalUser.IsSprinting)
                _playerState = PlayerState.Sprint;

            // If the player's not either running or sprinting then we
            // don't want to activate our speed.
            else _playerState = PlayerState.None;
        }

        /// <summary>
        ///     Called when the mod is stopped.
        /// </summary>
        public override void Stop()
        {
            // We need to reset these flags when the mod stops.
            Profile.LocalUser.CanRagdoll = true;
            Profile.LocalUser.IsCollisionProof = false;
        }

        /// <summary>
        ///     The state of the player's movement.
        /// </summary>
        private enum PlayerState
        {
            None,
            Run,
            Sprint
        }
    }
}