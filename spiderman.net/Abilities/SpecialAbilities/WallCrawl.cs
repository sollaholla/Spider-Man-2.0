using GTA;
using GTA.Math;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ScriptThreads;

namespace SpiderMan.Abilities.SpecialAbilities
{
    public class WallCrawl : SpecialAbility
    {
        public WallCrawl()
        {
            // Request our thingys.
            Streaming.RequestAnimationDictionary("swimming@swim");
            Streaming.RequestAnimationDictionary("move_crouch_proto");
            Streaming.RequestAnimationDictionary("laddersbase");
            Streaming.RequestAnimationDictionary("move_m@brave");
            new Model("w_pi_pistol").Request();
        }

        public override void Update()
        {
            // Disable the context key.
            Game.DisableControlThisFrame(2, Control.Context);

            // Check if we pressed the climb control..
            if (Game.IsDisabledControlPressed(2, Control.Context))
            {
                // Get the camera raycast.
                var ray = Utilities.GetPlayerRay(2.5f, ShapeTestFlags.IntersectMap, PlayerCharacter);

                // Begin the climb.
                if (ray.Hit && Vector3.Dot(ray.SurfaceNormal, Vector3.WorldUp) < 0.25f)
                    UpdateClimbing(ray.EndCoords, ray.SurfaceNormal);
            }
        }

        private void UpdateClimbing(Vector3 surfacePosition, Vector3 surfaceNormal)
        {
            // Create the attachmentObject.
            var attachmentObject = World.CreateProp("w_pi_pistol", surfacePosition, false, false);
            attachmentObject.PositionNoOffset = surfacePosition;
            attachmentObject.HasCollision = false;
            attachmentObject.FreezePosition = true;
            attachmentObject.Quaternion = Maths.LookRotation(Vector3.WorldUp, surfaceNormal);
            attachmentObject.Alpha = 0;
            // attachmentObject.Alpha = 0;

            // Attach the player to the attachment object.
            PlayerCharacter.Task.ClearAllImmediately();
            PlayerCharacter.AttachTo(attachmentObject, 0, new Vector3(0, 0, 1), Vector3.Zero);
            PlayerCharacter.Task.PlayAnimation("move_crouch_proto", "idle_intro", 8.0f, -1, AnimationFlags.Loop);

            // Delay for the control.
            GameWaiter.Wait(10);

            // Create camera.
            var camDirection = Vector3.Zero;
            var moveDirection = Vector3.Zero;
            //var camSpawn = attachmentObject.GetOffsetInWorldCoords(new Vector3(0, -2, 1));
            //var cam = World.CreateCamera(camSpawn, Vector3.Zero, 60);
            //cam.Direction = attachmentObject.Position - cam.Position;
            //cam.TransitionIn(100);

            //var pivot = World.CreateProp("w_pi_pistol", attachmentObject.Position, false, false);
            //pivot.FreezePosition = true;
            //pivot.IsVisible = false;
            //pivot.Quaternion = attachmentObject.Quaternion;

            //// Camera rotation.
            //var xRotation = 0f;
            //var yRotation = 45f;

            // flags.
            var cancelClimb = false;

            while (!cancelClimb)
            {
                // Override the enabled controls.
                SetActiveControls();

                // Rotate the wall cam.
                //RotateCam(cam, pivot, attachmentObject, ref xRotation, ref yRotation);

                // Get the movement vector.
                var movement = GetMovementVector();

                // Move the player attachment.
                Move( /*cam, */surfaceNormal, attachmentObject, ref camDirection, ref moveDirection, movement);

                // Play the player movement animations.
                DoMovementAnimations(attachmentObject, movement.Length());

                // Start a new surface ray.
                var surfaceRay = WorldProbe.StartShapeTestRay(attachmentObject.Position + attachmentObject.UpVector,
                    attachmentObject.Position - attachmentObject.UpVector,
                    ShapeTestFlags.IntersectMap, attachmentObject);

                // Handle the ejection keys.
                HandleEject(ref cancelClimb, attachmentObject);

                // Make sure the result is not empty.
                var result = surfaceRay.GetResult();
                if (!result.Hit)
                {
                    DetachPlayer(attachmentObject);
                    GameWaiter.Wait(10);
                    if (Game.IsDisabledControlPressed(2, Control.Sprint))
                    {
                        PlayerCharacter.Task.Skydive();
                        PlayerCharacter.Task.PlayAnimation("swimming@swim", "recover_back_to_idle",
                            2.0f, -2.0f, 1150, AnimationFlags.AllowRotation, 0.0f);
                        PlayerCharacter.Velocity = Vector3.WorldUp * 25f;
                        WebZip.OverrideFallHeight(float.MaxValue);
                        GameWaiter.Wait(100);
                    }
                    else
                    {
                        PlayerCharacter.Task.Climb();
                        WebZip.OverrideFallHeight(0f);
                    }
                    cancelClimb = true;
//                    break;
                }

                // Set the surface position.
                surfacePosition = result.EndCoords;

                // Check the surface normal.
                if (surfaceNormal != result.SurfaceNormal)
                {
                    // If the surface normal has changed, then change immediately rotation the player
                    // to match the normal.
                    surfaceNormal = result.SurfaceNormal;
                    Move( /*cam, */surfaceNormal, attachmentObject, ref camDirection, ref moveDirection, movement,
                        false);
                }

                attachmentObject.PositionNoOffset = surfacePosition;

                Script.Yield();
            }

            // Destroy the camera.
            //Utilities.DestroyAllCameras();

            // Delte the camera pivot.
            //pivot.Delete();
        }

