using System;
using GTA;
using GTA.Native;

namespace SpiderMan.Library.Modding.Stillhere
{
    public static class StringHelper
    {
        public static void AddLongString(string str)
        {
            const int strLen = 99;
            for (var i = 0; i < str.Length; i += strLen)
            {
                var substr = str.Substring(i, Math.Min(strLen, str.Length - i));
                Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, substr); //ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME
            }
        }

        public static float MeasureStringWidth(string str, Font font, float fontsize)
        {
            //int screenw = 2560;// Game.ScreenResolution.Width;
            //int screenh = 1440;// Game.ScreenResolution.Height;
            const float height = 1080f;
            var ratio = (float) Game.ScreenResolution.Width / Game.ScreenResolution.Height;
            var width = height * ratio;
            return MeasureStringWidthNoConvert(str, font, fontsize) * width;
        }

        private static float MeasureStringWidthNoConvert(string str, Font font, float fontsize)
        {
            Function.Call((Hash) 0x54CE8AC98E120CAB, "jamyfafi"); //_BEGIN_TEXT_COMMAND_WIDTH
            AddLongString(str);
            Function.Call(Hash.SET_TEXT_FONT, (int) font);
            Function.Call(Hash.SET_TEXT_SCALE, fontsize, fontsize);
            return
                Function.Call<float>(Hash._0x85F061DA64ED2F67,
                    true); //_END_TEXT_COMMAND_GET_WIDTH //Function.Call<float>((Hash)0x85F061DA64ED2F67, (int)font) * fontsize; //_END_TEXT_COMMAND_GET_WIDTH
        }
    }
}