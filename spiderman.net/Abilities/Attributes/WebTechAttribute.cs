using System;

namespace SpiderMan.Abilities.Attributes
{
    public class WebTechAttribute : Attribute
    {
        /// <summary>
        ///     The main ctor.
        /// </summary>
        /// <param name="categoryName">The name of the category for this web tech.</param>
        public WebTechAttribute(string categoryName)
        {
            CategoryName = categoryName;
        }

        public string CategoryName { get; }

        /// <summary>
        ///     Set to true if you wish for this ability to be
        ///     the default ability for the category.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}