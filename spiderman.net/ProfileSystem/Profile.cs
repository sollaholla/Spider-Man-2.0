using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GTA;
using SpiderMan.Library.Modding.Stillhere;

namespace SpiderMan.ProfileSystem
{
    /// <summary>
    /// A derivable class that allows for dynamic saving and loading of
    /// properties to an ini file.
    /// </summary>
    public abstract class Profile
    {
        /// <summary>
        /// The main ctor.
        /// </summary>
        /// <param name="path">The path to the ini file.</param>
        protected Profile(string path)
        {
            Path = path;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="path">The path to the ini file.</param>
        /// <param name="localUser">The ped who this profile is attached to.</param>
        protected Profile(string path, Ped localUser) :
            this(path)
        {
            LocalUser = localUser;
        }

        /// <summary>
        /// The ped whom this profile belongs to.
        /// </summary>
        public Ped LocalUser { get; set; }

        /// <summary>
        /// The path to our settings file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Reads property values from the ini at <see cref="Path"/>.
        /// </summary>
        public bool Read()
        {
            if (!File.Exists(Path))
                return false;

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
                GetInfo(property, sp, out var key, out var section, out var pType);

                // Get and set the value.
                try
                {
                    var value = Convert.ChangeType(settings.GetValue(section, key), pType);
                    property.SetValue(this, value);
                }
                catch
                {
                    // ignored
                }
            }

            return true;
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
                GetInfo(property, sp, out var key, out var section, out var _);

                // Get and set the value.
                try
                {
                    settings.SetValue(section, key, property.GetValue(this));
                }
                catch
                {
                    // ignored
                }
            }

            // Save the settings.
            settings.Save();
        }

        /// <summary>
        /// Set's the default values, reads the ini (if any), and then writes back the values.
        /// </summary>
        public void Init()
        {
            SetDefault();
            Read();
            Write();
        }

        /// <summary>
        /// Set the default values for each property.
        /// </summary>
        public abstract void SetDefault();

        /// <summary>
        /// Get's the serialized property attribute from the specified property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected static SerializableProperty TryGetSerializedProperty(PropertyInfo property)
        {
            // Make sure this is a valid property.
            var attributes = property.GetCustomAttributes(typeof(SerializableProperty), false);
            if (attributes.Length <= 0) return null;
            if (!(attributes[0] is SerializableProperty att)) return null;

            // Ensure that this key has a section.
            return !string.IsNullOrEmpty(att.Section) ? att : null;
        }

        /// <summary>
        /// Get's the properties name key, it's section, and the property type.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="att"></param>
        /// <param name="key"></param>
        /// <param name="section"></param>
        /// <param name="pType"></param>
        private static void GetInfo(PropertyInfo property, SerializableProperty att,
            out string key, out string section, out Type pType)
        {
            key = string.IsNullOrEmpty(att.OverrideKey) ? GetNameKey(property.Name, "_", true) : att.OverrideKey;
            section = att.Section;
            pType = property.PropertyType;
        }

        /// <summary>
        /// Returns the settings key form of the property name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="seperator">The seperator to seperate each word.</param>
        /// <param name="toLower">True if you want this string to be cast to lower case.</param>
        /// <returns></returns>
        /// https://stackoverflow.com/questions/18781027/regex-camel-case-to-underscore-ignore-first-occurrence
        protected static string GetNameKey(string propertyName, string seperator, bool toLower)
        {
            var ret = string.Concat(propertyName.Select((x, i) =>
                i > 0 && char.IsUpper(x) ? seperator + x.ToString() : x.ToString()));
            return toLower ? ret.ToLower() : ret;
        }

    }
}