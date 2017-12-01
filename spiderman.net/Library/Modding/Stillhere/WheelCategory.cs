using System.Collections.Generic;
using System.Linq;
using GTA.Math;

namespace SpiderMan.Library.Modding.Stillhere
{
    public class WheelCategory
    {
        public Texture2D CategoryTexture;
        public int CurrentItemIndex;
        public int ID;
        protected List<WheelCategoryItem> Items = new List<WheelCategoryItem>();
        public string Name;
        public Vector2 position2D = Vector2.Zero;

        /// <summary>
        ///     Instantiates a new category for use in a selection wheel.
        /// </summary>
        /// <param name="name">
        ///     Name of the category. If a matching .png image is found, the image will be displayed instead of any
        ///     item image.
        /// </param>
        public WheelCategory(string name)
        {
            Name = name;
        }

        public List<WheelCategoryItem> ItemList => Items;

        public WheelCategoryItem SelectedItem => Items.ElementAt(CurrentItemIndex);

        /// <summary>
        ///     Add item to this category.
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

        public bool IsItemSelected(WheelCategoryItem item)
        {
            if (Items.Contains(item))
                return Items.IndexOf(item) == CurrentItemIndex;
            return false;
        }

        public void GoToNextItem()
        {
            if (CurrentItemIndex < Items.Count - 1)
                CurrentItemIndex++;
            else
                CurrentItemIndex = 0;
        }

        public void GoToPreviousItem()
        {
            if (CurrentItemIndex > 0)
                CurrentItemIndex--;
            else
                CurrentItemIndex = Items.Count - 1;
        }
    }
}