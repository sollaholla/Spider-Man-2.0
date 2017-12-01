namespace SpiderMan.Library.Modding.Stillhere
{
    public class UIMenuDisplayOnly : UIMenu
    {
        public UIMenuDisplayOnly(string text) : base(text)
        {
            base.TitleFontSize = 0.5f;
            base.boxWidth = 400;

            CalculateMenuPositioning();

            MaxItemsInMenu(8);
        }

        public override void Draw()
        {
            if (IsVisible)
            {
                DisplayMenu();
                DisableControls();
                DrawScrollBar();
                //ManageCurrentIndex();
            }
        }

        protected override void ManageCurrentIndex()
        {
            //base.ManageCurrentIndex();
        }

        public void GoToNextItem()
        {
            SelectedIndex++;
            if (SelectedIndex >= maxItem + 1)
            {
                minItem++;
                maxItem++;
            }
        }

        public void GoToFirstItem()
        {
            SelectedIndex = 0;
            minItem = 0;
            maxItem = MaxItemsOnScreen - 1;
        }

        public void GoToPreviousItem()
        {
            SelectedIndex--;
            if (SelectedIndex < minItem && minItem > 0)
            {
                minItem--;
                maxItem--;
            }
        }

        public void GoToLastItem()
        {
            SelectedIndex = _itemList.Count - 1;
            minItem = _itemList.Count - MaxItemsOnScreen;
            maxItem = _itemList.Count - 1;
        }
    }
}