using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.Types;
using SpiderMan.Library.Modding;

namespace SpiderMan.Abilities.WebTech
{
    /// <summary>
    ///     The default suit mode for Peter.
    /// </summary>
    [WebTech("Suit Mode", IsDefault = true)]
    public class TrainingWheelsProtocol : Tech
    {
        /// <summary>
        ///     The name of the tech.
        /// </summary>
        public override string Name => "Training Wheels";

        /// <summary>
        ///     The description of the tech.
        /// </summary>
        public override string Description => "The default suit mode.";

        /// <summary>
        ///     Called when the tech is activated.
        /// </summary>
        public override void Activate()
        {
            // Play the activation sound effect.
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "T.W.P.wav", 1.0f);
        }

        /// <summary>
        ///     Called when the tech is deactivated.
        /// </summary>
        public override void Deactivate()
        {
        }

        /// <summary>
        ///     Called when the tech needs to be updated.
        /// </summary>
        public override void Process()
        {
        }
    }
}