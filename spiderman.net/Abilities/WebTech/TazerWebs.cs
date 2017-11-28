using GTA;
using spiderman.net.Library;
using System.Collections.Generic;
using Rope = spiderman.net.Library.Rope;
using GTA.Math;
using spiderman.net.Library.Extensions;
using spiderman.net.Scripts;
using spiderman.net.Abilities.Types;
using spiderman.net.Abilities.Attributes;

namespace spiderman.net.Abilities.WebTech
{
    [WebTech("Web Mode")]
    public class TazerWebs : Tech
    {
        public override string Name => "Tazer Webs";

        public override string Description => "Electrically charged web fluid.";

        public TazerWebs()
        {
            Abilities = new List<SpecialAbility>()
            {
                new WebZip(),
                new WebSwing()
            };
            Streaming.RequestAnimationDictionary("ragdoll@human");
        }
        
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

            if (Game.IsDisabledControlJustPressed(2, Control.ParachuteSmoke))
            {
                var camRay = WorldProbe.StartShapeTestRay(GameplayCamera.Position, GameplayCamera.Position +
                    GameplayCamera.Direction * 100f, ShapeTestFlags.IntersectPeds | ShapeTestFlags.IntersectVehicles,
                    PlayerCharacter).GetResult();

                if (camRay.Hit)
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
                            Utilities.CreateParticleChain(playerBone, camRay.EntityHit.Position, Vector3.Zero, nodeSize: 0.5f, noiseMultiplier: 0.0f);
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
