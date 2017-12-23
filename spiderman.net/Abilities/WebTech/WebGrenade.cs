using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.SpecialAbilities.PlayerOnly;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Extensions;
using SpiderMan.Library.Modding;
using SpiderMan.Library.Types;
using SpiderMan.ProfileSystem.SpiderManScript;
using SpiderMan.ScriptThreads;
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.WebTech
{
    [WebTech("Projectiles", IsDefault = true)]
    public class WebGrenade : Tech
    {
        private readonly List<Grenade> _currentGrenades = new List<Grenade>();

        private float _grenadeCooldown;

        public WebGrenade(SpiderManProfile profile) : base(profile)
        {
            new Model("w_ex_grenadefrag").Request();
        }

        public override string Name => "Web Grenade";

        public override string Description =>
            "A timed device that, when triggered, creates a series of webs sticking " +
            "nearby enemies together.";

        public override void Activate()
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Web Grenade.wav", 1.0f);
        }

        public override void Deactivate()
        {
            foreach (var grenade in _currentGrenades)
            {
                grenade?.Delete();
            }
        }

        public override void Process()
        {
            UpdateGrenades();

            if (_grenadeCooldown > 0f)
            {
                _grenadeCooldown -= Game.LastFrameTime;
                return;
            }

            Game.DisableControlThisFrame(2, Control.ThrowGrenade);
            if (!Game.IsDisabledControlJustReleased(2, Control.ThrowGrenade))
                return;
            PlayerCharacter.Heading = GameplayCamera.Direction.ToHeading();
            PlayerCharacter.PlayGrappleAnim(1f);
            var grenade = World.CreateProp("w_ex_grenadefrag", PlayerCharacter.Position, false, false);
            if (grenade == null)
                return;
            grenade.AttachTo(PlayerCharacter, PlayerCharacter.GetBoneIndex(Bone.SKEL_R_Hand), new Vector3(0.1f, 0f, -0.04f), new Vector3(-90, 0, 0));
            var timer = 0.25f;
            while (timer > 0f)
            {
                PlayerCharacter.Heading = GameplayCamera.Direction.ToHeading();
                timer -= Time.DeltaTime;
                Script.Yield();
            }
            grenade.Detach();
            grenade.Velocity = GameplayCamera.Direction * 75f + PlayerCharacter.Velocity + Vector3.WorldUp * 2.5f;
            _currentGrenades.Add(new Grenade(4f, grenade));
            _grenadeCooldown = 5f;
        }

        private void UpdateGrenades()
        {
            for (var i = 0; i < _currentGrenades.Count; i++)
            {
                var grenade = _currentGrenades[i];
                grenade.Timer -= Game.LastFrameTime;

                if (!(grenade.Timer <= 0) && !grenade.Prop.HasCollidedWithAnything) continue;
                if (grenade.Exploded) continue;

                grenade.Explode();

                _currentGrenades.ForEach(x =>
                {
                    if (x == grenade)
                        return;
                    x.Delete();
                });
            }
        }
    }

    public class Grenade
    {
        private readonly List<AttachmentInfo> _attachments = new List<AttachmentInfo>();

        public Grenade(float timer, Prop prop)
        {
            Timer = timer;
            Prop = prop;
        }

        public float Timer { get; set; }

        public Prop Prop { get; set; }

        public bool Exploded { get; private set; }

        public void Delete()
        {
            Prop.Delete();
            foreach (var attachment in _attachments)
                attachment?.Delete();
        }

        public void Explode()
        {
            if (Exploded) return;
            Exploded = true;
            var entities = World.GetNearbyEntities(Prop.Position, 20f);
            Prop.Position = new Vector3(Prop.Position.X, Prop.Position.Y, World.GetGroundHeight(Prop.Position));
            Prop.FreezePosition = true;
            foreach (var ent in entities)
            {
                if (ent.GetEntityType() == EntityType.Object) continue;
                if (ent.Handle == Game.Player.Character.Handle) continue;
                if (ent.IsDead) continue;
                //Utilities.CreateParticleChain(Prop.Position, ent.Position, Vector3.Zero, 0.1f, particleScale: 0.2f);
                if (ent is Ped ped)
                {
                    if (ped.IsInVehicle())
                        continue;
                    Utilities.ShockPed(ped, 100);
                }
                var length = Vector3.Distance(ent.Position, Prop.Position);
                var rope = Rope.AddRope(Prop.Position, length, GTARopeType.ThickRope, length / 2, 0.1f, true, false);
                rope.AttachEntities(Prop, Vector3.Zero, ent, Vector3.Zero, length / 2);
                rope.ActivatePhysics();
                _attachments.Add(new AttachmentInfo(ent, Prop, rope));
                WebAttachments.AddAttachment(ent, Prop, rope, true);
            }
            GTAGraphics.StartParticle("core", "ent_ray_prologue_elec_crackle_sp", Prop.Position, Vector3.Zero, 5.0f);
            World.DrawLightWithRange(Prop.Position, Color.White, 25f, 100f);
            World.AddOwnedExplosion(Game.Player.Character, Prop.Position, ExplosionType.Grenade, 0f, 0.1f, true, false);
        }
    }
}