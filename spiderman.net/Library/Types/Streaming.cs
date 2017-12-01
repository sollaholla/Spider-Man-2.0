using GTA.Native;

namespace SpiderMan.Library.Types
{
    /// <summary>
    ///     A class to handle streaming assets.
    /// </summary>
    public static class Streaming
    {
        /// <summary>
        ///     Requests the specified animation dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        public static void RequestAnimationDictionary(string dictionary)
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, dictionary);
        }

        public static bool RequestTextureDictionary(string dictionary)
        {
            if (Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, dictionary))
                return true;
            Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, dictionary);
            return false;
        }
    }
}