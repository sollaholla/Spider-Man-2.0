using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Modding;
using SpiderMan.ProfileSystem;
using SpiderMan.ProfileSystem.SpiderManScript;

namespace SpiderMan.Abilities.WebTech
{
    //[WebTech("Projectiles", IsDefault = true)]
    public class WebGrenade : Tech
    {
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
        { }

        public override void Process()
        { }
    }
}