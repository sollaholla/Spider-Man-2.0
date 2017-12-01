using System.Drawing;
using GTA;

namespace SpiderMan.Library.Modding.Stillhere
{
    /// <summary>
    ///     A class that allows drawing and manipulation of custom textures.
    /// </summary>
    public class Texture2D
    {
        /// <summary>
        ///     The main ctor.
        /// </summary>
        /// <param name="path">The path to the texture file.</param>
        /// <param name="index">The draw index of the texture.</param>
        public Texture2D(string path, int index)
        {
            Path = path;
            Index = index;
        }

        /// <summary>
        ///     The texture path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     The texture's draw index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     The level (above others) which to draw this.
        /// </summary>
        public int DrawLevel { get; set; }

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

        public void Draw(int level, int time, Point pos, PointF center, Size size, float rotation, Color color,
            float aspectRatio)
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
}