        private void HandleEject(ref bool cancelClimb, Entity attachmentObject)
        {
            // If we press context again then drop out.
            if (Game.IsDisabledControlJustPressed(2, Control.Context))
            {
                // Detach the player.
                cancelClimb = true;
                JumpOff(attachmentObject, 15);
                PlayerCharacter.Task.Jump();
                return;
            }

            if (Game.IsDisabledControlJustPressed(2, Control.Jump))
            {
                cancelClimb = true;
                JumpOff(attachmentObject, 25);
                PlayerCharacter.Task.PlayAnimation("swimming@swim", "recover_flip_back_to_front", 8.0f, -4.0f, 500,
                    AnimationFlags.AllowRotation, 0.0f);
                PlayerCharacter.Task.Skydive();
                return;
            }

            if (Vector3.Dot(Vector3.WorldUp, attachmentObject.UpVector) > 0.25f)
            {
                DetachPlayer(attachmentObject);
                cancelClimb = true;
            }
        }

        private void JumpOff(Entity attachmentObject, float force)
        {
            var headingDir = attachmentObject.UpVector.ToHeading();
            DetachPlayer(attachmentObject);
            PlayerCharacter.Task.ClearAllImmediately();
            PlayerCharacter.Heading = headingDir;
            PlayerCharacter.Velocity = PlayerCharacter.ForwardVector * force;
        }

//        private void RotateCam(Camera cam, Entity pivot, Entity target, ref float vert, ref float hori)
//        {
//            vert -=
//            (Game.IsControlEnabled(2, Control.LookLeftRight)
//                ? Game.GetControlNormal(2, Control.LookLeftRight)
//                : Game.GetDisabledControlNormal(2, Control.LookLeftRight)) * Time.UnscaledDeltaTime * 750f;
//
//            hori +=
//            (Game.IsControlEnabled(2, Control.LookUpDown)
//                ? Game.GetControlNormal(2, Control.LookUpDown)
//                : Game.GetDisabledControlNormal(2, Control.LookUpDown)) * Time.UnscaledDeltaTime * 750f;
//
//            hori = Maths.Clamp(hori, 5f, 89f);
//
//            var vertRot = Maths.AngleAxis(vert, target.UpVector);
//            var horiRot = Quaternion.Euler(hori, 0, 0);
//            var rotation = vertRot /* * horiRot*/;
//
//            pivot.Position = target.Position;
//            pivot.Quaternion = rotation;
//
//            var upDir = target.UpVector * 5f;
//            var downDir = -pivot.UpVector * 10f;
//            var targetCoords = pivot.Position + upDir + downDir;
//            var ray = WorldProbe.StartShapeTestRay(pivot.Position, targetCoords, ShapeTestFlags.IntersectMap, null);
//            var res = ray.GetResult();
//            var ex = Vector3.Zero;
//            var pivotDir = pivot.Position - cam.Position;
//            cam.Rotation = Maths.LookRotation(pivotDir, target.UpVector).ToEulerAngles();
//
//            if (res.Hit)
//                ex = res.EndCoords + pivotDir.Normalized * cam.NearClip * 2.1f;
//            else ex = targetCoords;
//
//            cam.Position = ex;
//        }

        private void DoMovementAnimations(Prop attachmentObject, float speed)
        {
            if (speed > 0f)
                if (!Game.IsControlPressed(2, Control.Sprint))
                    PlayAnim(attachmentObject, "laddersbase", "climb_up", -90, 0.25f);
                else
                    PlayAnim(attachmentObject, "move_m@brave", "run", 0, 1);
            else if (PlayerCharacter.IsPlayingAnimation("laddersbase", "climb_up"))
                PlayAnim(attachmentObject, "laddersbase", "base_left_hand_up", -90, 0.25f);
            else if (PlayerCharacter.IsPlayingAnimation("move_m@brave", "run"))
                PlayAnim(attachmentObject, "move_crouch_proto", "idle_intro", 0, 1);
        }

