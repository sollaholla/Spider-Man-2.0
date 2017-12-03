using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
using SpiderMan.ScriptThreads;
using Font = GTA.Font;

#pragma warning disable 1587
/// <summary>
/// Credits: stillhere (LfxB)
/// https://github.com/LfxB/GTA-V-Selection-Wheel/blob/master/SelectorWheel/SelectorWheel.cs
/// </summary>
#pragma warning restore 1587
namespace SpiderMan.Library.Modding.Stillhere
{
    public class Wheel
    {
        private const float deadzone = 0.001f;

        private bool _visible;

        private float aspectRatio = Game.ScreenResolution.Width / (float) Game.ScreenResolution.Height / 2f;
        private readonly string AUDIO_SELECTSOUND = "HIGHLIGHT_NAV_UP_DOWN";

        private readonly string AUDIO_SOUNDSET = "HUD_FRONTEND_DEFAULT_SOUNDSET";
        protected List<WheelCategory> Categories = new List<WheelCategory>();

        private readonly List<Control> ControlsToEnable = new List<Control>
        {
            /*Control.FrontendAccept,
            Control.FrontendAxisX,
            Control.FrontendAxisY,
            Control.FrontendDown,
            Control.FrontendUp,
            Control.FrontendLeft,
            Control.FrontendRight,
            Control.FrontendCancel,
            Control.FrontendSelect,
            Control.CharacterWheel,
            Control.CursorScrollDown,
            Control.CursorScrollUp,
            Control.CursorX,
            Control.CursorY,*/
            Control.MoveUpDown,
            Control.MoveLeftRight,
            Control.Sprint,
            Control.Jump,
            Control.Enter,
            Control.VehicleExit,
            Control.VehicleAccelerate,
            Control.VehicleBrake,
            Control.VehicleMoveLeftRight,
            Control.VehicleFlyYawLeft,
            Control.FlyLeftRight,
            Control.FlyUpDown,
            Control.VehicleFlyYawRight,
            Control.VehicleHandbrake,
            Control.WeaponWheelLeftRight,
            Control.WeaponWheelUpDown
            /*Control.VehicleRadioWheel,
            Control.VehicleRoof,
            Control.VehicleHeadlight,
            Control.VehicleCinCam,
            Control.Phone,
            Control.MeleeAttack1,
            Control.MeleeAttack2,
            Control.Attack,
            Control.Attack2
            Control.LookUpDown,
            Control.LookLeftRight*/
        };

        public int CurrentCatIndex;
        private bool HaveTexturesBeenCached;
        private float inputAngle = 265f;
        private Vector2 inputCoord = Vector2.Zero;
        private readonly float Radius = 250;
        private readonly string TexturePath;
        private readonly int TextureRefreshRate;
        private readonly Size TextureSize = new Size(30, 30);
        private float timecycleCurrentStrength;

        /// <summary>
        ///     https://pastebin.com/kVPwMemE
        /// </summary>
        public string TimecycleModifier = "hud_def_desat_Neutral";

        public float TimecycleModifierStrength = 1.0f;

        private float timeScale = 1f;

        private bool transitionIn;
        private bool transitionOut;

        private readonly bool UseTextures;
        private readonly int xTextureOffset;
        private readonly int yTextureOffset;

        /// <summary>
        ///     Instantiates a simple Selection Wheel that does not use textures. Just displays the category and item names.
        /// </summary>
        /// <param name="name">
        ///     Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it
        ///     yet.
        /// </param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, float wheelRadius = 250)
        {
            WheelName = name;
            Radius = wheelRadius;
        }

        /// <summary>
        ///     Instantiates a Selection Wheel that uses textures for categories or items, if they exist.
        /// </summary>
        /// <param name="name">
        ///     Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it
        ///     yet.
        /// </param>
        /// <param name="texturePath">Path where category and item .png files are kept. Ex: @"scripts\SelectorWheelExample\"</param>
        /// <param name="xtextureOffset">Simple X offset, usually set to 0.</param>
        /// <param name="ytextureOffset">Simple Y offset, usually set to 0.</param>
        /// <param name="textureSize">Size of images (in pixels).</param>
        /// <param name="textureRefreshRate">How long (in ms) each texture will be displayed for.</param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, string texturePath, int xtextureOffset, int ytextureOffset, Size textureSize,
            int textureRefreshRate = 50, float wheelRadius = 250)
        {
            WheelName = name;
            UseTextures = true;
            TexturePath = texturePath;
            xTextureOffset = xtextureOffset;
            yTextureOffset = ytextureOffset;
            TextureSize = textureSize;
            TextureRefreshRate = textureRefreshRate;
            Radius = wheelRadius;
        }

        public string WheelName { get; set; }

