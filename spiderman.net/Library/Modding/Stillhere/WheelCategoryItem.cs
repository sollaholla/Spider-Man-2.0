namespace SpiderMan.Library.Modding.Stillhere
{
    /// <summary>
    ///     Holds information about a wheel category slot.
    /// </summary>
    public class WheelCategoryItem
    {
        /// <summary>
        ///     Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">
        ///     Name of the item. If a matching .png image is found, the image will be displayed assuming no image
        ///     for this item's category has been found.
        /// </param>
        public WheelCategoryItem(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Instantiate a new item to be later added to a WheelCategory.
        /// </summary>
        /// <param name="name">
        ///     Name of the item. If a matching .png image is found,
        ///     the image will be displayed assuming no image for this item's category has been found.
        /// </param>
        /// <param name="description">A description that will be displayed on the right side of the screen.</param>
        public WheelCategoryItem(string name, string description) : this(name)
        {
            Description = description;
        }

        /// <summary>
        ///     Information to easily identify this item.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        ///     The name of this category item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The item's texture information.
        /// </summary>
        public Texture2D ItemTexture { get; set; }

        /// <summary>
        ///     The item's description.
        /// </summary>
        public string Description { get; }
    }
}