        private void PlayAnim(Prop attachmentObject, string animationDict, string animationName, float xAngle,
            float zOffset)
        {
            if (!PlayerCharacter.IsPlayingAnimation(animationDict, animationName))
            {
                ReAttach(attachmentObject, xAngle, zOffset);

                PlayerCharacter.Task.PlayAnimation(animationDict, animationName, 8.0f, -1, AnimationFlags.Loop);

                var timer = 0.25f;

                while (timer > 0f && !PlayerCharacter.IsPlayingAnimation(animationDict, animationName))
                {
                    timer -= Time.UnscaledDeltaTime;
                    Script.Yield();
                }
            }
        }

        private void ReAttach(Prop attachmentObject, float xAngle, float zOffset)
        {
            PlayerCharacter.Detach();
            PlayerCharacter.AttachToEntity(
                attachmentObject, 0, new Vector3(0, 0, zOffset),
                new Vector3(xAngle, 0, 0), false, false, true, 0, true);
        }

        private static void SetActiveControls()
        {
            Controls.DisableControlsKeepRecording(2);
            Game.EnableControlThisFrame(2, Control.LookLeftRight);
            Game.EnableControlThisFrame(2, Control.LookUpDown);
            Game.EnableControlThisFrame(2, Control.NextCamera);
            Game.EnableControlThisFrame(2, Control.FrontendPause);
            Game.EnableControlThisFrame(2, Control.FrontendPauseAlternate);
        }

        private static void Move( /*Camera cam, */ Vector3 surfaceNormal, Entity attachmentObject,
            ref Vector3 camDirection, ref Vector3 moveDirection, Vector3 movement, bool lerp = true)
        {
            if (movement != Vector3.Zero)
            {
                var targetDir = /*attachmentObject.Position - */ /*cam*/ /*GameplayCamera.Position*/
                    GameplayCamera.Direction + Vector3.WorldUp * 0.5f;
                targetDir.Normalize();
                var targetCamDir = Vector3.ProjectOnPlane(targetDir, surfaceNormal);
                targetCamDir.Normalize();
                camDirection = targetCamDir;

                var targetMovementDir = Maths.LookRotation(camDirection, surfaceNormal) * movement;
                targetMovementDir.Normalize();
                moveDirection = lerp ? Vector3.Lerp(moveDirection, targetMovementDir, Time.UnscaledDeltaTime * 5f) : targetMovementDir;

                var lookRotation = Maths.LookRotation(moveDirection, surfaceNormal);
                attachmentObject.Quaternion = lookRotation;

                var startCoords = attachmentObject.Position + surfaceNormal;
                var endCoords = startCoords + attachmentObject.ForwardVector * 0.5f;
                var ray = WorldProbe.StartShapeTestRay(startCoords, endCoords, ShapeTestFlags.IntersectMap, null);
                var res = ray.GetResult();

                if (!res.Hit)
                    attachmentObject.PositionNoOffset =
                        attachmentObject.Position +
                        attachmentObject.ForwardVector *
                        Time.DeltaTime * 5f * (Game.IsDisabledControlPressed(2, Control.Sprint) ? 2f : 1f);
            }
        }

        /// <summary>
        ///     Get the direction of input.
        /// </summary>
//        /// <param name="cameraRelative">True if this input is relative to the camera.</param>
        /// <returns></returns>
        private Vector3 GetMovementVector()
        {
            var xMove =
                Game.IsControlEnabled(2, Control.MoveLeftRight)
                    ? Game.GetControlNormal(2, Control.MoveLeftRight)
                    : Game.GetDisabledControlNormal(2, Control.MoveLeftRight);
            var yMove =
                Game.IsControlEnabled(2, Control.MoveUpDown)
                    ? Game.GetControlNormal(2, Control.MoveUpDown)
                    : Game.GetDisabledControlNormal(2, Control.MoveUpDown);

            var result = new Vector3(xMove, -yMove, 0);
            return result;
        }

        /// <summary>
        ///     Detaches the player from the given entity and
        ///     deletes the attachement entity.
        /// </summary>
        /// <param name="attachmentObject"></param>
        private void DetachPlayer(Entity attachmentObject)
        {
            PlayerCharacter.Detach();
            attachmentObject.Delete();
            ClearAnimations();
        }

        public override void Stop()
        {
            PlayerCharacter.Detach();
            PlayerCharacter.IsVisible = true;
            ClearAnimations();
            //Utilities.DestroyAllCameras();
        }

        private void ClearAnimations()
        {
            if (PlayerCharacter.IsPlayingAnimation("laddersbase", "base_left_hand_up"))
                PlayerCharacter.Task.ClearAnimation("laddersbase", "base_left_hand_up");

            if (PlayerCharacter.IsPlayingAnimation("move_crouch_proto", "idle_intro"))
                PlayerCharacter.Task.ClearAnimation("move_crouch_proto", "idle_intro");
        }
    }
}