using System;
using System.Numerics;

namespace HotbarTimers
{
    [Serializable]
    public class TextConfig
    {
        public FontType FontType { get; set; }
        public int FontSize { get; set; }
        public Vector4 FontColor {get;set;}

        public TextConfig(FontType fontType, int fontSize, Vector4 fontColor)
        {
            FontType = fontType;
            FontSize = fontSize;
            FontColor = fontColor;
        }
    }
}
