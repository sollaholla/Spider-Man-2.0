using GTA;
using GTA.Math;
using GTA.Native;
using spiderman.net.Library;
using spiderman.net.Library.Extensions;
using Rope = spiderman.net.Library.Rope;
using System.Collections.Generic;
using System.Drawing;
using System;
using spiderman.net.Scripts;
using spiderman.net.Abilities.Types;

namespace spiderman.net.Abilities
{
    /// <summary>
    /// The web attachments ability allows the player to attach
    /// multiple entities together.
    /// </summary>
    public class WebAttachments : SpecialAbility
    {
        /// <summary>
        /// The web shooter's helper prop for rope attachments.
        /// </summary>
        private Entity _webShooterHelper;

        /// <summary>
        /// The rope created that attaches the web shooter to the 
        /// targetted entity.
        /// </summary>
        private Rope _webShooterRope;

        /// <summary>
        /// The entity currently attached to our web shooter.
        /// </summary>
        private Entity _ropeAttachedEntity;

        /// <summary>
        /// Keeps track of all activate attachments.
        /// </summary>
        private List<AttachmentInfo> _activeAttachments;

        /// <summary>
        /// The initial distance from the character to the web shooter.
        /// </summary>
        private float _initialWebLength;

        /// <summary>
        /// The main ctor.
        /// </summary>
        public WebAttachments()
        {
            Streaming.RequestAnimationDictionary("guard_reactions");
            Streaming.RequestAnimationDictionary("move_crouch_proto");
            Streaming.RequestAnimationDictionary("amb@code_human_wander_texting@male@base");
            Streaming.RequestAnimationDictionary("weapons@projectile@");
            Streaming.RequestAnimationDictionary("swimming@swim");
            new Model("bmx").Request();
            _activeAttachments = new List<AttachmentInfo>();
            BackgroundThread.RegisterTick(UpdateAttachments);
        }

        /// <summary>
        /// True if _ropeAttachedEntity is not null.
        /// </summary>
        private bool Attached {
            get {
                return Entity.Exists(_ropeAttachedEntity) && Rope.Exists(_webShooterRope);
            }
        }

        /// <summary>
        /// Our update method.
        /// </summary>
        public override void Update()
        {
            // Check if we press the grapple button.
            StartRopeAttachment();

            // If the web shooter rope is attached, let's check
            // player input to determine what to do next.
            ProcessRopeAttachment();

            // If not attached then make sure these objects no longer exist.
            EndRopeAttachment();
        }

        private void EndRopeAttachment()
        {
            if (!Attached)
            {
                // Delete the web shooter helper.
                if (Entity.Exists(_webShooterHelper))
                    _webShooterHelper.Delete();

                // Delete the web shooter rope.
                if (Rope.Exists(_webShooterRope))
                    _webShooterRope.Delete();
            }
        }

