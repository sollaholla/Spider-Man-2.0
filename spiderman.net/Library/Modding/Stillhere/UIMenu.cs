using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using Control = GTA.Control;
using Font = GTA.Font;

namespace SpiderMan.Library.Modding.Stillhere
{
    public class UIMenu
    {
        public enum TextJustification
        {
            Center = 0,
            Left,
            Right //requires SET_TEXT_WRAP
        }

        private static readonly int InputWait = 80;
        private readonly List<BindedItem> _bindedList = new List<BindedItem>();
        protected List<UIMenuItem> _itemList = new List<UIMenuItem>();
        private readonly string AUDIO_BACK = "BACK";
        private readonly string AUDIO_LEFTRIGHT = "NAV_LEFT_RIGHT";

        private readonly string AUDIO_LIBRARY = "HUD_FRONTEND_DEFAULT_SOUNDSET";
        private readonly string AUDIO_SELECT = "SELECT";

        private readonly string AUDIO_UPDOWN = "NAV_UP_DOWN";
        public int boxHeight = 38; //height in pixels
        public int boxScrollWidth = 4; //width in pixels
        public int boxTitleHeight = 76; //height in pixels
        public int boxUnderlineHeight = 1; //height in pixels
        public int boxWidth = 500; //width in pixels

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
            /*Control.VehicleRadioWheel,
                Control.VehicleRoof,
                Control.VehicleHeadlight,
                Control.VehicleCinCam,
                Control.Phone,
                Control.MeleeAttack1,
                Control.MeleeAttack2,
                Control.Attack,
                Control.Attack2*/
            Control.LookUpDown,
            Control.LookLeftRight
        };

        public Color DefaultBoxColor = Color.FromArgb(144, 0, 0, 0);

        /*UIMenuItem Formatting*/
        public Color DefaultTextColor = Color.FromArgb(255, 255, 255, 255);

        public Color DescriptionBoxColor = Color.FromArgb(150, 0, 255, 255);

        /*Description Formatting*/
        public Color DescriptionTextColor = Color.FromArgb(255, 0, 0, 0);

        protected float heightItemBG;
        public Color HighlightedBoxColor = Color.FromArgb(255, 0, 0, 0);
        public Color HighlightedItemTextColor = Color.FromArgb(255, 0, 255, 255);

        private DateTime InputTimer;
        public bool IsVisible;

        protected float ItemTextFontSize;
        protected Font ItemTextFontType;
        protected int maxItem = 14; //must always be 1 less than MaxItemsOnScreen
        protected int MaxItemsOnScreen = 15;
        protected float MenuBGWidth;

        public int menuXPos = 38; //pixels from the top
        public int menuYPos = 38; //pixels from the left
        protected int minItem;
        protected float posMultiplier;

        protected float ScrollBarWidth;

        public int SelectedIndex;
        public UIMenuItem SelectedItem;
        public Color TitleBackgroundColor = Color.FromArgb(144, 0, 0, 0);
        protected float TitleBGHeight;

        /*Title Formatting*/
        public Color TitleColor = Color.FromArgb(255, 255, 255, 255);

        /*Title*/
        public float TitleFontSize;

        public Color TitleUnderlineColor = Color.FromArgb(140, 0, 255, 255);
        protected float UnderlineHeight;

        public bool UseEventBasedControls = true;

        /*Scroll or nah?*/
        private readonly bool UseScroll = true;

        /*Rectangle box for UIMenuItem objects*/
        protected float xPosBG;

        protected float xPosItemText;
        protected float xPosItemValue;
        protected float xPosRightEndOfMenu;
        protected float xPosScrollBar;
        private int YPosBasedOnScroll;
        private int YPosDescBasedOnScroll;
        protected float yPosItem;
        protected float yPosItemBG;
        private float YPosSmoothScrollBar;
        protected float yPosTitleBG;
        protected float yPosTitleText;
        protected float yPosUnderline;
        protected float yTextOffset;

        //protected event KeyEventHandler KeyUp;
        //bool AcceptPressed;
        //bool CancelPressed;

