using spiderman.net.Abilities.WebTech;
using System.Collections.Generic;

namespace spiderman.net.Abilities.Types
{
    /// <summary>
    /// Defines data about a category.
    /// </summary>
    public class CategorySlot
    {
        public Tech m_ActivateTech;

        public CategorySlot(string categoryName, int id, List<Tech> tech, Tech activateTech)
        {
            ID = id;
            CategoryName = categoryName;
            Tech = tech;
            m_ActivateTech = activateTech;
        }

        public string CategoryName { get; set; }
        public List<Tech> Tech { get; set; }
        public int ID { get; set; }
    }
}
