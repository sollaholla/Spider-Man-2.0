using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;
using spiderman.net.Scripts;

/// <summary>
/// Credits: stillhere (LfxB)
/// https://github.com/LfxB/GTA-V-Selection-Wheel/blob/master/SelectorWheel/SelectorWheel.cs
/// </summary>
namespace SelectorWheel
{
    public delegate void CategoryChangeEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);
    public delegate void ItemChangeEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);
    public delegate void WheelOpenEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);
    public delegate void WheelCloseEvent(Wheel sender, WheelCategory selectedCategory, WheelCategoryItem selectedItem);

    public class Wheel
    {
        public string WheelName { get; set; }
        private bool _visible;
        public int CurrentCatIndex = 0;
        protected List<WheelCategory> Categories = new List<WheelCategory>();

        Vector2 _origin = new Vector2(0.5f, 0.4f);
        Vector2 inputCoord = Vector2.Zero;
        float Radius = 250;
        float inputAngle = 265f;
        const float deadzone = 0.005f;

        bool UseTextures;
        bool HaveTexturesBeenCached;
        string TexturePath;
        int TextureRefreshRate;
        int xTextureOffset = 0;
        int yTextureOffset = 0;
        Size TextureSize = new Size(30, 30);

        float aspectRatio = ((float)Game.ScreenResolution.Width / (float)Game.ScreenResolution.Height) / 2f;

        bool transitionIn;
        bool transitionOut;

        float timeScale = 1f;

        /// <summary>
        /// https://pastebin.com/kVPwMemE
        /// </summary>
        public string TimecycleModifier = "hud_def_desat_Neutral";
        public float TimecycleModifierStrength = 1.0f;
        float timecycleCurrentStrength = 0f;

        string AUDIO_SOUNDSET = "HUD_FRONTEND_DEFAULT_SOUNDSET";
        string AUDIO_SELECTSOUND = "HIGHLIGHT_NAV_UP_DOWN";

        /// <summary>
        /// Called when user hovers over a new category.
        /// </summary>
        public event CategoryChangeEvent OnCategoryChange;

        /// <summary>
        /// Called when user switches to a new item.
        /// </summary>
        public event ItemChangeEvent OnItemChange;

        /// <summary>
        /// Called when user opens the wheel.
        /// </summary>
        public event WheelOpenEvent OnWheelOpen;

        /// <summary>
        /// Called when user closes the wheel.
        /// </summary>
        public event WheelCloseEvent OnWheelClose;

        /// <summary>
        /// Show/Hide the selection wheel.
        /// </summary>
        public bool Visible {
            get { return _visible; }
            set {
                //start and end screen effects, etc. before toggling.

                if (_visible == false && value == true) //When the wheel is just opened.
                {
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER, TimecycleModifier);
                    Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, timecycleCurrentStrength);
                    transitionIn = true;
                    transitionOut = false;

                    CalculateCategoryPlacement();
                    aspectRatio = ((float)Game.ScreenResolution.Width / (float)Game.ScreenResolution.Height) / 2f;

                    CategoryChange(SelectedCategory, SelectedCategory.SelectedItem);
                    ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                    WheelOpen(SelectedCategory, SelectedCategory.SelectedItem);
                }
                else if (_visible == true && value == false) //When the wheel is just closed.
                {
                    transitionIn = false;
                    transitionOut = true;

                    WheelClose(SelectedCategory, SelectedCategory.SelectedItem);
                    foreach (var cat in Categories)
                    {
                        if (cat.CategoryTexture != null)
                        {
                            cat.CategoryTexture.StopDraw();
                        }
                        if (cat.SelectedItem.ItemTexture != null)
                        {
                            cat.SelectedItem.ItemTexture.StopDraw();
                        }
                    }
                }

                _visible = value;
            }
        }

        /// <summary>
        /// Instantiates a simple Selection Wheel that does not use textures. Just displays the category and item names.
        /// </summary>
        /// <param name="name">Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it yet.</param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, float wheelRadius = 250)
        {
            WheelName = name;
            Radius = wheelRadius;
        }

        /// <summary>
        /// Instantiates a Selection Wheel that uses textures for categories or items, if they exist.
        /// </summary>
        /// <param name="name">Name of the wheel. I was planning to display it while the wheel is shown but I didn't implement it yet.</param>
        /// <param name="texturePath">Path where category and item .png files are kept. Ex: @"scripts\SelectorWheelExample\"</param>
        /// <param name="xtextureOffset">Simple X offset, usually set to 0.</param>
        /// <param name="ytextureOffset">Simple Y offset, usually set to 0.</param>
        /// <param name="textureSize">Size of images (in pixels).</param>
        /// <param name="textureRefreshRate">How long (in ms) each texture will be displayed for.</param>
        /// <param name="wheelRadius">Length from the origin.</param>
        public Wheel(string name, string texturePath, int xtextureOffset, int ytextureOffset, Size textureSize, int textureRefreshRate = 50, float wheelRadius = 250)
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

        /// <summary>
        /// Must be placed in your Tick method.
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
        /// Call this after you have already added some categories (max is 18 categories).
        /// This function will check the amount of categories and situate them around the origin of the screen.
        /// 
        /// 
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
                    {
                        cat.CategoryTexture = new Texture2D(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png", Categories.IndexOf(cat));
                    }
                    foreach (var item in cat.ItemList)
                    {
                        if (File.Exists(TexturePath + UIHelper.MakeValidFileName(item.Name) + ".png"))
                        {
                            item.ItemTexture = new Texture2D(TexturePath + UIHelper.MakeValidFileName(item.Name) + ".png", Categories.IndexOf(cat) /*cat.ItemList.IndexOf(item)*/);
                        }
                    }

                    /*Load textures into cache*/
                    if (cat.CategoryTexture != null)
                    {
                        cat.CategoryTexture.LoadTexture();
                    }
                    foreach (var item in cat.ItemList)
                    {
                        if (item.ItemTexture != null)
                        {
                            item.ItemTexture.LoadTexture();
                        }
                    }
                }

                HaveTexturesBeenCached = true;
            }
        }

        void CalculateFromStartAngle(float startAngle, int numCategories)
        {
            float angleOffset = 360 / numCategories;
            for (int i = 0; i < numCategories; i++)
            {
                Categories[i].position2D = PointOnCircleInPercentage(Radius, startAngle, OriginInPixels);
                startAngle += angleOffset;
            }
        }

        /// <summary>
        /// In screen percentage.
        /// X: 0.5f = 50% from the left.
        /// Y: 0.5f = 50% from the top.
        /// Set this before calling CalculateCategoryPlacement() or it won't apply.
        /// </summary>
        public Vector2 Origin {
            get { return _origin; }
            set { _origin = value; }
        }

        private Vector2 OriginInPixels {
            get { return new Vector2(UIHelper.XPercentageToPixel(_origin.X), UIHelper.YPercentageToPixel(_origin.Y)); }
        }

        private float AddXPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.XPixelToPercentage
                (
                    (int)UIHelper.XPercentageToPixel(percent) + pixelDist
                );
        }

        private float AddYPixelDistanceToPercent(float percent, int pixelDist)
        {
            return UIHelper.YPixelToPercentage
                (
                    (int)UIHelper.YPercentageToPixel(percent) + pixelDist
                );
        }

        /// <summary>
        /// Taken from https://stackoverflow.com/a/839904
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="angleInDegrees"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private static Vector2 PointOnCircleInPercentage(float radius, float angleInDegrees, Vector2 origin)
        {
            // Convert from degrees to radians via multiplication by PI/180   
            double radians = angleInDegrees * Math.PI / 180F;
            float x = (float)(radius * Math.Cos(radians)) + origin.X;
            float y = (float)(radius * Math.Sin(radians)) + origin.Y;

            return new Vector2(UIHelper.XPixelToPercentage((int)x), UIHelper.YPixelToPercentage((int)y));
        }

        private static Size SizeMultiply(Size size, double factor)
        {
            return new Size((int)(size.Width * factor), (int)(size.Height * factor));
        }

        private float CalculateRelativeValue(float input, float inputMin, float inputMax, float outputMin, float outputMax)
        {
            //http://stackoverflow.com/questions/22083199/method-for-calculating-a-value-relative-to-min-max-values
            //Making sure bounderies arent broken...
            if (input > inputMax)
            {
                input = inputMax;
            }
            if (input < inputMin)
            {
                input = inputMin;
            }
            //Return value in relation to min og max

            double position = (double)(input - inputMin) / (inputMax - inputMin);

            float relativeValue = (float)(position * (outputMax - outputMin)) + outputMin;

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
        /// Add category to this wheel.
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
            {
                return Categories.IndexOf(cat) == CurrentCatIndex;
            }
            return false;
        }

        WheelCategory SelectedCategory {
            get { return Categories[CurrentCatIndex]; }
        }

        void ControlCategorySelection()
        {
            foreach (var cat in Categories)
            {
                bool isSelectedCategory = SelectedCategory == cat;

                bool catTextureExists = cat.CategoryTexture != null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.Name) + ".png");
                bool itemTextureExists = cat.SelectedItem.ItemTexture != null; //File.Exists(TexturePath + UIHelper.MakeValidFileName(cat.SelectedItem.Name) + ".png");
                bool anyTextureExists = catTextureExists || itemTextureExists;

                if (UseTextures && anyTextureExists)
                {
                    //UI.DrawTexture(TexturePath + UIHelper.MakeValidFileName(catTextureExists ? cat.Name : cat.SelectedItem.Name) + ".png", Categories.IndexOf(cat), 1, TextureRefreshRate, new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset), new PointF(0.5f, 0.5f), TextureSize, 0f, isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255), aspectRatio);
                    Texture2D temp = catTextureExists ? cat.CategoryTexture : cat.SelectedItem.ItemTexture;
                    temp.Draw(1, TextureRefreshRate, new Point((int)(cat.position2D.X * UI.WIDTH) + xTextureOffset, (int)(cat.position2D.Y * UI.HEIGHT) + yTextureOffset), new PointF(0.5f, 0.5f), isSelectedCategory ? SizeMultiply(TextureSize, 1.25) : TextureSize, 0f, isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255), aspectRatio);
                }
                else
                {
                    Color col = isSelectedCategory ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255);
                    UIHelper.DrawCustomText(cat.Name, 0.8f, GTA.Font.ChaletComprimeCologne, col.R, col.G, col.B, col.A, cat.position2D.X, cat.position2D.Y, UIHelper.TextJustification.Center);
                }

            }

            UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Name, 0.55f, GTA.Font.ChaletComprimeCologne, 255, 255, 255, 255, _origin.X, AddYPixelDistanceToPercent(_origin.Y, -100), UIHelper.TextJustification.Center);
            if (SelectedCategory.ItemCount() > 1)
            {
                UIHelper.DrawCustomText((SelectedCategory.CurrentItemIndex + 1).ToString() + " / " + SelectedCategory.ItemCount().ToString(), 0.55f, GTA.Font.ChaletComprimeCologne, 255, 255, 255, 255, _origin.X, AddYPixelDistanceToPercent(_origin.Y, -50), UIHelper.TextJustification.Center);
            }

            if (SelectedCategory.SelectedItem.Description != null)
            {
                float pixelX = 255f / (float)UI.WIDTH;
                float pixelY = 255f / (float)UI.HEIGHT;
                UIHelper.DrawCustomText(SelectedCategory.SelectedItem.Description, 0.35f, GTA.Font.ChaletLondon, 
                    255, 255, 255, 255, pixelX, pixelY, UIHelper.TextJustification.Left, true, pixelX, 
                    512 / (float)UI.WIDTH, true, 0, 0, 0, 180, 10f / (float)UI.WIDTH, 10f / (float)UI.HEIGHT);
            }

            if (new Vector2(WheelLeftRightValue(), WheelUpDownValue()).Length() > deadzone)
            {
                inputAngle = InputToAngle();
            }
            inputCoord = PointOnCircleInPercentage(Radius / 1.5f, inputAngle, OriginInPixels);

            var p = new Point((int)(inputCoord.X * UI.WIDTH), (int)(inputCoord.Y * UI.HEIGHT));
            UI.DrawTexture(TexturePath + "Mouse.png", Categories.Count + 1, 1, 50, p, new Size(40, 40));

            int inputIndex = ClosestCategoryToInputCoord() != null ? Categories.IndexOf(ClosestCategoryToInputCoord()) : CurrentCatIndex;
            if (inputIndex != CurrentCatIndex)
            {
                CurrentCatIndex = inputIndex;

                CategoryChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
        }

        float InputToAngle()
        {
            var angle = Math.Atan2(Game.GetControlNormal(2, GTA.Control.WeaponWheelUpDown), Game.GetControlNormal(2, GTA.Control.WeaponWheelLeftRight));
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }
            return (float)(angle * (180 / Math.PI));
        }

        static double GetDistance(Vector2 point1, Vector2 point2)
        {
            //pythagorean theorem c^2 = a^2 + b^2
            //thus c = square root(a^2 + b^2)
            double a = (double)(point2.X - point1.X);
            double b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }

        WheelCategory ClosestCategoryToInputCoord()
        {
            return Categories.OrderBy(c => GetDistance(c.position2D, inputCoord)).First();
        }

        void ControlItemSelection()
        {
            if (GoToNextItemInCategory())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1) { SelectedCategory.SelectedItem.ItemTexture.StopDraw(); }
                SelectedCategory.GoToNextItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
            else if (GoToPreviousItemInCategory())
            {
                if (SelectedCategory.SelectedItem.ItemTexture != null && SelectedCategory.ItemCount() > 1) { SelectedCategory.SelectedItem.ItemTexture.StopDraw(); }
                SelectedCategory.GoToPreviousItem();
                ItemChange(SelectedCategory, SelectedCategory.SelectedItem);
                Game.PlaySound(AUDIO_SELECTSOUND, AUDIO_SOUNDSET);
            }
        }

        void ControlTransitions()
        {
            if (transitionIn)
            {
                float tempTScale = DecreaseNum(timeScale, Time.UnscaledDeltaTime * 8f, 0.05f);
                Game.TimeScale = tempTScale;
                timeScale = tempTScale;

                float tempStrength = IncreaseNum(timecycleCurrentStrength, Time.UnscaledDeltaTime * 8f, TimecycleModifierStrength);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
            }
            if (transitionOut)
            {
                float tempTScale = IncreaseNum(timeScale, Time.UnscaledDeltaTime * 8f, 1f);
                Game.TimeScale = tempTScale;
                timeScale = tempTScale;

                float tempStrength = DecreaseNum(timecycleCurrentStrength, Time.UnscaledDeltaTime * 2f, 0f);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, tempStrength);
                timecycleCurrentStrength = tempStrength;
                if (timecycleCurrentStrength <= 0f && timeScale >= 1f)
                {
                    Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                    transitionOut = false;
                }
            }

        }

        List<Control> ControlsToEnable = new List<Control>
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

        protected void DisableControls()
        {
            spiderman.net.Scripts.Controls.DisableControlsKeepRecording(2);

            foreach (var con in ControlsToEnable)
            {
                Game.EnableControlThisFrame(2, con);
            }
        }

        /// <summary>
        /// Right: positive 1
        /// Left: negative 1
        /// </summary>
        /// <returns>normalized value of left/right mouse/stick movement.</returns>
        float WheelLeftRightValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelLeftRight);
        }

        /// <summary>
        /// Down: positive 1
        /// Up: negative 1
        /// </summary>
        /// <returns>normalized value of up/down mouse/stick movement.</returns>
        float WheelUpDownValue()
        {
            return Game.GetControlNormal(2, Control.WeaponWheelUpDown);
        }

        bool GoToNextItemInCategory()
        {
            return Game.IsControlJustPressed(2, Control.WeaponWheelNext);
        }

        bool GoToPreviousItemInCategory()
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

    public class WheelCategory
    {
        public string Name;
        public int CurrentItemIndex = 0;
        protected List<WheelCategoryItem> Items = new List<WheelCategoryItem>();
        public Vector2 position2D = Vector2.Zero;
        public Texture2D CategoryTexture;
        public int ID;

        /// <summary>
        /// Instantiates a new category for use in a selection wheel.
        /// </summary>
        /// <param name="name">Name of the category. If a matching .png image is found, the image will be displayed instead of any item image.</param>
        public WheelCategory(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Add item to this category.
        /// </summary>
        /// <param name="item">Item to add to this category</param>
        public void AddItem(WheelCategoryItem item)
        {
            Items.Add(item);
        }

        public void ClearAllItems()
        {
            Items.Clear();
        }

        public void RemoveItem(WheelCategoryItem item)
        {
            Items.Remove(item);
        }

        public int ItemCount()
        {
            return Items.Count;
        }

        public List<WheelCategoryItem> ItemList {
            get { return Items; }
        }

        public bool IsItemSelected(WheelCategoryItem item)
        {
            if (Items.Contains(item))
            {
                return Items.IndexOf(item) == CurrentItemIndex;
            }
            return false;
        }

        public WheelCategoryItem SelectedItem {
            get { return Items.ElementAt(CurrentItemIndex); }
        }

        public void GoToNextItem()
        {
            if (CurrentItemIndex < Items.Count - 1)
            {
                CurrentItemIndex++;
            }
            else
            {
                CurrentItemIndex = 0;
            }
        }

        public void GoToPreviousItem()
        {
            if (CurrentItemIndex > 0)
            {
                CurrentItemIndex--;
            }
            else
            {
                CurrentItemIndex = Items.Count - 1;
            }
        }
    }

    /// <summary>
    /// Holds information about a wheel category slot.
    /// </summary>
    public class WheelCategoryItem
    {
        /// <summary>
        /// Information to easily identify this item.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// The name of this category item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The item's texture information.
        /// </summary>
        public Texture2D ItemTexture { get; set; }

        /// <summary>
        /// The item's description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">Name of the item. If a matching .png image is found, the image will be displayed assuming no image for this item's category has been found.</param>
        public WheelCategoryItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">Name of the item. If a matching .png image is found, 
        /// the image will be displayed assuming no image for this item's category has been found.</param>
        /// <param name="description">A description that will be displayed on the right side of the screen.</param>
        public WheelCategoryItem(string name, string description) : this(name)
        {
            Description = description;
        }
    }

    /// <summary>
    /// A class that allows drawing and manipulation of custom textures.
    /// </summary>
    public class Texture2D
    {
        /// <summary>
        /// The texture path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The texture's draw index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The level (above others) which to draw this.
        /// </summary>
        public int DrawLevel { get; set; }

        /// <summary>
        /// The main ctor.
        /// </summary>
        /// <param name="path">The path to the texture file.</param>
        /// <param name="index">The draw index of the texture.</param>
        public Texture2D(string path, int index)
        {
            Path = path;
            Index = index;
        }

        public void Draw(int level, int time, Point pos, Size size)
        {
            UI.DrawTexture(Path, Index, level, time, pos, size);
        }

        public void Draw(int level, int time, Point pos, Size size, float rotation, Color color)
        {
            UI.DrawTexture(Path, Index, level, time, pos, size, rotation, color);
        }

        public void Draw(int level, int time, Point pos, PointF center, Size size, float rotation, Color color)
        {
            UI.DrawTexture(Path, Index, level, time, pos, center, size, rotation, color);
        }

        public void Draw(int level, int time, Point pos, PointF center, Size size, float rotation, Color color, float aspectRatio)
        {
            UI.DrawTexture(Path, Index, level, time, pos, center, size, rotation, color, aspectRatio);
        }

        public void LoadTexture()
        {
            StopDraw();
        }

        public void StopDraw()
        {
            UI.DrawTexture(Path, Index, 1, 0, new Point(1280, 720), new Size(0, 0));
        }
    }

    public static class UIHelper
    {
        public enum TextJustification
        {
            Center = 0,
            Left,
            Right //requires SET_TEXT_WRAP
        }

        public static void DrawCustomText(string Message, float FontSize, GTA.Font FontType, int Red, int Green, int Blue, int Alpha, float XPos, float YPos, TextJustification justifyType = TextJustification.Left, bool ForceTextWrap = false, float startWrap = 0f, float endWrap = 1f, bool withRectangle = false, int R = 0, int G = 0, int B = 0, int A = 255, float rectWidthOffset = 0f, float rectHeightOffset = 0f, float rectYPosDivisor = 23.5f)
        {
            Function.Call(Hash._SET_TEXT_ENTRY, "jamyfafi"); //Required, don't change this! AKA BEGIN_TEXT_COMMAND_DISPLAY_TEXT
            Function.Call(Hash.SET_TEXT_SCALE, FontSize, FontSize); //1st param: 1.0f
            Function.Call(Hash.SET_TEXT_FONT, (int)FontType);
            Function.Call(Hash.SET_TEXT_COLOUR, Red, Green, Blue, Alpha);
            //Function.Call(Hash.SET_TEXT_DROPSHADOW, 0, 0, 0, 0, 0);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int)justifyType);
            if (justifyType == TextJustification.Right || ForceTextWrap)
            {
                Function.Call(Hash.SET_TEXT_WRAP, startWrap, endWrap);
            }

            //Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, Message);
            AddLongString(Message);

            Function.Call(Hash._DRAW_TEXT, XPos, YPos); //AKA END_TEXT_COMMAND_DISPLAY_TEXT

            if (withRectangle)
            {
                switch (FontType)
                {
                    case GTA.Font.ChaletLondon: rectYPosDivisor = 15f; break;
                    case GTA.Font.HouseScript: rectYPosDivisor = 23f; break;
                    case GTA.Font.Monospace: rectYPosDivisor = 25f; break;
                    case GTA.Font.ChaletComprimeCologne: rectYPosDivisor = 30f; break;
                    case GTA.Font.Pricedown: rectYPosDivisor = 35f; break;
                }

                float adjWidth = MeasureStringWidthNoConvert(Message, FontType, FontSize);
                float fontHeight = MeasureFontHeightNoConvert(FontSize, FontType);
                float rectangleWidth = (endWrap - startWrap) + rectWidthOffset;
                float baseYPos = YPos + (FontSize / rectYPosDivisor);
                //int numLines = (int)Math.Ceiling(adjWidth / ((endWrap - startWrap) * 0.98f));
                int numLines = GetStringLineCount(Message, FontSize, FontType, startWrap, endWrap, XPos, YPos);
                for (int i = 0; i < numLines; i++)
                {
                    float adjustedYPos = i == 0 ? baseYPos - rectHeightOffset / 2
                        : (i == numLines - 1 ? baseYPos + rectHeightOffset / 2
                        : baseYPos);

                    float adjustedRectangleHeight = i == 0 || i == numLines - 1 ? fontHeight + rectHeightOffset
                        : fontHeight;

                    float adjustedXPos = justifyType == TextJustification.Left ? XPos + ((endWrap - startWrap) / 2)
                        : (justifyType == TextJustification.Right ? endWrap - ((endWrap - startWrap) / 2)
                        : XPos);

                    DrawRectangle(adjustedXPos, adjustedYPos + (i * fontHeight), rectangleWidth, adjustedRectangleHeight, R, G, B, A);
                }
            }
        }

        public static void DrawRectangle(float BgXpos, float BgYpos, float BgWidth, float BgHeight, int bgR, int bgG, int bgB, int bgA)
        {
            Function.Call(Hash.DRAW_RECT, BgXpos, BgYpos, BgWidth, BgHeight, bgR, bgG, bgB, bgA);
        }

        public static void AddLongString(string str)
        {
            const int strLen = 99;
            for (int i = 0; i < str.Length; i += strLen)
            {
                string substr = str.Substring(i, Math.Min(strLen, str.Length - i));
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, substr); //ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME
            }
        }

        public static float MeasureStringWidth(string str, GTA.Font font, float fontsize)
        {
            //int screenw = 2560;// Game.ScreenResolution.Width;
            //int screenh = 1440;// Game.ScreenResolution.Height;
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;
            return MeasureStringWidthNoConvert(str, font, fontsize) * width;
        }

        private static float MeasureStringWidthNoConvert(string str, GTA.Font font, float fontsize)
        {
            Function.Call((Hash)0x54CE8AC98E120CAB, "jamyfafi"); //_BEGIN_TEXT_COMMAND_WIDTH
            AddLongString(str);
            Function.Call(Hash.SET_TEXT_FONT, (int)font);
            Function.Call(Hash.SET_TEXT_SCALE, fontsize, fontsize);
            return Function.Call<float>(Hash._0x85F061DA64ED2F67, true); //_END_TEXT_COMMAND_GET_WIDTH //Function.Call<float>((Hash)0x85F061DA64ED2F67, (int)font) * fontsize; //_END_TEXT_COMMAND_GET_WIDTH
        }

        public static float MeasureFontHeight(float fontSize, GTA.Font font)
        {
            return Function.Call<float>(Hash._0xDB88A37483346780, fontSize, (int)font) * Game.ScreenResolution.Height; //1080f
        }

        public static float MeasureFontHeightNoConvert(float fontSize, GTA.Font font)
        {
            return Function.Call<float>(Hash._0xDB88A37483346780, fontSize, (int)font);
        }

        public static int GetStringLineCount(string text, float FontSize, GTA.Font FontType, float startWrap, float endWrap, float x, float y)
        {
            Function.Call((Hash)0x521FB041D93DD0E4, "jamyfafi"); //_BEGIN_TEXT_COMMAND_LINE_COUNT
            Function.Call(Hash.SET_TEXT_SCALE, FontSize, FontSize); //1st param: 1.0f
            Function.Call(Hash.SET_TEXT_FONT, (int)FontType);
            Function.Call(Hash.SET_TEXT_WRAP, startWrap, endWrap);
            AddLongString(text);
            return Function.Call<int>((Hash)0x9040DFB09BE75706, x, y); //_END_TEXT_COMMAND_GET_LINE_COUNT
        }

        public static float XPixelToPercentage(int pixel)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return pixel / width;
        }

        public static float YPixelToPercentage(int pixel)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return pixel / height;
        }

        public static float XPercentageToPixel(float percent)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return percent * width;
        }

        public static float YPercentageToPixel(float percent)
        {
            const float height = 1080f;
            float ratio = (float)Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            float width = height * ratio;

            return percent * height;
        }

        public static string MakeValidFileName(string original, char replacementChar = '_')
        {
            var invalidChars = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars());
            return new string(original.Select(c => invalidChars.Contains(c) ? replacementChar : c).ToArray());
        }
    }
}