        public UIMenu(string title)
        {
            Title = title;

            TitleFontSize = 0.9f; //TitleFont = 1.1f; for no-value fit.
            ItemTextFontSize = 0.452f;
            ItemTextFontType = Font.ChaletComprimeCologne;

            CalculateMenuPositioning();

            //KeyUp += UIMenu_KeyUp;
        }

        public UIMenu ParentMenu { get; set; }
        public UIMenuItem ParentItem { get; set; }
        public UIMenu NextMenu { get; set; }
        public UIMenuItem BindingMenuItem { get; set; }
        public string Title { get; set; }
        public Dictionary<UIMenuItem, UIMenu> Binded { get; }

        public List<UIMenuItem> UIMenuItemList
        {
            get => _itemList;
            set => _itemList = value;
        }

        /// <summary>
        ///     Called when user selects a simple item.
        /// </summary>
        public event ItemSelectEvent OnItemSelect;

        /// <summary>
        ///     Called when user presses left or right over a simple item.
        /// </summary>
        public event ItemLeftRightEvent OnItemLeftRight;

        /*private void UIMenu_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsVisible)
            {
                if (e.KeyCode == Keys.NumPad5 || e.KeyCode == Keys.Enter)
                {
                    AcceptPressed = true;
                    UI.ShowSubtitle("HI");
                }

                if (e.KeyCode == Keys.NumPad0 || e.KeyCode == Keys.Back)
                {
                    CancelPressed = true;
                }
            }
        }*/

        public virtual void CalculateMenuPositioning()
        {
            const float height = 1080f;
            var ratio = (float) Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            var width = height * ratio;

            TitleBGHeight = boxTitleHeight / height; //0.046f
            yPosTitleBG = menuYPos / height + TitleBGHeight * 0.5f;
            MenuBGWidth = boxWidth / width; //MenuBGWidth = 0.24f; for no-value fit.
            xPosBG = menuXPos / width + MenuBGWidth * 0.5f; //xPosBG = 0.13f; for no-value fit.
            xPosItemText = (menuXPos + 10) / width;
            heightItemBG = boxHeight / height;
            UnderlineHeight = boxUnderlineHeight / height; //0.002f;
            posMultiplier = boxHeight / height;
            yTextOffset = 0.015f; //offset between text pos and box pos. yPosItemBG - yTextOffset
            ScrollBarWidth = boxScrollWidth / width;

            yPosTitleText = yPosTitleBG - TitleFontSize / 35f;
            yPosUnderline = yPosTitleBG + TitleBGHeight / 2 + UnderlineHeight / 2;
            yPosItemBG = yPosUnderline + UnderlineHeight / 2 + heightItemBG / 2; //0.0655f;
            yPosItem = yPosItemBG - ItemTextFontSize / 30.13f;
            //xPosItemText = xPosBG - (MenuBGWidth / 2) + 0.0055f;
            xPosRightEndOfMenu = xPosBG + MenuBGWidth / 2; //will Right Justify
            xPosScrollBar = xPosRightEndOfMenu - ScrollBarWidth / 2;
            xPosItemValue = xPosScrollBar - ScrollBarWidth / 2;
            YPosSmoothScrollBar =
                yPosItemBG; //sets starting scroll bar Y pos. Will be manipulated for smooth scrolling later.
        }

        public void MaxItemsInMenu(int number)
        {
            MaxItemsOnScreen = number;
            maxItem = number - 1;
        }

        public void ResetIndexPosition()
        {
            SelectedIndex = 0;
            minItem = 0;
            MaxItemsInMenu(MaxItemsOnScreen);
        }

        public void SetIndexPosition(int indexPosition)
        {
            SelectedIndex = indexPosition;

            if (SelectedIndex >= MaxItemsOnScreen)
            {
                //int possibleMin = SelectedIndex - MaxItemsOnScreen;
                minItem = SelectedIndex - MaxItemsOnScreen;
                maxItem = SelectedIndex;
            }
            else
            {
                minItem = 0;
                maxItem = MaxItemsOnScreen - 1;
            }
        }

        public void AddMenuItem(UIMenuItem item)
        {
            _itemList.Add(item);
        }

