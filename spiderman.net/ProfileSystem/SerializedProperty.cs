using System;

namespace SpiderMan.ProfileSystem
{
    /// <summary>
    /// Allows you to define a property which will be saved
    /// to script settings. Used by the <see cref="Profile"/> class.
    /// </summary>
    public class SerializableProperty : Attribute
    {
        public SerializableProperty(string section)
        {
            Section = section;
        }

        public SerializableProperty(string section, string description) : 
            this(section)
        {
            Description = description;
        }

        /// <summary>
        /// The section of the ini to place this property and
        /// it's value.
        /// </summary>
        public string Section { get; }

        /// <summary>
        /// The description of this property.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Allows you to override the name of this
        /// properties key instead of using the property name.
        /// </summary>
        public string OverrideKey { get; set; }
    }
}