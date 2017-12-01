using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ScriptThreads;
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.WebTech
{
    [WebTech("Attack Mode")]
    public class MultiDisarm : Tech
    {
        public override string Name => "Multi Disarm";

        public override string Description =>
            "Allows your web shooter to target nearby enemy weapons so you can disarm multiple enemies.";

        public override void Activate()
        {
        }

        public override void Deactivate()
        {
        }

        public override void Process()
        {
            if (Game.IsDisabledControlJustPressed(2, Control.Cover))
            {
                var peds = World.GetNearbyPeds(PlayerCharacter.Position, 20f);
                if (peds.Length <= 0) return;

                var playerForward = Vector3.ProjectOnPlane(GameplayCamera.Direction, Vector3.WorldUp);
                var playerPosition = PlayerCharacter.Position;
                var pList = new List<Ped>();

                for (var i = 0; i < peds.Length; i++)
                {
                    var ped = peds[i];
                    if (ped.IsPlayer) continue;
                    if (ped.IsInVehicle()) continue;
                    if (ped.IsDead) continue;

                    var dir = ped.Position - playerPosition;
                    dir.Normalize();
                    var angle = Vector3.Angle(playerForward.Normalized, dir);

                    if (angle < 90f)
                    {
                        var ray = WorldProbe.StartShapeTestRay(playerPosition, ped.Position,
                            ShapeTestFlags.IntersectMap, PlayerCharacter);
                        var result = ray.GetResult();
                        if (result.Hit) continue;
                        if (ped.Weapons.Current == null) continue;
                        if (ped.Weapons.Current.Hash == WeaponHash.Unarmed) continue;
                        pList.Add(ped);
                    }
                }

                if (pList.Count <= 0) return;

                var boneCoord = PlayerCharacter.GetBoneCoord(Bone.SKEL_R_Hand);
                var helperObj = World.CreateVehicle("bmx", boneCoord);
                helperObj.Alpha = 0;
                helperObj.AttachTo(PlayerCharacter, PlayerCharacter.GetBoneIndex(Bone.SKEL_R_Hand));
                var center = Vector3.Zero;
                var rList = new List<Rope>();
                foreach (var p in pList)
                {
                    center += p.Position;
                    var d = Vector3.Distance(helperObj.Position, p.Position);
                    var r = Rope.AddRope(helperObj.Position, d, GTARopeType.ThickRope, d, 0.1f, true, false);
                    r.AttachEntities(helperObj, Vector3.Zero, p, Vector3.Zero, d);
                    r.ActivatePhysics();
                    rList.Add(r);
                }
                center /= pList.Count;

                PlayerCharacter.PlayAimAnim(center);
                GameWaiter.Wait(300);
                PlayerCharacter.PlayGrappleAnim(-1f);
                foreach (var r in rList)
                    r.Delete();
                foreach (var p in pList)
                    DisarmPed(p);
                helperObj.Delete();
            }
        }

        private void DisarmPed(Ped ped)
        {
            ped.Task.ClearAllImmediately();
            ped.Task.PlayAnimation("missheist_agency3astumble_walk", "stumble_walk_player1",
                -8.0f, 4.0f, 1250, AnimationFlags.AllowRotation, 0.0f);
            var weaponObject = ped.Weapons.CurrentWeaponObject;
            if (weaponObject == null) return;
            weaponObject.Detach();
            weaponObject.ApplyForce(ped.ForwardVector * 150 * Time.UnscaledDeltaTime);
            ped.Weapons.RemoveAll();
            ped.Task.FightAgainst(PlayerCharacter);
        }
    }
}