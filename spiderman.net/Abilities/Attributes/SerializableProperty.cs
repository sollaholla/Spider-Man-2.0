using System;

namespace spiderman.net.Abilities.Attributes
{
    /// <summary>
    /// Allows you to define a property which will be saved
    /// to script settings. Used by the <see cref="ManagedSettings"/> class.
    /// </summary>
    public class SerializableProperty : Attribute
    {
        public SerializableProperty(string section)
        {
            Section = section;
        }

        /// <summary>
        /// The section of the ini to place this property and
        /// it's value.
        /// </summary>
        public string Section { get; }

        /// <summary>
        /// Allows you to override the name of this
        /// properties key instead of using the property name.
        /// </summary>
        public string OverrideKey { get; set; }
    }
}
