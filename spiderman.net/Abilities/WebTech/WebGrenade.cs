using spiderman.net.Abilities.Attributes;
using spiderman.net.Abilities.Types;
using spiderman.net.Library;

namespace spiderman.net.Abilities.WebTech
{
    [WebTech("Projectiles", IsDefault = true)]
    public class WebGrenade : Tech
    {
        public override string Name => "Web Grenade";

        public override string Description => "A timed device that, when triggered, creates a series of webs sticking " +
            "nearby enemies together.";

        public override void Activate()
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Web Grenade.wav", 1.0f);
        }

        public override void Deactivate()
        {
        }

        public override void Process()
        {
        }
    }
}
