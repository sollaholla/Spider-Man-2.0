using System.Collections.Generic;
using GTA;
using SpiderMan.Library.Modding;

namespace SpiderMan.Abilities.Types
{
    /// <summary>
    ///     Defines how the structure of Stark Tech works.
    /// </summary>
    public abstract class Tech
    {
        /// <summary>
        ///     The abilities that will run with this tech activated.
        /// </summary>
        public List<SpecialAbility> Abilities = new List<SpecialAbility>();

        /// <summary>
        ///     The player's ped component.
        /// </summary>
        public Ped PlayerCharacter => CoreScript.PlayerCharacter;

        /// <summary>
        ///     The name of this tech tech.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     The tech abilities description.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        ///     Runs when the tech is initialized.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        ///     Runs when the tech is updated.
        /// </summary>
        public abstract void Process();

        /// <summary>
        ///     Runs when the tech is stopped.
        /// </summary>
        public abstract void Deactivate();
    }
}