        private void ProcessRopeAttachment()
        {
            while (Attached)
            {
                Controls.DisableControlsKeepRecording(2);
                Game.EnableControlThisFrame(2, Control.ReplayStartStopRecording);
                Game.EnableControlThisFrame(2, Control.LookLeftRight);
                Game.EnableControlThisFrame(2, Control.LookBehind);
                Game.EnableControlThisFrame(2, Control.LookUpDown);
                Game.EnableControlThisFrame(2, Control.MoveLeftRight);
                Game.EnableControlThisFrame(2, Control.MoveUpDown);
                Game.EnableControlThisFrame(2, Control.NextCamera);
                Game.EnableControlThisFrame(2, Control.Sprint);
                Game.EnableControlThisFrame(2, Control.FrontendPause);
                Game.EnableControlThisFrame(2, Control.FrontendPauseAlternate);

                UI.ShowHudComponentThisFrame(HudComponent.Reticle);

                // If the entity died then end to prevent crashing.
                if (_ropeAttachedEntity.IsDead)
                {
                    EndAttachment();
                    break;
                }

                // Cancel the web.
                if (Game.IsDisabledControlJustPressed(2, Control.Aim))
                {
                    EndAttachment();
                    break;
                }

                var entityType = _ropeAttachedEntity.GetEntityType();
                var isPed = entityType == EntityType.Ped;
                var isVeh = entityType == EntityType.Vehicle;

                var headingDirection = PlayerCharacter.ForwardVector;
                var directionToEntity = _ropeAttachedEntity.Position - PlayerCharacter.Position;
                directionToEntity.Normalize();
                var distance = Vector3.Distance(PlayerCharacter.Position, _ropeAttachedEntity.Position);
                var reverse = Vector3.Angle(headingDirection, directionToEntity) < 45;

                if (Game.IsDisabledControlJustPressed(2, Control.Attack))
                {
                    bool flip = false;
                    if (reverse)
                    {
                        headingDirection = -directionToEntity;
                        if (distance < 25 && isVeh) flip = true;
                        else PlayerCharacter.PlayGrappleAnim(-1f);
                    }
                    else PlayerCharacter.PlayGrappleAnim(1f);
                    if (isPed)
                    {
                        var ped = new Ped(_ropeAttachedEntity.Handle);
                        ped.Task.ClearAllImmediately();
                        ped.SetToRagdoll(500);
                        ped.Velocity = headingDirection * distance;
                    }
                    _ropeAttachedEntity.Velocity = headingDirection * 25;
                    EndAttachment();
                    if (flip) FrontFlip();
                    Script.Wait(150);
                    break;
                }

                if (Game.IsDisabledControlJustPressed(2, Control.Enter) && isVeh)
                {
                    var veh = new Vehicle(_ropeAttachedEntity.Handle);
                    var driver = veh.Driver;
                    if (Entity.Exists(driver))
                    {
                        driver.Task.ClearAllImmediately();
                        driver.SetToRagdoll(500);
                        driver.Velocity += veh.Velocity;
                        veh.BreakDoor(VehicleDoor.FrontLeftDoor);
                        if (reverse) PlayerCharacter.PlayGrappleAnim(-1f);
                        else PlayerCharacter.PlayGrappleAnim(1f);
                        EndAttachment();
                        break;
                    }
                }

                if (Game.IsDisabledControlJustPressed(2, Control.Jump))
                {
                    PlayerCharacter.Task.PlayAnimation("move_crouch_proto", "idle_intro", 8.0f, -8.0f, -1, AnimationFlags.Loop, 0.0f);
                    PlayerCharacter.Task.PlayAnimation("amb@code_human_wander_texting@male@base", "static", 8.0f, -8.0f, -1,
                        AnimationFlags.UpperBodyOnly |
                        AnimationFlags.Loop |
                        AnimationFlags.AllowRotation, 0.0f);
                }

                if (Game.IsDisabledControlPressed(2, Control.Jump))
                {
                    var velocityDir = PlayerCharacter.RightVector;
                    _ropeAttachedEntity.ApplyForce(velocityDir);
                    PlayerCharacter.Heading = directionToEntity.ToHeading();
                    PlayerCharacter.SetIKTarget(IKIndex.LeftArm, PlayerCharacter.Position + directionToEntity, 0, 0);
                    PlayerCharacter.SetIKTarget(IKIndex.RightArm, PlayerCharacter.Position + directionToEntity, 0, 0);
                    PlayerCharacter.SetIKTarget(IKIndex.Head, _ropeAttachedEntity.Position + Vector3.WorldUp * 5f, 0, 0);
                }

                if (Game.IsDisabledControlJustReleased(2, Control.Jump))
                {
                    PlayerCharacter.Task.ClearAll();
                    EndAttachment();
                    break;
                }

                if (Game.IsDisabledControlJustPressed(2, Control.ParachuteSmoke))
                {
                    var ray = WorldProbe.StartShapeTestRay(GameplayCamera.Position,
                        GameplayCamera.Position + GameplayCamera.Direction * 100f, ShapeTestFlags.Everything, PlayerCharacter);
                    var res = ray.GetResult();

                    if (res.Hit)
                    {
                        Entity entity = GetEntityFromRayResult(res);
                        SetAttachedEntityToRagdollIfPed();
                        var rope = AttachRopeAttachedEntityToEntity(entity);
                        _activeAttachments.Add(new AttachmentInfo(entity, _ropeAttachedEntity, rope));
                        EndAttachment();
                        break;
                    }
                }

                Script.Yield();
            }
        }

        private void StartRopeAttachment()
        {
            Game.DisableControlThisFrame(2, Control.ParachuteSmoke);
            if (Game.IsDisabledControlJustPressed(2, Control.ParachuteSmoke) && !Attached)
            {
                // Do the camera raycast.
                ShapeTestResult result = DoCameraRay(500f);

                // Check if the result hit an entity.
                if (result.Hit && result.EntityHit != null)
                {
                    // Aim at the entity.
                    OnAimAtEntity(result.EntityHit);

                    // Shoot the web.
                    OnShotWeb(_webShooterHelper, result.EntityHit);

                    // Set the entity hit.
                    _ropeAttachedEntity = result.EntityHit;
                }
            }
        }

        private void UpdateAttachments()
        {
            var playerPos = PlayerCharacter.Position;
            _activeAttachments.ForEach(x => {
                x.ProcessAttachment(playerPos);
                if (x.Terminated)
                    _activeAttachments.Remove(x);
            });
        }

        private static Entity GetEntityFromRayResult(ShapeTestResult res)
        {
            Entity entity = null;
            if (res.EntityHit != null)
            {
                entity = res.EntityHit;
            }
            else
            {
                entity = World.CreateVehicle("bmx", res.EndCoords, 0f);
                entity.PositionNoOffset = res.EndCoords;
                entity.FreezePosition = true;
                entity.Alpha = 0;
                entity.HasCollision = false;
            }

            return entity;
        }

