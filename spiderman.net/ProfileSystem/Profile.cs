using System;
using System.Collections.Generic;
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
        /// Set the default values for each property.
        /// </summary>
        public abstract void SetDefault();

        /// <summary>
        /// Get's the serialized property attribute from the specified property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static SerializableProperty TryGetSerializedProperty(PropertyInfo property)
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
        private static string GetNameKey(string propertyName, string seperator, bool toLower)
        {
            var ret = string.Concat(propertyName.Select((x, i) =>
                i > 0 && char.IsUpper(x) ? seperator + x.ToString() : x.ToString()));
            return toLower ? ret.ToLower() : ret;
        }

        #region SIMPLE_UI_IMPLEMENTATIONS

        /// <summary>
        /// Add the settings properties to the specified menu. Also allows you
        /// to manipulate the properties without any extra work.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="pool"></param>
        internal UIMenu AddToMenu(UIMenu menu, MenuPool pool)
        {
            var menuName = System.IO.Path.GetFileNameWithoutExtension(Path);
            menuName = GetNameKey(menuName, " ", false);
            var subMenu = new UIMenu(menuName);

            var properties = GetType().GetProperties();
            var categories = new List<Tuple<string, List<PropertyInfo>>>();

            foreach (var property in properties)
            {
                var serializedProperty = TryGetSerializedProperty(property);
                if (serializedProperty == null) continue;

                var find = categories.Find(x => x.Item1 == serializedProperty.Section);
                if (find == null)
                    // Then we add it to the categories.
                    categories.Add(new Tuple<string, List<PropertyInfo>>(serializedProperty.Section,
                        new List<PropertyInfo> { property }));
                else
                    // We just add it to the properties for that category.
                    find.Item2.Add(property);
            }

            // Now that we have the categories and there properties, we're
            // going to create menu items for them, and bind them to 
            // the properties themselves.
            foreach (var category in categories)
            {
                var categoryName = GetNameKey(category.Item1, " ", false) + " Settings";
                var categoryMenu = new UIMenu(categoryName);

                foreach (var property in category.Item2)
                {
                    // Now we need to create the category menu items, 
                    // and add them to this categories' menu.
                    var propertyType = property.PropertyType;

                    // Instead of defining each menu item within the 
                    // type checks we're going to initialize one
                    // here to get more performance out of this.
                    var propertyName = GetNameKey(property.Name, " ", false);
                    UIMenuItem item = null;
                    ItemLeftRightEvent onItemLeftRight = null;
                    ItemSelectEvent onItemSelect = null;

                    if (propertyType == typeof(int))
                    {
                        // Then we want a number selector. Same 
                        // for float.
                        item = new UIMenuNumberValueItem(propertyName, (int)property.GetValue(this));

                        // Now, just like the next types, we need
                        // to define our subscription to the onItemLeftRight
                        // delegate.
                        var property1 = property;
                        onItemLeftRight = (sender, selectedItem, index, left) =>
                        {
                            if (selectedItem != item)
                                return;

                            var currentValue = (int)property1.GetValue(this);
                            if (left) currentValue -= 1;
                            else currentValue += 1;
                            property1.SetValue(this, currentValue);
                            item.Value = "< " + currentValue + " >";
                        };

                        onItemSelect = (sender, selectedItem, index) =>
                        {
                            if (selectedItem != item)
                                return;

                            var input = Game.GetUserInput(property.GetValue(this).ToString(), 999);
                            if (string.IsNullOrEmpty(input)) return;
                            if (!int.TryParse(input, out var res)) return;
                            property.SetValue(this, res);
                            item.Value = "< " + res + " >";
                            Script.Wait(100);
                        };
                    }
                    else if (propertyType == typeof(float))
                    {
                        item = new UIMenuNumberValueItem(propertyName, (float)property.GetValue(this));

                        onItemLeftRight = (sender, selectedItem, index, left) =>
                        {
                            if (selectedItem != item)
                                return;

                            var currentValue = (float)property.GetValue(this);
                            if (left) currentValue -= 1f;
                            else currentValue += 1f;
                            property.SetValue(this, currentValue);
                            item.Value = "< " + currentValue + " >";
                        };

                        onItemSelect = (sender, selectedItem, index) =>
                        {
                            if (selectedItem != item)
                                return;

                            var input = Game.GetUserInput(property.GetValue(this).ToString(), 999);
                            if (string.IsNullOrEmpty(input)) return;
                            if (!float.TryParse(input, out var res)) return;
                            property.SetValue(this, res);
                            item.Value = "< " + res + " >";
                            Script.Wait(100);
                        };
                    }
                    else if (propertyType == typeof(string))
                    {
                        item = new UIMenuItem(propertyName, "\"" + (string)property.GetValue(this) + "\"");

                        onItemSelect = (sender, selectedItem, index) =>
                        {
                            if (selectedItem != item)
                                return;

                            var input = Game.GetUserInput(property.GetValue(this).ToString(), 999);
                            if (string.IsNullOrEmpty(input)) return;

                            property.SetValue(this, input);
                            item.Value = "\"" + input + "\"";
                        };
                    }
                    else if (propertyType == typeof(bool))
                    {
                        item = new UIMenuItem(propertyName, (bool)property.GetValue(this));

                        onItemSelect = (sender, selectedItem, index) =>
                        {
                            if (selectedItem != item)
                                return;

                            var value = (bool)property.GetValue(this);
                            property.SetValue(this, !value);
                            item.Value = !value;
                        };
                    }

                    if (item == null) continue;
                    var serializedProperty = TryGetSerializedProperty(property);
                    item.Description = serializedProperty.Description;
                    categoryMenu.OnItemLeftRight += onItemLeftRight;
                    categoryMenu.OnItemSelect += onItemSelect;

                    // Now let's add it to this categories' menu.
                    categoryMenu.AddMenuItem(item);
                }
                pool.AddSubMenu(categoryMenu, subMenu, categoryName);
            }
            pool.AddSubMenu(subMenu, menu, menuName);

            var saveButton = new UIMenuItem("Save Settings", null, "Save these settings to " + System.IO.Path.GetFileName(Path));
            subMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item != saveButton) return;
                Write();
                UI.Notify("Settings saved!");
            };
            subMenu.AddMenuItem(saveButton);

            return subMenu;
        }

        #endregion
    }
}