using System.Drawing;
using GTA.Native;

namespace SpiderMan.Library.Types
{
    public class ParticleLooped
    {
        /// <summary>
        ///     The main constructor.
        /// </summary>
        /// <param name="handle">The particles internal index.</param>
        public ParticleLooped(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        ///     The internal index of this particle.
        /// </summary>
        public int Handle { get; }

        /// <summary>
        ///     Returns true if this particle exists in memory.
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            return Function.Call<bool>(Hash.DOES_PARTICLE_FX_LOOPED_EXIST, Handle);
        }

        /// <summary>
        ///     Set's the color of the particle. The particle must be playing first.
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, Handle, color.R, color.G, color.B, false);
            //Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, Handle, color.A);
        }

        /// <summary>
        ///     Set the specified properties evolution.
        /// </summary>
        /// <param name="propertyName">The name of the propery.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="id">Not sure...</param>
        public void SetEvolution(string propertyName, float value, bool id = false)
        {
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_EVOLUTION, Handle, propertyName, value, id);
        }

        /// <summary>
        ///     Removes this particle from the game.
        /// </summary>
        public void Remove()
        {
            Function.Call(Hash.REMOVE_PARTICLE_FX, Handle, true);
        }

        /// <summary>
        ///     Similar to Remove(), except this slowly fades out the particle before removing
        ///     it from the game.
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            Function.Call(Hash.STOP_PARTICLE_FX_LOOPED, Handle, true);
        }
    }
}