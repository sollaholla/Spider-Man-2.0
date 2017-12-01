using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.Abilities.Attributes;
using SpiderMan.Abilities.Types;

namespace SpiderMan.Abilities.SpecialAbilities
{
    public class StarkTech : SpecialAbility
    {
        /// <summary>
        ///     The path to our sprites.
        /// </summary>
        private const string MainPath = ".\\scripts\\Spider-Man Files\\";

        private List<CategorySlot> _slots;

        /// <summary>
        ///     The main weapon wheel.
        /// </summary>
        private readonly Wheel _wheel = new Wheel("Stark-Tech", MainPath,
            0, 0, new Size(64, 64));

        /// <summary>
        ///     The main constructor.
        /// </summary>
        public StarkTech()
        {
            // Create's the weapon wheel.
            CreateWheel();
        }

        /// <summary>
        ///     Create the weapon wheel.
        /// </summary>
        private void CreateWheel()
        {
            // Generate slots for this assembly.
            _slots = GetCategorySlotsFromAssembly(Assembly.GetExecutingAssembly());
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                _wheel.AddCategory(GenerateCategory(slot.ID, slot.CategoryName, slot.Tech));
            }

            // Refresh the menu.
            _wheel.Origin = new Vector2(0.5f, 0.5f);
            _wheel.CalculateCategoryPlacement();

            // Subscribe to the selection events.
            _wheel.OnWheelClose += OnWheelClose;
            _wheel.OnWheelOpen += OnWheelOpen;
            _wheel.WheelName = "Karen";
        }