        public void BindItemToSubmenu(UIMenu submenu, UIMenuItem itemToBindTo)
        {
            submenu.ParentMenu = this;
            submenu.ParentItem = itemToBindTo;
            /*if (Binded.ContainsKey(itemToBindTo))
                Binded[itemToBindTo] = submenu;
            else
                Binded.Add(itemToBindTo, submenu);*/
            _bindedList.Add(new BindedItem {BindedSubmenu = submenu, BindedItemToSubmenu = itemToBindTo});
        }

        public virtual void Draw()
        {
            if (IsVisible)
            {
                DisplayMenu();
                DisableControls();
                DrawScrollBar();
                ManageCurrentIndex();
                /*if (SelectedItem is UIMenuListItem)
                {
                    SelectedItem.ChangeListIndex();
                }*/
                //UI.ShowSubtitle("selectedIndex: " + SelectedIndex + ", minItem: " + minItem + ", maxItem: " + maxItem); //Debug

                if ( /*BindingMenuItem != null && NextMenu != null*/ _bindedList.Count > 0)
                    if (JustPressedAccept() && /*BindingMenuItem == SelectedItem*/
                        _bindedList.Any(bind => bind.BindedItemToSubmenu == SelectedItem))
                    {
                        IsVisible = false;

                        foreach (var bind in _bindedList.Where(bind => bind.BindedItemToSubmenu == SelectedItem))
                        {
                            bind.BindedSubmenu.IsVisible = true;
                            //bind.BindedSubmenu.AcceptPressed = false;
                            bind.BindedSubmenu.InputTimer = DateTime.Now.AddMilliseconds(350);
                        }

                        InputTimer = DateTime.Now.AddMilliseconds(350);
                        //return;
                    }

                if (JustPressedCancel())
                {
                    IsVisible = false;

                    if (ParentMenu != null)
                    {
                        ParentMenu.IsVisible = true;
                        //ParentMenu.CancelPressed = false;
                        ParentMenu.InputTimer = DateTime.Now.AddMilliseconds(350);
                    }

                    //CancelPressed = false;
                    InputTimer = DateTime.Now.AddMilliseconds(350);
                    //return;
                }

                if (UseEventBasedControls)
                {
                    if (JustPressedAccept())
                    {
                        ItemSelect(SelectedItem, SelectedIndex);
                        InputTimer = DateTime.Now.AddMilliseconds(InputWait);
                        //AcceptPressed = false;
                    }

                    if (JustPressedLeft())
                    {
                        ItemLeftRight(SelectedItem, SelectedIndex, true);
                        InputTimer = DateTime.Now.AddMilliseconds(InputWait);
                    }

                    if (JustPressedRight())
                    {
                        ItemLeftRight(SelectedItem, SelectedIndex, false);
                        InputTimer = DateTime.Now.AddMilliseconds(InputWait);
                    }
                }
            }
        }


