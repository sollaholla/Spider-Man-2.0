using GTA;
using GTA.Math;
using spiderman.net.Abilities.Attributes;
using spiderman.net.Abilities.Types;
using spiderman.net.Library;
using spiderman.net.Library.Extensions;
using System.Drawing;
using System;

namespace spiderman.net.Abilities.WebTech
{
    /// <summary>
    /// Adds the ability to do a devistating punch to enemies.
    /// </summary>
    [WebTech("Combat Mode")]
    public class WebPunch : Tech
    {
        /// <summary>
        /// The name of the tech ability.
        /// </summary>
        public override string Name => "Web Punch";

        /// <summary>
        /// The description of the tech ability.
        /// </summary>
        public override string Description => "Allows spidey to deal a devistating, long range, punch to his enemies.";

        /// <summary>
        /// Called once the tech is activated.
        /// </summary>
        public override void Activate()
        {
        }

        /// <summary>
        /// Called once the tech is deactivated.
        /// </summary>
        public override void Deactivate()
        {
        }

        /// <summary>
        /// Called every tick that the tech is active.
        /// </summary>
        public override void Process()
        {
            var entity = Utilities.GetAimedEntity(75f, out var endCoords, out var entityType);
            if (entity != null && entityType == EntityType.Ped && !entity.IsDead)
            {
                var drawPos = entity.Position + Vector3.WorldUp * 2;
                GTAGraphics.DrawMarker(GTAMarkerType.MarkerTypeUpsideDownCone, drawPos, Vector3.Zero, Vector3.Zero,
                    new Vector3(1, 1, 1), Color.Red, false, false, false, false);

                if (Game.IsDisabledControlJustPressed(2, Control.Cover))
                {
                    var ped = new Ped(entity.Handle);
                    PlayerCharacter.PlayAimAnim(entity);
                    PlayerCharacter.FaceEntity(entity);
                    var boneCoord = PlayerCharacter.GetBoneCoord(Bone.SKEL_R_Hand);
                    var length = Vector3.Distance(entity.Position, boneCoord);
                    var rope = GTARope.AddRope(boneCoord, length, GTARopeType.ThickRope, length / 2, 0.1f, false, false);
                    GameWaiter.DoWhile(500, () => true, () =>
                    {
                        rope.PinVertex(0, PlayerCharacter.GetBoneCoord(Bone.SKEL_R_Hand));
                        rope.PinVertex(rope.VertexCount - 1, entity.Position);
                    });
                    PlayerCharacter.PlayGrappleAnim(-1);
                    rope.Delete();
                    ped.SetToRagdoll(-1);
                    GameWaiter.DoWhile(5000, () =>
                    {
                        //if (entity.HasCollidedWithAnything)
                        //    return false;

                        if (Vector3.Distance(entity.Position, PlayerCharacter.Position) < 3f)
                        {
                            // Play punching anim...
                            PunchPed(ped);

                            return false;
                        }

                        if (PlayerCharacter.IsRagdoll)
                            return false;

                        var dir = PlayerCharacter.Position - entity.Position;
                        dir.Normalize();
                        entity.Velocity = dir * 45f;

                        return true;

                    }, null);

                    ped.Task.ClearAll();
                    ped.Task.ClearSecondary();
                    ped.SetToRagdoll(0);
                }
            }
        }

        private void PunchPed(Ped ped)
        {
            PlayerCharacter.Task.PlayAnimation("melee@unarmed@streamed_core", "plyr_takedown_front_uppercut", 
                8.0f, -8.0f, 750, AnimationFlags.AllowRotation, 0.2f);
            ped.FaceEntity(PlayerCharacter);
            ped.Velocity = PlayerCharacter.ForwardVector * 45f;
            var damage = (int)(5000 / 45f);
            ped.ApplyDamage(damage);
        }
    }
}
