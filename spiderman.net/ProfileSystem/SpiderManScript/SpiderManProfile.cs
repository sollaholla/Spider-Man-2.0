using System;
using System.Collections.Generic;
using System.Reflection;
using GTA;
using GTA.Math;
using SpiderMan.Library.Modding.Stillhere;
using SpiderMan.ScriptThreads;

namespace SpiderMan.ProfileSystem.SpiderManScript
{
    public class SpiderManProfile : Profile
    {
        public SpiderManProfile(string path) : base(path)
        {
        }

        /// <summary>
        /// If the local user is not a player then we use
        /// this to override the move vector.
        /// </summary>
        public Vector3 MoveVectorOverride { get; set; }

        /// <summary>
        /// If the local user is not a player then we use this to override the camera rotation.
        /// </summary>
        public Vector3 CameraRotationOverride { get; set; }

        [SerializableProperty("AgilitySettings", "The greater the value, the faster spidey will run.")]
        public float RunSpeedMultiplier { get; set; }

        [SerializableProperty("AgilitySettings", "If true, spidey will do a flip to avoid " +
                                         "hitting a vehicle when performing a web pull.")]
        public bool FlipAfterWebPull { get; set; }

        [SerializableProperty("CombatSettings", "The greater the value, the stronger the force of spidey's hits.")]
        public float PunchForceMultiplier { get; set; }

        [SerializableProperty("CombatSettings", "The greater the value, the more damage spidey does to enemies.")]
        public float PunchDamageMultiplier { get; set; }

        [SerializableProperty("WebbingSettings", "The greater the value, the faster spidey will fling towards" +
                                         "his target when web zipping.")]
        public float WebZipForceMultiplier { get; set; }

        [SerializableProperty("MiscSettings", "The maximum health for this character.")]
        public int MaxHealth { get; set; }

        [SerializableProperty("MiscSettings", "The speed at which spidey's health recharges.")]
        public float HealthRechargeMultiplier { get; set; }

        [SerializableProperty("MiscSettings", "The model to use when this profile is active.")]
        public string Skin { get; set; }

        /// <summary>
        /// Returns the camera direction for the local player.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCameraDirection()
        {
            return LocalUser.IsPlayer
                ? GameplayCamera.Direction
                : Quaternion.Euler(CameraRotationOverride) * Vector3.RelativeFront;
        }

        /// <summary>
        /// Returns the camera position for the local player.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCameraPosition()
        {
            return LocalUser.IsPlayer
                ? GameplayCamera.Position
                : LocalUser.Position;
        }

        /// <summary>
        /// Returns the camera direction for the local player.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCameraRotation()
        {
            return LocalUser.IsPlayer
                ? GameplayCamera.Rotation
                : CameraRotationOverride;
        }

        /// <summary>
        /// Returns the input direction for the local player.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetInputDirection()
        {
            return LocalUser.IsPlayer
                ? new Vector3(Game.GetControlNormal(2, Control.MoveLeftRight),
                    -Game.GetControlNormal(2, Control.MoveUpDown), 0f)
                : MoveVectorOverride;
        }

        public void DisableControls()
        {
            if (!LocalUser.IsPlayer)
                return;

            Controls.DisableControlsKeepRecording(2);
        }

        public override void SetDefault()
        {
            RunSpeedMultiplier = 1f;
            PunchForceMultiplier = 1f;
            PunchDamageMultiplier = 1f;
            WebZipForceMultiplier = 1f;
            MaxHealth = 3000;
            HealthRechargeMultiplier = 10f;
            FlipAfterWebPull = true;
            Skin = "PLAYER_ONE";
            LocalUser = new Ped(0);
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
                var categoryName = GetNameKey(category.Item1, " ", false);
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
                    else if (propertyType.IsEnum)
                    {
                        item = new UIMenuNumberValueItem(propertyName, (Enum)property.GetValue(this));

                        onItemLeftRight = (sender, selectedItem, index, left) =>
                        {
                            if (selectedItem != item)
                                return;

                            var currentValue = (Enum)property.GetValue(this);
                            var values = Enum.GetValues(propertyType);
                            var enumerator = values.GetEnumerator();
                            var valueIndex = 0;
                            while (enumerator.MoveNext())
                            {
                                var v = (Enum)enumerator.Current;
                                if (v != null && Equals(v, currentValue))
                                    break;
                                valueIndex++;
                            }
                            if (left)
                            {
                                valueIndex -= 1;
                                if (valueIndex < 0)
                                    valueIndex = values.Length - 1;
                            }
                            else valueIndex = (valueIndex + 1) % values.Length;
                            var value = values.GetValue(valueIndex);
                            property.SetValue(this, value);
                            item.Value = "< " + value + " >";
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

            var activatePowersButton = new UIMenuItem("Activate Powers No Suit", null,
                "Activate your abilities without using the suit.");
            var activatePowersButton2 = new UIMenuItem("Activate Powers + Equip Suit", null,
                "Activate your powers and equip your suit.");
            var saveButton = new UIMenuItem("Save Settings", null, "Save these settings to " + System.IO.Path.GetFileName(Path));
            subMenu.AddMenuItem(activatePowersButton);
            subMenu.AddMenuItem(activatePowersButton2);
            subMenu.AddMenuItem(saveButton);

            subMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == activatePowersButton)
                    LocalUser = Game.Player.Character;
                if (item == activatePowersButton2)
                {
                    Game.Player.ChangeModel(Skin);
                    LocalUser = Game.Player.Character;
                }

                if (item != saveButton) return;
                Write();
                UI.Notify("Settings saved!");
            };

            return subMenu;
        }

        #endregion
    }
}