        protected void DisplayMenu()
        {
            DrawCustomText(Title, TitleFontSize, Font.HouseScript, TitleColor.R, TitleColor.G, TitleColor.B,
                TitleColor.A, xPosBG, yPosTitleText, xPosItemText, xPosItemValue,
                TextJustification.Center); //Draw title text
            DrawRectangle(xPosBG, yPosTitleBG, MenuBGWidth, TitleBGHeight, TitleBackgroundColor.R,
                TitleBackgroundColor.G, TitleBackgroundColor.B, TitleBackgroundColor.A); //Draw main rectangle
            DrawRectangle(xPosBG, yPosUnderline, MenuBGWidth, UnderlineHeight, TitleUnderlineColor.R,
                TitleUnderlineColor.G, TitleUnderlineColor.B,
                TitleUnderlineColor.A); //Draw rectangle as underline of title

            foreach (UIMenuItem item in _itemList)
            {
                var ScrollOrNotDecision =
                    UseScroll && _itemList.IndexOf(item) >= minItem && _itemList.IndexOf(item) <= maxItem || !UseScroll;
                if (ScrollOrNotDecision)
                {
                    YPosBasedOnScroll = UseScroll && _itemList.Count > MaxItemsOnScreen
                        ? CalculatePosition(_itemList.IndexOf(item), minItem, maxItem, 0, MaxItemsOnScreen - 1)
                        : _itemList.IndexOf(item);
                    YPosDescBasedOnScroll = UseScroll && _itemList.Count > MaxItemsOnScreen
                        ? MaxItemsOnScreen
                        : _itemList.Count;

                    if (_itemList.IndexOf(item) == SelectedIndex)
                    {
                        DrawCustomText(item.Text, ItemTextFontSize, ItemTextFontType, HighlightedItemTextColor.R,
                            HighlightedItemTextColor.G, HighlightedItemTextColor.B, HighlightedItemTextColor.A,
                            xPosItemText, yPosItem + YPosBasedOnScroll * posMultiplier, xPosItemText,
                            xPosItemValue); //Draw highlighted item text

                        if (item.Value != null)
                            DrawCustomText(Convert.ToString(item.Value), ItemTextFontSize, ItemTextFontType,
                                HighlightedItemTextColor.R, HighlightedItemTextColor.G, HighlightedItemTextColor.B,
                                HighlightedItemTextColor.A, xPosItemValue, yPosItem + YPosBasedOnScroll * posMultiplier,
                                xPosItemText, xPosItemValue, TextJustification.Right);

                        DrawRectangle(xPosBG, yPosItemBG + YPosBasedOnScroll * posMultiplier, MenuBGWidth, heightItemBG,
                            HighlightedBoxColor.R, HighlightedBoxColor.G, HighlightedBoxColor.B,
                            HighlightedBoxColor.A); //Draw rectangle over highlighted text

                        if (item.Description != null)
                        {
                            /*foreach (string desc in item.DescriptionTexts)
                            {
                                DrawCustomText(desc, ItemTextFontSize, ItemTextFontType, DescriptionTextColor.R, DescriptionTextColor.G, DescriptionTextColor.B, DescriptionTextColor.A, xPosItemText, yPosItem + (item.DescriptionTexts.IndexOf(desc) + YPosDescBasedOnScroll) * posMultiplier, TextJustification.Left, false); // Draw description text at bottom of menu
                                DrawRectangle(xPosBG, yPosItemBG + (item.DescriptionTexts.IndexOf(desc) + YPosDescBasedOnScroll) * posMultiplier, MenuBGWidth, heightItemBG, DescriptionBoxColor.R, DescriptionBoxColor.G, DescriptionBoxColor.B, DescriptionBoxColor.A); //Draw rectangle over description text at bottom of the list.
                            }*/

                            DrawCustomText(item.Description, ItemTextFontSize, ItemTextFontType, DescriptionTextColor.R,
                                DescriptionTextColor.G, DescriptionTextColor.B, DescriptionTextColor.A, xPosItemText,
                                yPosItem + YPosDescBasedOnScroll * posMultiplier, xPosItemText, xPosItemValue,
                                TextJustification.Left, true); // Draw description text at bottom of menu
                            float numLines = item.DescriptionWidth / (boxWidth - 10);
                            for (var l = 0; l < (int) Math.Ceiling(numLines); l++)
                                DrawRectangle(xPosBG, yPosItemBG + (l + YPosDescBasedOnScroll) * posMultiplier,
                                    MenuBGWidth, heightItemBG, DescriptionBoxColor.R, DescriptionBoxColor.G,
                                    DescriptionBoxColor.B,
                                    DescriptionBoxColor
                                        .A); //Draw rectangle over description text at bottom of the list.
                            //UI.ShowSubtitle(numLines.ToString());
                        }

                        SelectedItem = item;
                    }
                    else
                    {
                        DrawCustomText(item.Text, ItemTextFontSize, ItemTextFontType, DefaultTextColor.R,
                            DefaultTextColor.G, DefaultTextColor.B, DefaultTextColor.A, xPosItemText,
                            yPosItem + YPosBasedOnScroll * posMultiplier, xPosItemText, xPosItemValue); //Draw item text

                        if (item.Value != null)
                            DrawCustomText(Convert.ToString(item.Value), ItemTextFontSize, ItemTextFontType,
                                DefaultTextColor.R, DefaultTextColor.G, DefaultTextColor.B, DefaultTextColor.A,
                                xPosItemValue, yPosItem + YPosBasedOnScroll * posMultiplier, xPosItemText,
                                xPosItemValue, TextJustification.Right);

                        DrawRectangle(xPosBG, yPosItemBG + YPosBasedOnScroll * posMultiplier, MenuBGWidth, heightItemBG,
                            DefaultBoxColor.R, DefaultBoxColor.G, DefaultBoxColor.B,
                            DefaultBoxColor.A); //Draw background rectangles around all items.
                    }
                }
            }

            //DevMenuPositioner();
        }

