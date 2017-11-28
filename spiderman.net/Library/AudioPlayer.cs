using NAudio.Wave;
using System.IO;

namespace spiderman.net.Library
{
    /// <summary>
    /// Holds helper methods that handle external audio.
    /// </summary>
    public static class AudioPlayer
    {
        /// <summary>
        /// The path to our audio.
        /// </summary>
        public const string MainPath = ".\\scripts\\Spider-Man Files\\";

        /// <summary>
        /// Plays a sound from the given path. Must be .wav format.
        /// </summary>
        /// <param name="path">The path to the sound.</param>
        /// <param name="volume">The sounds volume.</param>
        public static void PlaySound(string path, float volume)
        {
            using (var wr = new WaveFileReader(path))
            {
                var output = new DirectSoundOut();
                var w32 = new WaveChannel32(wr);
                w32.Volume = volume;
                output.Init(w32);
                output.Play();
                wr.Close();
            }
        }
    }
}