        /// <summary>
        ///     Loads each type in the assembly, finds all tech, and shoves them into a list
        ///     that defines what categories (and tech) should be defined in the weapon wheel.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static List<CategorySlot> GetCategorySlotsFromAssembly(Assembly assembly)
        {
            var retVal = new List<CategorySlot>();
            var types = assembly.GetTypes();
            var idCount = 0;

            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type.BaseType == typeof(Tech))
                    if (type.GetCustomAttribute(typeof(WebTechAttribute)) is WebTechAttribute att)
                    {
                        var tech = (Tech) Activator.CreateInstance(type);
                        var cat = att.CategoryName;
                        var find = retVal.Find(x => x.CategoryName == cat);

                        if (find == null)
                        {
                            var add = new CategorySlot(cat, idCount, new List<Tech> {tech}, tech);
                            retVal.Add(add);
                            idCount++;
                        }
                        else
                        {
                            find.Tech.Add(tech);
                            find.Tech = find.Tech.OrderByDescending(x => GetWebTechAttribute(x).IsDefault).ToList();
                            if (find.m_ActivateTech != null)
                                find.m_ActivateTech.Deactivate();
                            find.m_ActivateTech = find.Tech[0];
                        }
                    }
            }

            return retVal;
        }

        private static WebTechAttribute GetWebTechAttribute(Tech x)
        {
            return x.GetType().GetCustomAttribute(typeof(WebTechAttribute)) as WebTechAttribute;
        }

        private void OnWheelOpen(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem)
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Karen Hello.wav", 1f);
            _wheel.OnWheelOpen -= OnWheelOpen;
        }

        /// <summary>
        ///     Generates a category from the specified tech (the categoryItem tags will be a pointer to the tech).
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="tech"></param>
        /// <returns></returns>
        private static WheelCategory GenerateCategory(int catID, string categoryName, List<Tech> tech)
        {
            // Keep a cache of our categories.
            var c = new WheelCategory(categoryName) {ID = catID};

            // Loop through each tech object.
            for (var i = 0; i < tech.Count; i++)
            {
                // Get the current web tech.
                var t = tech[i];

                // Add the wheel category item.
                var categoryItem = new WheelCategoryItem(t.Name, t.Description)
                {
                    // Set the tag to our tech item.
                    Tag = t
                };

                // Add the category item.
                c.AddItem(categoryItem);
            }

            return c;
        }

        /// <summary>
        ///     Called when the player selects a new category / item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedCategory"></param>
        /// <param name="selectedItem"></param>
        private void OnWheelClose(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem)
        {
            // Get the selected tech.
            var selectedTech = (Tech) selectedItem.Tag;

            // Make sure this item is tied to some tech.
            if (selectedTech == null)
                return;

            foreach (var categorySlot in _slots)
                if (categorySlot.ID == selectedCategory.ID)
                    SetTech(selectedTech, ref categorySlot.m_ActivateTech);
        }

        private static void SetTech(Tech selectedTech, ref Tech tech)
        {
            // Check if the tech was changed.
            if (tech == null)
            {
                // Set and activate the tech.
                tech = selectedTech;
                selectedTech.Activate();
            }
            else
            {
                // Our tech needs to be switched and activated.
                if (tech != selectedTech)
                {
                    StopTechAbilties(tech);
                    tech.Deactivate();
                    tech = selectedTech;
                    selectedTech.Activate();
                }
            }
        }

        /// <summary>
        ///     Called once each frame.
        /// </summary>
        public override void Update()
        {
            // Update the weapon wheel logic.
            WeaponWheelLogic();

            // Make sure we're not updating these while the weapon
            // wheel is visible.
            while (_wheel.Visible)
            {
                WeaponWheelLogic();
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                Script.Yield();
            }

            foreach (var slot in _slots)
                ProcessTech(slot.m_ActivateTech);
        }

        /// <summary>
        ///     Process the tech and it's abilties.
        /// </summary>
        /// <param name="t"></param>
        private void ProcessTech(Tech t)
        {
            if (t == null)
                return;

            t.Process();

            if (t.Abilities == null)
                return;

            foreach (var a in t.Abilities)
                a.Update();
        }

        private static void StopTechAbilties(Tech tech)
        {
            if (tech == null) return;
            if (tech.Abilities == null) return;
            foreach (var a in tech.Abilities)
                a?.Stop();
        }

        /// <summary>
        ///     Everything related to the weapon wheel.
        /// </summary>
        private void WeaponWheelLogic()
        {
            // If the player is dead, then hide the weapon
            // wheel.
            if (PlayerCharacter.IsDead)
                _wheel.Visible = false;

            // Check if we've pressed the weapon selection control.
            if (Game.IsDisabledControlPressed(2, Control.SelectWeapon))
            {
                // Show the weapon select menu.
                if (!_wheel.Visible)
                    _wheel.Visible = true;
            }
            // Otherwise...
            else
            {
                // Hide the weapon select menu.
                if (_wheel.Visible)
                    _wheel.Visible = false;
            }

            // If the weapon select menu is visible, we want to draw
            // the back-drop.
            if (_wheel.Visible)
                DrawBackground();

            // Process our weapon selection wheel.
            _wheel.ProcessSelectorWheel();
        }

        /// <summary>
        ///     Draw's the background image while the weapon wheel is open.
        /// </summary>
        private void DrawBackground()
        {
            var screenRes = GTAGraphics.GetScreenResolution();
            var hudbgSize = new Size(512, 512);
            //UI.DrawTexture(MainPath + "hudbg.png", 0, 1, 60, GetCenterPointForImage(screenRes, hudbgSize), hudbgSize);
            UI.DrawTexture(MainPath + "spideyhud.png", 28, 0, 60, Point.Empty, screenRes);
        }

        /// <summary>
        ///     Returns the centered position on the screen for the given image.
        /// </summary>
        /// <param name="screenRes">The screen resolution.</param>
        /// <param name="imageSize">The size of the image.</param>
        /// <returns></returns>
        private static Point GetCenterPointForImage(Size screenRes, Size imageSize)
        {
            return new Point(
                screenRes.Width / 2 - imageSize.Width / 2,
                screenRes.Height / 2 - imageSize.Height / 2);
        }

        /// <summary>
        ///     Called when the script stops.
        /// </summary>
        public override void Stop()
        {
            // Deactivate our tech when we stop the script.
            //FullyDeactivateTech(_currentSuitMode);
            //FullyDeactivateTech(_currentTargettingMode);
            //FullyDeactivateTech(_currentWebMode);
            foreach (var slot in _slots)
                FullyDeactivateTech(slot.m_ActivateTech);
        }

        /// <summary>
        ///     Deactivates the tech and all of it's abilities.
        /// </summary>
        /// <param name="t"></param>
        private void FullyDeactivateTech(Tech t)
        {
            t?.Deactivate();
            t?.Abilities?.ForEach(x => x.Stop());
        }
    }
}