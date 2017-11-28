using spiderman.net.Library;
using SelectorWheel;
using GTA.Math;
using GTA;
using System.Drawing;
using System.Collections.Generic;
using spiderman.net.Abilities.WebTech;
using System;

namespace spiderman.net.Abilities
{
    public class StarkTech : SpecialAbility
    {
        /// <summary>
        /// The path to our sprites.
        /// </summary>
        private const string MainPath = ".\\scripts\\Spider-Man Files\\";

        /// <summary>
        /// The main weapon wheel.
        /// </summary>
        private Wheel _wheel = new Wheel("Stark-Tech", MainPath,
            0, 0, new Size(64, 64));

        private List<Tech> _suitModes = new List<Tech> {
            new TrainingWheelsProtocol(),
            new InstantKill()
        };
        private Tech _currentSuitMode;

        private List<Tech> _webModes = new List<Tech> {
            new SpiderWebs(),
            new TazerWebs()
        };
        private Tech _currentWebMode;

        private List<Tech> _targettingModes = new List<Tech> {
            new MultiDisarm()
        };
        private Tech _currentTargettingMode;

        /// <summary>
        /// The main constructor.
        /// </summary>
        public StarkTech()
        {
            // Create's the weapon wheel.
            CreateWheel();
        }

        /// <summary>
        /// Create the weapon wheel.
        /// </summary>
        private void CreateWheel()
        {
            // Add the suit category.
            _wheel.AddCategory(GenerateCategory(0, "Suit Mode", _suitModes));
            _currentSuitMode = _suitModes[0];
            _wheel.AddCategory(GenerateCategory(1, "Web Mode", _webModes));
            _currentWebMode = _webModes[0];
            _wheel.AddCategory(GenerateCategory(2, "Targetting Mode", _targettingModes));
            _currentTargettingMode = _targettingModes[0];

            // Refresh the menu.
            _wheel.Origin = new Vector2(0.5f, 0.5f);
            _wheel.CalculateCategoryPlacement();

            // Subscribe to the selection events.
            _wheel.OnWheelClose += OnWheelClose;
            _wheel.OnWheelOpen += OnWheelOpen;
            _wheel.WheelName = "Karen";
        }

        private void OnWheelOpen(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem)
        {
            AudioPlayer.PlaySound(AudioPlayer.MainPath + "Karen Hello.wav", 1f);
            _wheel.OnWheelOpen -= OnWheelOpen;
        }

        /// <summary>
        /// Generates a category from the specified tech (the categoryItem tags will be a pointer to the tech).
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="tech"></param>
        /// <returns></returns>
        private static WheelCategory GenerateCategory(int catID, string categoryName, List<Tech> tech)
        {
            // Keep a cache of our categories.
            var c = new WheelCategory(categoryName) { ID = catID };

            // Loop through each tech object.
            for (int i = 0; i < tech.Count; i++)
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
        /// Called when the player selects a new category / item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="selectedCategory"></param>
        /// <param name="selectedItem"></param>
        private void OnWheelClose(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem)
        {
            // Get the selected tech.
            var selectedTech = (Tech)selectedItem.Tag;

            // Make sure this item is tied to some tech.
            if (selectedTech == null)
                return;

            // Set the current tech mode based on category ID.
            switch(selectedCategory.ID)
            {
                case 0:
                    SetTech(selectedTech, ref _currentSuitMode);
                    break;
                case 1:
                    SetTech(selectedTech, ref _currentWebMode);
                    break;
                case 2:
                    SetTech(selectedTech, ref _currentTargettingMode);
                    break;
            }
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
        /// Called once each frame.
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
                Script.Yield();
            }

            ProcessTech(_currentWebMode);
            ProcessTech(_currentSuitMode);
            ProcessTech(_currentTargettingMode);
        }

        /// <summary>
        /// Process the tech and it's abilties.
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
        /// Everything related to the weapon wheel.
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
        /// Draw's the background image while the weapon wheel is open.
        /// </summary>
        private void DrawBackground()
        {
            var screenRes = Library.GTAGraphics.GetScreenResolution();
            var hudbgSize = new Size(512, 512);
            //UI.DrawTexture(MainPath + "hudbg.png", 0, 1, 60, GetCenterPointForImage(screenRes, hudbgSize), hudbgSize);
            UI.DrawTexture(MainPath + "spideyhud.png", 28, 0, 60, Point.Empty, screenRes);
        }
        
        /// <summary>
        /// Returns the centered position on the screen for the given image.
        /// </summary>
        /// <param name="screenRes">The screen resolution.</param>
        /// <param name="imageSize">The size of the image.</param>
        /// <returns></returns>
        private static Point GetCenterPointForImage(Size screenRes, Size imageSize)
        {
            return new Point(
                (screenRes.Width / 2) - (imageSize.Width / 2),
                (screenRes.Height / 2) - (imageSize.Height / 2));
        }

        /// <summary>
        /// Called when the script stops.
        /// </summary>
        public override void Stop()
        {
            // Deactivate our tech when we stop the script.
            FullyDeactivateTech(_currentSuitMode);
            FullyDeactivateTech(_currentTargettingMode);
            FullyDeactivateTech(_currentWebMode);
        }

        /// <summary>
        /// Deactivates the tech and all of it's abilities.
        /// </summary>
        /// <param name="t"></param>
        private void FullyDeactivateTech(Tech t)
        {
            t?.Deactivate();
            t?.Abilities?.ForEach(x => x.Stop());
        }
    }
}
