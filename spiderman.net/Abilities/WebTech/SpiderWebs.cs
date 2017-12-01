using System.Collections.Generic;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.SpecialAbilities;
using SpiderMan.Abilities.Types;

namespace SpiderMan.Abilities.WebTech
{
    /// <summary>
    ///     The default web mode. Does nothing other than,
    ///     play audio.
    /// </summary>
    [WebTech("Web Mode", IsDefault = true)]
    public class SpiderWebs : Tech
    {
        public SpiderWebs()
        {
            Abilities = new List<SpecialAbility>
            {
                new WebSwing(),
                new WebZip(),
                new WebAttachments()
            };
        }

        public override string Name => "Spider Web";

        public override string Description => "Standard silk webbing.";

        public override void Activate()
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Spider Web.wav", .8f);
        }

        public override void Deactivate()
        {
        }

        public override void Process()
        {
        }
    }
}