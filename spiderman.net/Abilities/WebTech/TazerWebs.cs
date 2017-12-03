using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.SpecialAbilities;
using SpiderMan.Abilities.SpecialAbilities.PlayerOnly;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.WebTech
{
    [WebTech("Web Mode")]
    public class TazerWebs : Tech
    {
        public TazerWebs(SpiderManProfile profile) : 
            base(profile)
        {
            Abilities = new List<SpecialAbility>
            {
                new WebZip(profile),
                new WebSwing(profile)
            };
            Streaming.RequestAnimationDictionary("ragdoll@human");
        }

        public override string Name => "Tazer Webs";

        public override string Description => "Electrically charged web fluid.";

        public override void Activate()
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Tazer Webs.wav", 1.0f);
        }

        public override void Deactivate()
        {
        }

        public override void Process()
        {
            Game.DisableControlThisFrame(2, Control.ParachuteSmoke);

            var camRay = WorldProbe.StartShapeTestRay(GameplayCamera.Position, GameplayCamera.Position +
                                                                               GameplayCamera.Direction * 100f,
                ShapeTestFlags.IntersectPeds | ShapeTestFlags.IntersectVehicles,
                PlayerCharacter).GetResult();

            if (camRay.Hit)
            {
                var bounds = camRay.EntityHit.Model.GetDimensions();
                var z = bounds.Z / 2;

                World.DrawMarker(MarkerType.UpsideDownCone, camRay.EntityHit.Position + Vector3.WorldUp * z * 1.5f, Vector3.Zero, Vector3.Zero,
                    new Vector3(0.3f, 0.3f, 0.3f), Color.White);

                if (Game.IsDisabledControlJustPressed(2, Control.ParachuteSmoke))
                {
                    var directionToEntity = camRay.EntityHit.Position - PlayerCharacter.Position;
                    var distance = directionToEntity.Length();
                    directionToEntity.Normalize();


                    // Set the player's heading towards the target.
                    PlayerCharacter.Heading = directionToEntity.ToHeading();

                    // Play the web shoot animation.
                    PlayerCharacter.Task.PlayAnimation("guard_reactions", "1hand_aiming_to_idle", 8.0f, -8.0f, -1,
                        (AnimationFlags)40, 0.0f);

                    var playerBone = PlayerCharacter.GetBoneCoord(Bone.SKEL_R_Hand);
                    var rope = Rope.AddRope(playerBone, distance, GTARopeType.ThickRope, 0.2f, 0.1f, true, false);
                    var timer = 2.0f;
                    var counter = 0;

                    while (timer > 0)
                    {
                        PlayerCharacter.SetAnimationSpeed("guard_reactions", "1hand_aiming_to_idle", 0f);
                        playerBone = PlayerCharacter.GetBoneCoord(Bone.SKEL_R_Hand);
                        rope.PinVertex(0, playerBone);
                        rope.PinVertex(rope.VertexCount - 1, camRay.EntityHit.Position);
                        timer -= Time.DeltaTime;
                        directionToEntity = camRay.EntityHit.Position - PlayerCharacter.Position;
                        PlayerCharacter.Heading = directionToEntity.ToHeading();
                        if (counter % 10 == 0)
                        {
                            if (camRay.EntityType == EntityType.Ped)
                            {
                                var ped = new Ped(camRay.EntityHit.Handle);
                                Utilities.ShockPed(ped, 15);
                            }
                            Utilities.CreateParticleChain(playerBone, camRay.EntityHit.Position, Vector3.Zero,
                                nodeSize: 0.5f, noiseMultiplier: 0.0f);
                        }
                        counter++;
                        Script.Yield();
                    }

                    if (camRay.EntityType == EntityType.Vehicle)
                    {
                        var veh = new Vehicle(camRay.EntityHit.Handle);
                        veh.Explode();
                    }
                    PlayerCharacter.Task.ClearAll();
                    rope.Delete();
                }
            }
        }
    }
}