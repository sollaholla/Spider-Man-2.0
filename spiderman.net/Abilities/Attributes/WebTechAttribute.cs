using System;

namespace spiderman.net.Abilities.Attributes
{
    public class WebTechAttribute : Attribute
    {
        public WebTechAttribute(string categoryName)
        {
            CategoryName = categoryName;
        }

        public string CategoryName { get; }
        public bool IsDefault { get; set; }
    }
}
