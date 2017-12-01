namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     A structure that contains information about a particle.
    /// </summary>
    public struct Particle
    {
        /// <summary>
        ///     The main constructor.
        /// </summary>
        /// <param name="handle">The internal index of this object.</param>
        public Particle(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        ///     Get's the handle of the PTFX.
        /// </summary>
        public int Handle { get; }
    }
}