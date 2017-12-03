using System.Collections.Generic;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.SpecialAbilities;
using SpiderMan.Abilities.SpecialAbilities.PlayerOnly;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Modding;
using SpiderMan.ProfileSystem;
using SpiderMan.ProfileSystem.SpiderManScript;

namespace SpiderMan.Abilities.WebTech
{
    /// <summary>
    ///     The default web mode. Does nothing other than,
    ///     play audio.
    /// </summary>
    [WebTech("Web Mode", IsDefault = true)]
    public class SpiderWebs : Tech
    {
        public SpiderWebs(SpiderManProfile profile) : 
            base(profile)
        {
            Abilities = new List<SpecialAbility>
            {
                new WebSwing(profile),
                new WebZip(profile),
                new WebAttachments(profile)
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