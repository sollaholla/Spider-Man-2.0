using System.Collections.Generic;
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
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.WebTech
{
    [WebTech("Projectiles", IsDefault = true)]
    public class WebGrenade : Tech
    {
        private readonly List<Grenade> _currentGrenades = new List<Grenade>();

        private float _grenadeCooldown;

        public WebGrenade(SpiderManProfile profile) : base(profile)
        { }

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
            grenade.AttachTo(PlayerCharacter, PlayerCharacter.GetBoneIndex(Bone.SKEL_R_Hand));
            GameWaiter.Wait(200);
            grenade.Detach();
            grenade.Velocity = GameplayCamera.Direction * 45f + PlayerCharacter.Velocity;
            _currentGrenades.Add(new Grenade(4f, grenade));
            _grenadeCooldown = 20f;
        }

        private void UpdateGrenades()
        {
            foreach (var grenade in _currentGrenades)
            {
                grenade.Timer -= Game.LastFrameTime;
                if (grenade.Timer <= 0)
                {
                    grenade.Explode();
                }
            }
        }
    }

    public class Grenade
    {
        private readonly List<AttachmentInfo> _attachments = new List<AttachmentInfo>();

        private bool _didExplode;

        public Grenade(float timer, Prop prop)
        {
            Timer = timer;
            Prop = prop;
        }

        public float Timer { get; set; }

        public Prop Prop { get; set; }

        public void Delete()
        {
            Prop.Delete();
            foreach (var attachment in _attachments)
                attachment?.Delete();
        }

        public void Explode()
        {
            if (_didExplode) return;
            _didExplode = true;
            var entities = World.GetNearbyEntities(Prop.Position, 20f);
            foreach (var ent in entities)
            {
                if (ent.GetEntityType() == EntityType.Object) continue;
                if (ent.Handle == Game.Player.Character.Handle) continue;
                var length = Vector3.Distance(ent.Position, Prop.Position);
                var rope = Rope.AddRope(Prop.Position, length, GTARopeType.ThickRope, length / 2, 0.1f, true, false);
                rope.AttachEntities(Prop, Vector3.Zero, ent, Vector3.Zero, length / 2);
                rope.ActivatePhysics();
                _attachments.Add(new AttachmentInfo(ent, Prop, rope));
                WebAttachments.AddAttachment(ent, Prop, rope);
            }
            GTAGraphics.StartParticle("core", "ent_ray_prologue_elec_crackle_sp", Prop.Position, Vector3.Zero, 1.0f);
            Prop.IsVisible = false;
            //Object object, float weight, float p2,
            //float p3, float p4, float p5, float gravity, float p7,
            //float p8, float p9, float p10, float buoyancy
            Function.Call(Hash.SET_OBJECT_PHYSICS_PARAMS, Prop.Handle, 10000f);
        }
    }
}