        /// <summary>
        ///     Show/Hide the selection wheel.
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set
            {
                //start and end screen effects, etc. before toggling.

                if (_visible == false && value) //When the wheel is just opened.
                {
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER, TimecycleModifier);
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, timecycleCurrentStrength);
                    transitionIn = true;
                    transitionOut = false;

                    CalculateCategoryPlacement();
                    aspectRatio = Game.ScreenResolution.Width / (float) Game.ScreenResolution.Height / 2f;

                    CategoryChange(SelectedCategory, SelectedCategory.SelectedItem);
                    ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                    WheelOpen(SelectedCategory, SelectedCategory.SelectedItem);
                }
                else if (_visible && value == false) //When the wheel is just closed.
                {
                    transitionIn = false;
                    transitionOut = true;

                    WheelClose(SelectedCategory, SelectedCategory.SelectedItem);
                    foreach (var cat in Categories)
                    {
                        if (cat.CategoryTexture != null)
                            cat.CategoryTexture.StopDraw();
                        if (cat.SelectedItem.ItemTexture != null)
                            cat.SelectedItem.ItemTexture.StopDraw();
                    }
                }

                _visible = value;
            }
        }

        /// <summary>
        ///     In screen percentage.
        ///     X: 0.5f = 50% from the left.
        ///     Y: 0.5f = 50% from the top.
        ///     Set this before calling CalculateCategoryPlacement() or it won't apply.
        /// </summary>
        public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.4f);

        private Vector2 OriginInPixels =>
            new Vector2(UIHelper.XPercentageToPixel(Origin.X), UIHelper.YPercentageToPixel(Origin.Y));

        private WheelCategory SelectedCategory => Categories[CurrentCatIndex];

        /// <summary>
        ///     Called when user hovers over a new category.
        /// </summary>
        public event CategoryChangeEvent OnCategoryChange;

        /// <summary>
        ///     Called when user switches to a new item.
        /// </summary>
        public event ItemChangeEvent OnItemChange;

        /// <summary>
        ///     Called when user opens the wheel.
        /// </summary>
        public event WheelOpenEvent OnWheelOpen;

        /// <summary>
        ///     Called when user closes the wheel.
        /// </summary>
        public event WheelCloseEvent OnWheelClose;

        /// <summary>
        ///     Must be placed in your Tick method.
        /// </summary>
        public void ProcessSelectorWheel()
        {
            ControlTransitions();
            if (!Visible) return;

            DisableControls();
            ControlCategorySelection();
            ControlItemSelection();
        }

        /// <summary>
        ///     Call this after you have already added some categories (max is 18 categories).
        ///     This function will check the amount of categories and situate them around the origin of the screen.
        /// </summary>
        public void CalculateCategoryPlacement()
        {
            /* 0f is on the middle-right, and it moves clockwise.
             * i.e. 270f is directly up.
             * */

            switch (Categories.Count)
            {
                case 1:
                {
                    Categories[0].position2D = PointOnCircleInPercentage(Radius, 270f, OriginInPixels);
                    break;
                }
                case 4:
                {
                    CalculateFromStartAngle(225f, 4);
                    break;
                }
                default:
                {
                    CalculateFromStartAngle(270f, Categories.Count);
                    break;
                }
            }

            if (!HaveTexturesBeenCached)
            {
                foreach (var cat in Categories)
                {
                    if (File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png"))
                        cat.CategoryTexture = new Texture2D(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png",
                            Categories.IndexOf(cat));
                    foreach (var item in cat.ItemList)
                        if (File.Exists(TexturePath + UIHelper.MakeValidFileName(item.Name) + ".png"))
                            item.ItemTexture =
                                new Texture2D(TexturePath + UIHelper.MakeValidFileName(item.Name) + ".png",
                                    Categories.IndexOf(cat) /*cat.ItemList.IndexOf(item)*/);

                    /*Load textures into cache*/
                    if (cat.CategoryTexture != null)
                        cat.CategoryTexture.LoadTexture();
                    foreach (var item in cat.ItemList)
                        if (item.ItemTexture != null)
                            item.ItemTexture.LoadTexture();
                }

                HaveTexturesBeenCached = true;
            }
        }

        private void CalculateFromStartAngle(float startAngle, int numCategories)
        {
            float angleOffset = 360 / numCategories;
            for (var i = 0; i < numCategories; i++)
            {
                Categories[i].position2D = PointOnCircleInPercentage(Radius, startAngle, OriginInPixels);
                startAngle += angleOffset;
            }
        }

        private float AddXPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.XPixelToPercentage
            (
                (int) UIHelper.XPercentageToPixel(percent) + pixelDist
            );
        }

        private float AddYPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.YPixelToPercentage
            (
                (int) UIHelper.YPercentageToPixel(percent) + pixelDist
            );
        }

        /// <summary>
        ///     Taken from https://stackoverflow.com/a/839904
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="angleInDegrees"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private static Vector2 PointOnCircleInPercentage(float radius, float angleInDegrees, Vector2 origin)
        {
            // Convert from degrees to radians via multiplication by PI/180   
            var radians = angleInDegrees * Math.PI / 180F;
            var x = (float) (radius * Math.Cos(radians)) + origin.X;
            var y = (float) (radius * Math.Sin(radians)) + origin.Y;

            return new Vector2(UIHelper.XPixelToPercentage((int) x), UIHelper.YPixelToPercentage((int) y));
        }

        private static Size SizeMultiply(Size size, double factor)
        {
            return new Size((int) (size.Width * factor), (int) (size.Height * factor));
        }

        private float CalculateRelativeValue(float input, float inputMin, float inputMax, float outputMin,
            float outputMax)
        {
            //http://stackoverflow.com/questions/22083199/method-for-calculating-a-value-relative-to-min-max-values
            //Making sure bounderies arent broken...
            if (input > inputMax)
                input = inputMax;
            if (input < inputMin)
                input = inputMin;
            //Return value in relation to min og max

            var position = (double) (input - inputMin) / (inputMax - inputMin);

            var relativeValue = (float) (position * (outputMax - outputMin)) + outputMin;

            return relativeValue;
        }

        private float IncreaseNum(float num, float increment, float max)
        {
            return num + increment > max ? max : num + increment;
        }

        private float DecreaseNum(float num, float decrement, float min)
        {
            return num - decrement < min ? min : num - decrement;
        }

        /// <summary>
        ///     Add category to this wheel.
        /// </summary>
        /// <param name="category"></param>
        public void AddCategory(WheelCategory category)
        {
            if (Categories.Count == 18) return; //Don't allow more than 18 categories.

            Categories.Add(category);
        }

        public void ClearAllCategories()
        {
            Categories.Clear();
        }

        public void RemoveCategory(WheelCategory cat)
        {
            Categories.Remove(cat);
        }

        public bool IscategorySelected(WheelCategory cat)
        {
            if (Categories.Contains(cat))
                return Categories.IndexOf(cat) == CurrentCatIndex;
            return false;
        }

        private void ControlCategorySelection()
        {
            foreach (var cat in Categories)
            {
                var isSelectedCategory = SelectedCategory == cat;

                var catTextureExists =
                    cat.CategoryTexture !=
                    null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png");
                var itemTextureExists =
                    cat.SelectedItem.ItemTexture !=
                    null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.SelectedItem.Name) + ".png");
                var anyTextureExists = catTextureExists || itemTextureExists;

                if (UseTextures && anyTextureExists)
                {
                    //UI.DrawTexture(TexturePath + UIHelper.MakeValidFileName(catTextureExists ? cat.Name : cat.SelectedItem.Name) + ".png", Categories.IndexOf(cat), 1, TextureRefreshRate, new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset), new PointF(0.5f, 0.5f), TextureSize, 0f, isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255), aspectRatio);
                    var temp = catTextureExists ? cat.CategoryTexture : cat.SelectedItem.ItemTexture;
                    temp.Draw(1, TextureRefreshRate,
                        new Point((int)(cat.position2D.X * UI.WIDTH + xTextureOffset),
                            (int)(cat.position2D.Y * UI.HEIGHT + yTextureOffset)), new PointF(0.5f, 0.5f),
                        isSelectedCategory ? SizeMultiply(TextureSize, 1.25) : TextureSize, 0f,
                        isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255),
                        aspectRatio);
                }
                else
                {
                    var col = isSelectedCategory
                        ? Color.FromArgb(255, 255, 255, 255)
                        : Color.FromArgb(120, 255, 255, 255);
                    UIHelper.DrawCustomText(cat.Name, 0.8f, Font.ChaletComprimeCologne, col.R, col.G, col.B, col.A,
                        cat.position2D.X, cat.position2D.Y, UIHelper.TextJustification.Center);
                }
            }

            UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Name, 0.55f, Font.ChaletComprimeCologne, 255, 255,
                255, 255, Origin.X, AddYPixelDistanceToPercent(Origin.Y, -100), UIHelper.TextJustification.Center);
            if (SelectedCategory.ItemCount() > 1)
                UIHelper.DrawCustomText(
                    SelectedCategory.CurrentItemIndex + 1 + " / " + SelectedCategory.ItemCount().ToString(), 0.55f,
                    Font.ChaletComprimeCologne, 255, 255, 255, 255, Origin.X, AddYPixelDistanceToPercent(Origin.Y, -50),
                    UIHelper.TextJustification.Center);

            if (SelectedCategory.SelectedItem.Description != null)
            {
                var pixelX = 255f / UI.WIDTH;
                var pixelY = 255f / UI.HEIGHT;
                UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Description, 0.35f, Font.ChaletLondon,
                    255, 255, 255, 255, pixelX, pixelY, UIHelper.TextJustification.Left, true, pixelX,
                    512 / (float) UI.WIDTH, true, 0, 0, 0, 180, 10f / (float) UI.WIDTH, 10f / (float) UI.HEIGHT);
            }

            if (new Vector2(WheelLeftRightValue(), WheelUpDownValue()).Length() > deadzone)
                inputAngle = InputToAngle();
            inputCoord = PointOnCircleInPercentage(Radius / 1.5f, inputAngle, OriginInPixels);

            var p = new Point((int) (inputCoord.X * UI.WIDTH), (int) (inputCoord.Y * UI.HEIGHT));
            UI.DrawTexture(TexturePath + "Mouse.png", Categories.Count + 1, 1, 50, p, new Size(40, 40));

            var inputIndex = ClosestCategoryToInputCoord() != null
                ? Categories.IndexOf(ClosestCategoryToInputCoord())
                : CurrentCatIndex;
            if (inputIndex != CurrentCatIndex)
            {
                CurrentCatIndex = inputIndex;

                CategoryChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
        }

        private float InputToAngle()
        {
            var angle = Math.Atan2(Game.GetControlNormal(2, Control.WeaponWheelUpDown),
                Game.GetControlNormal(2, Control.WeaponWheelLeftRight));
            if (angle < 0)
                angle += Math.PI * 2;
            return (float) (angle * (180 / Math.PI));
        }

        private static double GetDistance(Vector2 point1, Vector2 point2)
        {
            //pythagorean theorem c^2 = a^2 + b^2
            //thus c = square root(a^2 + b^2)
            double a = point2.X - point1.X;
            double b = point2.Y - point1.Y;

            return Math.Sqrt(a * a + b * b);
        }

        private WheelCategory ClosestCategoryToInputCoord()
        {
            return Categories.OrderBy(c => GetDistance(c.position2D, inputCoord)).First();
        }

        private void ControlItemSelection()
        {
            if (GoToNextItemInCategory())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1)
                    SelectedCategory.SelectedItem.ItemTexture.StopDraw();
                SelectedCategory.GoToNextItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
            else if (GoToPreviousItemInCategory())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1)
                    SelectedCategory.SelectedItem.ItemTexture.StopDraw();
                SelectedCategory.GoToPreviousItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
        }

        private void ControlTransitions()
        {
            if (transitionIn)
            {
                var tempTScale = DecreaseNum(timeScale, Time.UnscaledDeltaTime * 8f, 0.05f);
                Game.TimeScale = tempTScale;
                timeScale = tempTScale;

                var tempStrength = IncreaseNum(timecycleCurrentStrength, Time.UnscaledDeltaTime * 8f,
                    TimecycleModifierStrength);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
            }
            if (transitionOut)
            {
                var tempTScale = IncreaseNum(timeScale, Time.UnscaledDeltaTime * 8f, 1f);
                Game.TimeScale = tempTScale;
                timeScale = tempTScale;

                var tempStrength = DecreaseNum(timecycleCurrentStrength, Time.UnscaledDeltaTime * 2f, 0f);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
                if (timecycleCurrentStrength <= 0f && timeScale >= 1f)
                {
                    Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                    transitionOut = false;
                }
            }
        }

        protected void DisableControls()
        {
            Controls.DisableControlsKeepRecording(2);

            foreach (var con in ControlsToEnable)
                Game.EnableControlThisFrame(2, con);
        }

        /// <summary>
        ///     Right: positive 1
        ///     Left: negative 1
        /// </summary>
        /// <returns>normalized value of left/right mouse/stick movement.</returns>
        private float WheelLeftRightValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelLeftRight);
        }

        /// <summary>
        ///     Down: positive 1
        ///     Up: negative 1
        /// </summary>
        /// <returns>normalized value of up/down mouse/stick movement.</returns>
        private float WheelUpDownValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelUpDown);
        }

        private bool GoToNextItemInCategory()
        {
            return Game.IsControlJustPressed(2, Control.WeaponWheelNext);
        }

        private bool GoToPreviousItemInCategory()
        {
            return Game.IsControlJustPressed(2, Control.WeaponWheelPrev);
        }

        protected virtual void CategoryChange(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnCategoryChange?.Invoke(this, selectedCategory, selecteditem);
        }

        protected virtual void ItemChange(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnItemChange?.Invoke(this, selectedCategory, selecteditem);
        }

        protected virtual void WheelOpen(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnWheelOpen?.Invoke(this, selectedCategory, selecteditem);
        }

        protected virtual void WheelClose(WheelCategory selectedCategory, WheelCategoryItem selecteditem)
        {
            OnWheelClose?.Invoke(this, selectedCategory, selecteditem);
        }
    }
}