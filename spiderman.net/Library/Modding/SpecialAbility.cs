using GTA;

namespace SpiderMan.Library.Modding
{
    /// <summary>
    ///     A class that defines a model for special abilities.
    /// </summary>
    public abstract class SpecialAbility
    {
        /// <summary>
        ///     The local player's character component.
        /// </summary>
        public Ped PlayerCharacter => CoreScript.PlayerCharacter;

        /// <summary>
        ///     This is called when this ability get's updated.
        /// </summary>
        public abstract void Update();

        /// <summary>
        ///     This is called when the script stops.
        /// </summary>
        public abstract void Stop();
    }
}