using GTA;
using spiderman.net.Abilities.Attributes;
using System;
using System.Linq;
using System.Reflection;

namespace spiderman.net.Abilities.Types
{
    /// <summary>
    /// A derivable class that allows for dynamic saving and loading of
    /// properties to an ini file.
    /// </summary>
    public abstract class ManagedSettings
    {
        /// <summary>
        /// The path to our settings file.
        /// </summary>
        public virtual string Path => ".\\scripts\\Spider-Man Files\\PlayerProfile.ini";

        /// <summary>
        /// Reads property values from the ini at <see cref="Path"/>.
        /// </summary>
        public void Read()
        {
            // Initialize variables.
            var type = GetType();
            var properties = type.GetProperties();
            var settings = ScriptSettings.Load(Path);

            // Loop through each of our properties.
            foreach (var property in properties)
            {
                // Get the serialized property and
                // validate it.
                var sp = TryGetSerializedProperty(property);
                if (sp == null) continue;
                // Now determine how to read this variable.
                GetInfo(property, sp, out string key, out string section, out Type pType);

                // Get and set the value.
                try
                {
                    var value = Convert.ChangeType(settings.GetValue(section, key), pType);
                    property.SetValue(this, value);
                }
                catch { }
            }
        }

        /// <summary>
        /// Write our property values to the ini at <see cref="Path"/>.
        /// </summary>
        public void Write()
        {
            // Initialize variables.
            var type = GetType();
            var properties = type.GetProperties();
            var settings = ScriptSettings.Load(Path);

            // Loop through each of our properties.
            foreach (var property in properties)
            {
                // Get the serialized property and
                // validate it.
                var sp = TryGetSerializedProperty(property);
                if (sp == null) continue;
                // Now determine how to read this variable.
                GetInfo(property, sp, out string key, out string section, out Type pType);

                // Get and set the value.
                try
                {
                    settings.SetValue(section, key, property.GetValue(this));
                }
                catch { }
            }

            // Save the settings.
            settings.Save();
        }

        public abstract void GetDefault();

        private static SerializableProperty TryGetSerializedProperty(PropertyInfo property)
        {
            // Make sure this is a valid property.
            var attributes = property.GetCustomAttributes(typeof(SerializableProperty), false);
            if (attributes.Length > 0)
            {
                if (attributes[0] is SerializableProperty att)
                {
                    // Ensure that this key has a section.
                    if (!string.IsNullOrEmpty(att.Section))
                    {
                        return att;
                    }
                }
            }

            return null;
        }

        private static void GetInfo(System.Reflection.PropertyInfo property, SerializableProperty att, out string key, out string section, out Type pType)
        {
            key = string.IsNullOrEmpty(att.OverrideKey) ? GetNameKey(property.Name) : att.OverrideKey;
            section = att.Section;
            pType = property.PropertyType;
        }

        /// <summary>
        /// Returns the settings key form of the property name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns></returns>
        /// https://stackoverflow.com/questions/18781027/regex-camel-case-to-underscore-ignore-first-occurrence
        private static string GetNameKey(string propertyName)
        {
            return string.Concat(propertyName.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }
}
