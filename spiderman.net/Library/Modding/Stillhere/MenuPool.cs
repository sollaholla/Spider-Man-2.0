using System.Collections.Generic;
using System.Linq;

namespace SpiderMan.Library.Modding.Stillhere
{
    internal class MenuPool
    {
        public UIMenu LastUsedMenu { get; set; }

        public List<UIMenu> UIMenuList { get; set; } = new List<UIMenu>();

        public void AddMenu(UIMenu menu)
        {
            UIMenuList.Add(menu);
            if (UIMenuList.Count == 1) LastUsedMenu = menu;
        }

        /// <summary>
        ///     Adds a submenu to a parent menu and to the MenuPool. Returns UIMenuItem that links the parent menu to the submenu.
        /// </summary>
        /// <param name="SubMenu">The submenu</param>
        /// <param name="ParentMenu">The parent menu.</param>
        /// <param name="text">The text of the menu item in the parent menu that leads to the submenu when entered.</param>
        public void AddSubMenu(UIMenu SubMenu, UIMenu ParentMenu, string text, bool UseSameColorsAsParent = true)
        {
            AddMenu(SubMenu);
            /*SubMenu.ParentMenu = ParentMenu;
            ParentMenu.NextMenu = SubMenu;*/
            var item = new UIMenuItem(
                text + "  ~r~>"); //colour codes: gtaforums.com/topic/820813-displaying-help-text/?p=1067993556
            //ParentMenu.BindingMenuItem = BindingItem;
            ParentMenu.AddMenuItem(item);
            //ParentMenu.BindingMenuItem = item;
            ParentMenu.BindItemToSubmenu(SubMenu, item);

            if (UseSameColorsAsParent)
            {
                SubMenu.TitleColor = ParentMenu.TitleColor;
                SubMenu.TitleUnderlineColor = ParentMenu.TitleUnderlineColor;
                SubMenu.TitleBackgroundColor = ParentMenu.TitleBackgroundColor;

                SubMenu.DefaultTextColor = ParentMenu.DefaultTextColor;
                SubMenu.DefaultBoxColor = ParentMenu.DefaultBoxColor;
                SubMenu.HighlightedItemTextColor = ParentMenu.HighlightedItemTextColor;
                SubMenu.HighlightedBoxColor = ParentMenu.HighlightedBoxColor;

                SubMenu.DescriptionTextColor = ParentMenu.DescriptionTextColor;
                SubMenu.DescriptionBoxColor = ParentMenu.DescriptionBoxColor;
            }
        }

        /// <summary>
        ///     Adds a submenu to a parent menu and to the MenuPool. Returns UIMenuItem that links the parent menu to the submenu.
        /// </summary>
        /// <param name="SubMenu">The submenu</param>
        /// <param name="ParentMenu">The parent menu.</param>
        /// <param name="text">The text of the menu item in the parent menu that leads to the submenu when entered.</param>
        /// <param name="description">The description of the menu item that leads to the submenu when entered.</param>
        public void AddSubMenu(UIMenu SubMenu, UIMenu ParentMenu, string text, string description,
            bool UseSameColorsAsParent = true)
        {
            AddMenu(SubMenu);
            //SubMenu.ParentMenu = ParentMenu;
            //ParentMenu.NextMenu = SubMenu;
            var item = new UIMenuItem(text + "  ~r~>", null, description);
            //ParentMenu.BindingMenuItem = BindingItem;
            ParentMenu.AddMenuItem(item);
            //ParentMenu.BindingMenuItem = item;
            ParentMenu.BindItemToSubmenu(SubMenu, item);

            if (UseSameColorsAsParent)
            {
                SubMenu.TitleColor = ParentMenu.TitleColor;
                SubMenu.TitleUnderlineColor = ParentMenu.TitleUnderlineColor;
                SubMenu.TitleBackgroundColor = ParentMenu.TitleBackgroundColor;

                SubMenu.DefaultTextColor = ParentMenu.DefaultTextColor;
                SubMenu.DefaultBoxColor = ParentMenu.DefaultBoxColor;
                SubMenu.HighlightedItemTextColor = ParentMenu.HighlightedItemTextColor;
                SubMenu.HighlightedBoxColor = ParentMenu.HighlightedBoxColor;

                SubMenu.DescriptionTextColor = ParentMenu.DescriptionTextColor;
                SubMenu.DescriptionBoxColor = ParentMenu.DescriptionBoxColor;
            }
        }

        /// <summary>
        ///     Draws all visible menus.
        /// </summary>
        public void Draw()
        {
            foreach (var menu in UIMenuList.Where(menu => menu.IsVisible))
            {
                menu.Draw();
                SetLastUsedMenu(menu);
            }
        }

        /// <summary>
        ///     Set the last used menu.
        /// </summary>
        public void SetLastUsedMenu(UIMenu menu)
        {
            LastUsedMenu = menu;
        }

        /// <summary>
        ///     Process all of your menus' functions. Call this in a tick event.
        /// </summary>
        public void ProcessMenus()
        {
            if (LastUsedMenu == null)
                LastUsedMenu = UIMenuList[0];
            Draw();
        }

        /// <summary>
        ///     Checks if any menu is currently visible.
        /// </summary>
        /// <returns>true if at least one menu is visible, false if not.</returns>
        public bool IsAnyMenuOpen()
        {
            return UIMenuList.Any(menu => menu.IsVisible);
        }

        /// <summary>
        ///     Closes all of your menus.
        /// </summary>
        public void CloseAllMenus()
        {
            foreach (var menu in UIMenuList.Where(menu => menu.IsVisible))
                menu.IsVisible = false;
        }

        public void RemoveAllMenus()
        {
            UIMenuList.Clear();
        }

        public void OpenCloseLastMenu()
        {
            if (IsAnyMenuOpen())
                CloseAllMenus();
            else
                LastUsedMenu.IsVisible = !LastUsedMenu.IsVisible;
        }
    }

    /*public class UIMenuListItem : UIMenuItem
    {
        public List<dynamic> List { get; set; }
        public int SelectedIndex = 0;

        public UIMenuListItem(string text, dynamic value, string description, List<dynamic> list)
        {
            this.Text = text;
            this.Value = value;
            this.Description = description;
            List = list;
        }

        public override void ChangeListIndex()
        {

        }
    }*/

    /*public static class SplitStringByLength
    {
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            for (int index = 0; index < str.Length; index += maxLength)
            {
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
            }
        }
    }*/

    // using System.Text.RegularExpressions;
}