        private void DevMenuPositioner()
        {
            if (Game.IsKeyPressed(Keys.NumPad6))
                ItemTextFontSize = (float) Math.Round(ItemTextFontSize + 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad4))
                ItemTextFontSize = (float) Math.Round(ItemTextFontSize - 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad8))
                heightItemBG = (float) Math.Round(heightItemBG + 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad2))
                heightItemBG = (float) Math.Round(heightItemBG - 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad9))
                posMultiplier = (float) Math.Round(posMultiplier + 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad7))
                posMultiplier = (float) Math.Round(posMultiplier - 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad3))
                yTextOffset = (float) Math.Round(yTextOffset + 0.001, 3);
            if (Game.IsKeyPressed(Keys.NumPad1))
                yTextOffset = (float) Math.Round(yTextOffset - 0.001, 3);
            CalculateMenuPositioning();
            UI.ShowSubtitle("ItemTextFontSize: " + ItemTextFontSize + ", heightItemBG: " + heightItemBG +
                            ", posMultiplier: " + posMultiplier + ", yTextOffset: " + yTextOffset);
        }

        protected void DrawScrollBar()
        {
            if (UseScroll && _itemList.Count > MaxItemsOnScreen)
            {
                YPosSmoothScrollBar = CalculateSmoothPosition(YPosSmoothScrollBar,
                    CalculateScroll(SelectedIndex, 0, _itemList.Count - 1, yPosItemBG,
                        yPosItemBG + (MaxItemsOnScreen - 1) * posMultiplier), 0.0005f, yPosItemBG,
                    yPosItemBG + (MaxItemsOnScreen - 1) * posMultiplier);
                DrawRectangle(xPosScrollBar, YPosSmoothScrollBar, ScrollBarWidth, heightItemBG, TitleUnderlineColor.R,
                    TitleUnderlineColor.G, TitleUnderlineColor.B, TitleUnderlineColor.A);

                //DrawRectangle(xPosScrollBar, CalculateScroll(SelectedIndex, 0, _itemList.Count - 1, yPosItemBG, yPosItemBG + (MaxItemsOnScreen - 1) * posMultiplier), ScrollBarWidth, heightItemBG, TitleUnderlineColor.R, TitleUnderlineColor.G, TitleUnderlineColor.B, TitleUnderlineColor.A);
            }
        }

        private int CalculatePosition(int input, int inputMin, int inputMax, int outputMin, int outputMax)
        {
            //http://stackoverflow.com/questions/22083199/method-for-calculating-a-value-relative-to-min-max-values
            //Making sure bounderies arent broken...
            if (input > inputMax)
                input = inputMax;
            if (input < inputMin)
                input = inputMin;
            //Return value in relation to min og max

            var position = (double) (input - inputMin) / (inputMax - inputMin);

            var relativeValue = (int) (position * (outputMax - outputMin)) + outputMin;

            return relativeValue;
        }

        private float CalculateScroll(float input, float inputMin, float inputMax, float outputMin, float outputMax)
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

        private float CalculateSmoothPosition(float currentPosition, float desiredPosition, float step, float min,
            float max)
        {
            if (currentPosition == desiredPosition) return currentPosition;

            if (currentPosition < desiredPosition)
            {
                //currentPosition += (desiredPosition - currentPosition) * 0.1f;
                currentPosition += (desiredPosition - currentPosition) * 5f * Time.UnscaledDeltaTime;
                if (currentPosition > max)
                    currentPosition = max;
                return currentPosition;
            }
            if (currentPosition > desiredPosition)
            {
                //currentPosition -= (currentPosition - desiredPosition) * 0.1f;
                currentPosition -= (currentPosition - desiredPosition) * 5f * Time.UnscaledDeltaTime;
                if (currentPosition < min)
                    currentPosition = min;
                return currentPosition;
            }
            return currentPosition;
        }

        public static void DrawCustomText(string Message, float FontSize, Font FontType,
            int Red, int Green, int Blue, int Alpha, float XPos, float YPos, float wrapX, float wrapY,
            TextJustification justifyType = TextJustification.Left, bool ForceTextWrap = false,
            string textType = "jamyfafi")
        {
            Function.Call(Hash._SET_TEXT_ENTRY,
                textType); //Required, don't change this! AKA BEGIN_TEXT_COMMAND_DISPLAY_TEXT
            Function.Call(Hash.SET_TEXT_SCALE, 1.0f, FontSize);
            Function.Call(Hash.SET_TEXT_FONT, (int) FontType);
            Function.Call(Hash.SET_TEXT_COLOUR, Red, Green, Blue, Alpha);
            //Function.Call(Hash.SET_TEXT_DROPSHADOW, 0, 0, 0, 0, 0);
            Function.Call(Hash.SET_TEXT_JUSTIFICATION, (int) justifyType);
            if (justifyType == TextJustification.Right || ForceTextWrap)
                Function.Call(Hash.SET_TEXT_WRAP, wrapX, wrapY);

            //Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, Message);
            StringHelper.AddLongString(Message);

            Function.Call(Hash._DRAW_TEXT, XPos, YPos); //AKA END_TEXT_COMMAND_DISPLAY_TEXT
        }

        private void DrawRectangle(float BgXpos, float BgYpos, float BgWidth, float BgHeight, int bgR, int bgG, int bgB,
            int bgA)
        {
            Function.Call(Hash.DRAW_RECT, BgXpos, BgYpos, BgWidth, BgHeight, bgR, bgG, bgB, bgA);
        }

        protected virtual void ManageCurrentIndex()
        {
            if (JustPressedUp())
            {
                if (SelectedIndex > 0 && SelectedIndex <= _itemList.Count - 1)
                {
                    SelectedIndex--;
                    if (SelectedIndex < minItem && minItem > 0)
                    {
                        minItem--;
                        maxItem--;
                    }
                }
                else if (SelectedIndex == 0)
                {
                    SelectedIndex = _itemList.Count - 1;
                    minItem = _itemList.Count - MaxItemsOnScreen;
                    maxItem = _itemList.Count - 1;
                }
                else
                {
                    SelectedIndex = _itemList.Count - 1;
                    minItem = _itemList.Count - MaxItemsOnScreen;
                    maxItem = _itemList.Count - 1;
                }

                if (IsHoldingSpeedupControl())
                    InputTimer = DateTime.Now.AddMilliseconds(20);
                else
                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);
            }

            if (JustPressedDown())
            {
                if (SelectedIndex >= 0 && SelectedIndex < _itemList.Count - 1)
                {
                    SelectedIndex++;
                    if (SelectedIndex >= maxItem + 1)
                    {
                        minItem++;
                        maxItem++;
                    }
                }
                else if (SelectedIndex == _itemList.Count - 1)
                {
                    SelectedIndex = 0;
                    minItem = 0;
                    maxItem = MaxItemsOnScreen - 1;
                }
                else
                {
                    SelectedIndex = 0;
                    minItem = 0;
                    maxItem = MaxItemsOnScreen - 1;
                }

                if (IsHoldingSpeedupControl())
                    InputTimer = DateTime.Now.AddMilliseconds(20);
                else
                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);
            }
        }

        protected void DisableControls()
        {
            Controls.DisableControlsKeepRecording(2);

            foreach (var con in ControlsToEnable)
                Game.EnableControlThisFrame(2, con);
        }

        private bool IsGamepad()
        {
            return Game.CurrentInputMode == InputMode.GamePad;
        }

        public bool JustPressedUp()
        {
            if (IsGamepad() && Game.IsControlPressed(2, Control.PhoneUp) || Game.IsKeyPressed(Keys.NumPad8) ||
                Game.IsKeyPressed(Keys.Up))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
                    return true;
                }
            return false;
        }

        public bool JustPressedDown()
        {
            if (IsGamepad() && Game.IsControlPressed(2, Control.PhoneDown) || Game.IsKeyPressed(Keys.NumPad2) ||
                Game.IsKeyPressed(Keys.Down))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_UPDOWN, AUDIO_LIBRARY);
                    return true;
                }
            return false;
        }

        public bool JustPressedLeft()
        {
            if (IsGamepad() && Game.IsControlPressed(2, Control.PhoneLeft) || Game.IsKeyPressed(Keys.NumPad4) ||
                Game.IsKeyPressed(Keys.Left))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
                    return true;
                }
            return false;
        }

        public bool JustPressedRight()
        {
            if (IsGamepad() && Game.IsControlPressed(2, Control.PhoneRight) || Game.IsKeyPressed(Keys.NumPad6) ||
                Game.IsKeyPressed(Keys.Right))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_LEFTRIGHT, AUDIO_LIBRARY);
                    return true;
                }
            return false;
        }

        /*public bool JustPressedAccept()
        {
            if ((IsGamepad() && Game.IsControlJustPressed(2, Control.PhoneSelect)) || AcceptPressed)
            {
                Game.PlaySound(AUDIO_SELECT, AUDIO_LIBRARY);
                //AcceptPressed = false;
                return true;
            }
            return false;
        }

        public bool JustPressedCancel()
        {
            if ((IsGamepad() && Game.IsControlJustPressed(2, Control.PhoneCancel)) || CancelPressed)
            {
                Game.PlaySound(AUDIO_BACK, AUDIO_LIBRARY);
                //CancelPressed = false;
                return true;
            }
            return false;
        }*/

        public bool JustPressedAccept()
        {
            if (Game.IsControlPressed(2, Control.PhoneSelect) || Game.IsKeyPressed(Keys.NumPad5) ||
                Game.IsKeyPressed(Keys.Enter))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_SELECT, AUDIO_LIBRARY);
                    //InputTimer = Game.GameTime + 350;
                    return true;
                }
            return false;
        }

        public bool JustPressedCancel()
        {
            if (Game.IsControlPressed(2, Control.PhoneCancel) || Game.IsKeyPressed(Keys.NumPad0) ||
                Game.IsKeyPressed(Keys.Back))
                if (InputTimer < DateTime.Now)
                {
                    Game.PlaySound(AUDIO_BACK, AUDIO_LIBRARY);
                    //InputTimer = Game.GameTime + InputWait;
                    return true;
                }
            return false;
        }

        private bool IsHoldingSpeedupControl()
        {
            if (IsGamepad())
                return Game.IsControlPressed(2, Control.VehicleHandbrake);
            return Game.IsKeyPressed(Keys.ShiftKey);
        }

        public void SetInputWait(int ms = 350)
        {
            InputTimer = DateTime.Now.AddMilliseconds(ms);
        }

        public bool ControlBoolValue(UIMenuItem item, bool boolToControl)
        {
            if (IsVisible && SelectedItem == item)
            {
                //if (JustPressedAccept())
                //{
                boolToControl = !boolToControl;
                item.Value = boolToControl;
                //InputTimer = DateTime.Now.AddMilliseconds(InputWait);
                return boolToControl;
                //}
            }
            item.Value = boolToControl;
            return boolToControl;
        }

        public bool ControlBoolValue_NoEvent(UIMenuItem item, bool boolToControl)
        {
            item.Value = boolToControl;

            if (IsVisible && SelectedItem == item)
                if (JustPressedAccept())
                {
                    boolToControl = !boolToControl;
                    item.Value = boolToControl;
                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);
                    return boolToControl;
                }
            return boolToControl;
        }

        public float ControlFloatValue(UIMenuItem item, bool left, float numberToControl, float incrementValue,
            float incrementValueFast, int decimals = 2, bool limit = false, float min = 0f, float max = 1f)
        {
            if (IsVisible && SelectedItem == item)
            {
                if (left)
                    if (IsHoldingSpeedupControl())
                        numberToControl -= incrementValueFast;
                    else
                        numberToControl -= incrementValue;
                if (!left)
                    if (IsHoldingSpeedupControl())
                        numberToControl += incrementValueFast;
                    else
                        numberToControl += incrementValue;
                if (limit)
                {
                    if (numberToControl < min)
                        numberToControl = min;
                    if (numberToControl > max)
                        numberToControl = max;
                }

                item.Value = "< " + numberToControl + " >";

                //InputTimer = DateTime.Now.AddMilliseconds(InputWait);

                return (float) Math.Round(numberToControl, decimals);
            }
            item.Value = "< " + numberToControl + " >";
            return numberToControl;
        }

        public float ControlFloatValue_NoEvent(UIMenuItem item, float numberToControl, float incrementValue,
            float incrementValueFast, int decimals = 2, bool limit = false, float min = 0f, float max = 1f)
        {
            item.Value = "< " + numberToControl + " >";

            if (IsVisible && SelectedItem == item)
            {
                if (JustPressedLeft())
                {
                    if (IsHoldingSpeedupControl())
                        numberToControl -= incrementValueFast;
                    else
                        numberToControl -= incrementValue;

                    if (limit)
                    {
                        if (numberToControl < min)
                            numberToControl = min;
                        if (numberToControl > max)
                            numberToControl = max;
                    }

                    item.Value = "< " + numberToControl + " >";

                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);

                    return (float) Math.Round(numberToControl, decimals);
                }
                if (JustPressedRight())
                {
                    if (IsHoldingSpeedupControl())
                        numberToControl += incrementValueFast;
                    else
                        numberToControl += incrementValue;

                    if (limit)
                    {
                        if (numberToControl < min)
                            numberToControl = min;
                        if (numberToControl > max)
                            numberToControl = max;
                    }

                    item.Value = "< " + numberToControl + " >";

                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);

                    return (float) Math.Round(numberToControl, decimals);
                }
            }
            return numberToControl;
        }

        public int ControlIntValue(UIMenuItem item, bool left, int numberToControl, int incrementValue,
            int incrementValueFast, bool limit = false, int min = 0, int max = 100)
        {
            if (IsVisible && SelectedItem == item)
            {
                if (left)
                    if (IsHoldingSpeedupControl())
                        numberToControl -= incrementValueFast;
                    else
                        numberToControl -= incrementValue;
                if (!left)
                    if (IsHoldingSpeedupControl())
                        numberToControl += incrementValueFast;
                    else
                        numberToControl += incrementValue;
                if (limit)
                    if (numberToControl < min)
                        numberToControl = min;
                    else if (numberToControl > max)
                        numberToControl = max;

                item.Value = "< " + numberToControl + " >";

                //InputTimer = DateTime.Now.AddMilliseconds(InputWait);
            }
            item.Value = "< " + numberToControl + " >";
            return numberToControl;
        }

        public int ControlIntValue_NoEvent(UIMenuItem item, int numberToControl, int incrementValue,
            int incrementValueFast, bool limit = false, int min = 0, int max = 100)
        {
            item.Value = "< " + numberToControl + " >";

            if (IsVisible && SelectedItem == item)
            {
                if (JustPressedLeft())
                {
                    if (IsHoldingSpeedupControl())
                        numberToControl -= incrementValueFast;
                    else
                        numberToControl -= incrementValue;

                    if (limit)
                    {
                        if (numberToControl < min)
                            numberToControl = min;
                        if (numberToControl > max)
                            numberToControl = max;
                    }

                    item.Value = "< " + numberToControl + " >";

                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);

                    return numberToControl;
                }
                if (JustPressedRight())
                {
                    if (IsHoldingSpeedupControl())
                        numberToControl += incrementValueFast;
                    else
                        numberToControl += incrementValue;

                    if (limit)
                    {
                        if (numberToControl < min)
                            numberToControl = min;
                        if (numberToControl > max)
                            numberToControl = max;
                    }

                    item.Value = "< " + numberToControl + " >";

                    InputTimer = DateTime.Now.AddMilliseconds(InputWait);

                    return numberToControl;
                }
            }
            return numberToControl;
        }

        protected virtual void ItemSelect(UIMenuItem selecteditem, int index)
        {
            OnItemSelect?.Invoke(this, selecteditem, index);
        }

        protected virtual void ItemLeftRight(UIMenuItem selecteditem, int index, bool left)
        {
            OnItemLeftRight?.Invoke(this, selecteditem, index, left);
        }
    }
}