        private Rope AttachRopeAttachedEntityToEntity(Entity entity)
        {
            var length = Math.Min(Vector3.Distance(entity.Position, _ropeAttachedEntity.Position) / 2, 7.5f);
            var rope = Rope.AddRope(entity.Position, length, GTARopeType.ThickRope, length, 0.1f, true, false);
            rope.AttachEntities(entity, Vector3.Zero, _ropeAttachedEntity, Vector3.Zero, length);
            rope.ActivatePhysics();
            PlayerCharacter.PlayAimAnim(entity);
            return rope;
        }

        private void SetAttachedEntityToRagdollIfPed()
        {
            var type = _ropeAttachedEntity.GetEntityType();
            if (type == EntityType.Ped)
            {
                var ped = new Ped(_ropeAttachedEntity.Handle);
                if (!ped.IsRagdoll)
                {
                    ped.Task.ClearAllImmediately();
                    ped.Euphoria.BodyWrithe.Start(-1);
                    var timer = 0.2f;
                    while (timer > 0 && !ped.IsRagdoll)
                    {
                        timer -= Time.DeltaTime;
                        Script.Yield();
                    }
                }
            }
        }

        private void FrontFlip()
        {
            PlayerCharacter.Task.ClearAllImmediately();
            PlayerCharacter.Velocity = Vector3.WorldUp * 15f;
            PlayerCharacter.SetConfigFlag(60, false);
            Script.Wait(10);
            WebZip.OverrideFallHeight(float.MaxValue);
            PlayerCharacter.Task.Skydive();
            Script.Wait(1);
            PlayerCharacter.Task.PlayAnimation("swimming@swim", "recover_back_to_idle", 2.0f, -2.0f, 1150, AnimationFlags.AllowRotation, 0.0f);
            var t = 0.7f;
            while (t > 0)
            {
                t -= Time.DeltaTime;
                Game.SetControlNormal(2, Control.ParachutePitchUpDown, -1f);
                Script.Yield();
            }
        }

        private void EndAttachment()
        {
            // Delete the web helpers.
            _webShooterRope?.Delete();
            _webShooterHelper?.Delete();
            PlayerCharacter.Task.ClearAnimation("amb@code_human_wander_texting@male@base", "static");
            PlayerCharacter.Task.ClearAnimation("move_crouch_proto", "idle_intro");
            Script.Wait(200);
        }

        private void OnShotWeb(Entity webShooterHelper, Entity entityHit)
        {
            // The length of the rope.
            _initialWebLength = Vector3.Distance(webShooterHelper.Position, entityHit.Position);

            // Create the rope between the entity hit,
            // and the web shooter helper.
            _webShooterRope = Rope.AddRope(webShooterHelper.Position, _initialWebLength, GTARopeType.ThickRope,
                _initialWebLength, 0.1f, true, false);

            // Attach the entities together.
            _webShooterRope.AttachEntities(webShooterHelper, Vector3.Zero, entityHit, Vector3.Zero, _initialWebLength);
        }

        private void OnAimAtEntity(Entity entity)
        {
            PlayerCharacter.PlayAimAnim(entity);

            // Wait 150 ms.
            Script.Wait(150);

            // Now we need to spawn our web shooter helper.
            _webShooterHelper = CreateWebShooterHelper();
        }

        private ShapeTestResult DoCameraRay(float distance)
        {
            // Do our raycast in front of us.
            var ray = WorldProbe.StartShapeTestRay(GameplayCamera.Position,
                GameplayCamera.Position + GameplayCamera.Direction * distance,
                ShapeTestFlags.Everything, PlayerCharacter);

            // Get the ray's result.
            var result = ray.GetResult();
            return result;
        }

        private Vehicle CreateWebShooterHelper()
        {
            // Create the prop...
            var webShooterHelper = World.CreateVehicle("bmx", PlayerCharacter.Position);

            // Configure it's properties.
            webShooterHelper.HasCollision = false;
            webShooterHelper.IsVisible = false;

            // Attach to the player's right hand.
            webShooterHelper.AttachToEntity(PlayerCharacter, PlayerCharacter.GetBoneIndex(Bone.SKEL_R_Hand),
                Vector3.Zero, Vector3.Zero, false, false, false, 0, false);

            // return the new prop.
            return webShooterHelper;
        }

        /// <summary>
        /// Called when the script stops.
        /// </summary>
        public override void Stop()
        {
            BackgroundThread.UnregisterTick(UpdateAttachments);

            foreach (var rope in _activeAttachments)
                rope.Delete();

            // Detach the web shooter helper,
            // and delete it and the rope.
            if (_webShooterHelper != null)
            {
                _webShooterRope.DetachEntity(_webShooterHelper);
                _webShooterRope.Delete();
                _webShooterHelper.Delete();
            }
        }
    }
}
