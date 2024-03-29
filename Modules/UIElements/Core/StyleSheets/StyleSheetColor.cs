// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class StyleSheetColor
    {
        public static bool TryGetColor(string name, out Color color)
        {
            Color32 c32;
            bool result = s_NameToColor.TryGetValue(name, out c32);
            color = c32;

            return result;
        }

        private static Dictionary<string, Color32> s_NameToColor = new Dictionary<string, Color32>()
        {
            {"aliceblue", HexToColor32(0xf0f8ff)},
            {"antiquewhite", HexToColor32(0xfaebd7)},
            {"aqua", HexToColor32(0x00ffff)},
            {"aquamarine", HexToColor32(0x7fffd4)},
            {"azure", HexToColor32(0xf0ffff)},
            {"beige", HexToColor32(0xf5f5dc)},
            {"bisque", HexToColor32(0xffe4c4)},
            {"black", HexToColor32(0x000000)},
            {"blanchedalmond", HexToColor32(0xffebcd)},
            {"blue", HexToColor32(0x0000ff)},
            {"blueviolet", HexToColor32(0x8a2be2)},
            {"brown", HexToColor32(0xa52a2a)},
            {"burlywood", HexToColor32(0xdeb887)},
            {"cadetblue", HexToColor32(0x5f9ea0)},
            {"chartreuse", HexToColor32(0x7fff00)},
            {"chocolate", HexToColor32(0xd2691e)},
            {"coral", HexToColor32(0xff7f50)},
            {"cornflowerblue", HexToColor32(0x6495ed)},
            {"cornsilk", HexToColor32(0xfff8dc)},
            {"crimson", HexToColor32(0xdc143c)},
            {"cyan", HexToColor32(0x00ffff)},
            {"darkblue", HexToColor32(0x00008b)},
            {"darkcyan", HexToColor32(0x008b8b)},
            {"darkgoldenrod", HexToColor32(0xb8860b)},
            {"darkgray", HexToColor32(0xa9a9a9)},
            {"darkgreen", HexToColor32(0x006400)},
            {"darkgrey", HexToColor32(0xa9a9a9)},
            {"darkkhaki", HexToColor32(0xbdb76b)},
            {"darkmagenta", HexToColor32(0x8b008b)},
            {"darkolivegreen", HexToColor32(0x556b2f)},
            {"darkorange", HexToColor32(0xff8c00)},
            {"darkorchid", HexToColor32(0x9932cc)},
            {"darkred", HexToColor32(0x8b0000)},
            {"darksalmon", HexToColor32(0xe9967a)},
            {"darkseagreen", HexToColor32(0x8fbc8f)},
            {"darkslateblue", HexToColor32(0x483d8b)},
            {"darkslategray", HexToColor32(0x2f4f4f)},
            {"darkslategrey", HexToColor32(0x2f4f4f)},
            {"darkturquoise", HexToColor32(0x00ced1)},
            {"darkviolet", HexToColor32(0x9400d3)},
            {"deeppink", HexToColor32(0xff1493)},
            {"deepskyblue", HexToColor32(0x00bfff)},
            {"dimgray", HexToColor32(0x696969)},
            {"dimgrey", HexToColor32(0x696969)},
            {"dodgerblue", HexToColor32(0x1e90ff)},
            {"firebrick", HexToColor32(0xb22222)},
            {"floralwhite", HexToColor32(0xfffaf0)},
            {"forestgreen", HexToColor32(0x228b22)},
            {"fuchsia", HexToColor32(0xff00ff)},
            {"gainsboro", HexToColor32(0xdcdcdc)},
            {"ghostwhite", HexToColor32(0xf8f8ff)},
            {"goldenrod", HexToColor32(0xdaa520)},
            {"gold", HexToColor32(0xffd700)},
            {"gray", HexToColor32(0x808080)},
            {"green", HexToColor32(0x008000)},
            {"greenyellow", HexToColor32(0xadff2f)},
            {"grey", HexToColor32(0x808080)},
            {"honeydew", HexToColor32(0xf0fff0)},
            {"hotpink", HexToColor32(0xff69b4)},
            {"indianred", HexToColor32(0xcd5c5c)},
            {"indigo", HexToColor32(0x4b0082)},
            {"ivory", HexToColor32(0xfffff0)},
            {"khaki", HexToColor32(0xf0e68c)},
            {"lavenderblush", HexToColor32(0xfff0f5)},
            {"lavender", HexToColor32(0xe6e6fa)},
            {"lawngreen", HexToColor32(0x7cfc00)},
            {"lemonchiffon", HexToColor32(0xfffacd)},
            {"lightblue", HexToColor32(0xadd8e6)},
            {"lightcoral", HexToColor32(0xf08080)},
            {"lightcyan", HexToColor32(0xe0ffff)},
            {"lightgoldenrodyellow", HexToColor32(0xfafad2)},
            {"lightgray", HexToColor32(0xd3d3d3)},
            {"lightgreen", HexToColor32(0x90ee90)},
            {"lightgrey", HexToColor32(0xd3d3d3)},
            {"lightpink", HexToColor32(0xffb6c1)},
            {"lightsalmon", HexToColor32(0xffa07a)},
            {"lightseagreen", HexToColor32(0x20b2aa)},
            {"lightskyblue", HexToColor32(0x87cefa)},
            {"lightslategray", HexToColor32(0x778899)},
            {"lightslategrey", HexToColor32(0x778899)},
            {"lightsteelblue", HexToColor32(0xb0c4de)},
            {"lightyellow", HexToColor32(0xffffe0)},
            {"lime", HexToColor32(0x00ff00)},
            {"limegreen", HexToColor32(0x32cd32)},
            {"linen", HexToColor32(0xfaf0e6)},
            {"magenta", HexToColor32(0xff00ff)},
            {"maroon", HexToColor32(0x800000)},
            {"mediumaquamarine", HexToColor32(0x66cdaa)},
            {"mediumblue", HexToColor32(0x0000cd)},
            {"mediumorchid", HexToColor32(0xba55d3)},
            {"mediumpurple", HexToColor32(0x9370db)},
            {"mediumseagreen", HexToColor32(0x3cb371)},
            {"mediumslateblue", HexToColor32(0x7b68ee)},
            {"mediumspringgreen", HexToColor32(0x00fa9a)},
            {"mediumturquoise", HexToColor32(0x48d1cc)},
            {"mediumvioletred", HexToColor32(0xc71585)},
            {"midnightblue", HexToColor32(0x191970)},
            {"mintcream", HexToColor32(0xf5fffa)},
            {"mistyrose", HexToColor32(0xffe4e1)},
            {"moccasin", HexToColor32(0xffe4b5)},
            {"navajowhite", HexToColor32(0xffdead)},
            {"navy", HexToColor32(0x000080)},
            {"oldlace", HexToColor32(0xfdf5e6)},
            {"olive", HexToColor32(0x808000)},
            {"olivedrab", HexToColor32(0x6b8e23)},
            {"orange", HexToColor32(0xffa500)},
            {"orangered", HexToColor32(0xff4500)},
            {"orchid", HexToColor32(0xda70d6)},
            {"palegoldenrod", HexToColor32(0xeee8aa)},
            {"palegreen", HexToColor32(0x98fb98)},
            {"paleturquoise", HexToColor32(0xafeeee)},
            {"palevioletred", HexToColor32(0xdb7093)},
            {"papayawhip", HexToColor32(0xffefd5)},
            {"peachpuff", HexToColor32(0xffdab9)},
            {"peru", HexToColor32(0xcd853f)},
            {"pink", HexToColor32(0xffc0cb)},
            {"plum", HexToColor32(0xdda0dd)},
            {"powderblue", HexToColor32(0xb0e0e6)},
            {"purple", HexToColor32(0x800080)},
            {"rebeccapurple", HexToColor32(0x663399)},
            {"red", HexToColor32(0xff0000)},
            {"rosybrown", HexToColor32(0xbc8f8f)},
            {"royalblue", HexToColor32(0x4169e1)},
            {"saddlebrown", HexToColor32(0x8b4513)},
            {"salmon", HexToColor32(0xfa8072)},
            {"sandybrown", HexToColor32(0xf4a460)},
            {"seagreen", HexToColor32(0x2e8b57)},
            {"seashell", HexToColor32(0xfff5ee)},
            {"sienna", HexToColor32(0xa0522d)},
            {"silver", HexToColor32(0xc0c0c0)},
            {"skyblue", HexToColor32(0x87ceeb)},
            {"slateblue", HexToColor32(0x6a5acd)},
            {"slategray", HexToColor32(0x708090)},
            {"slategrey", HexToColor32(0x708090)},
            {"snow", HexToColor32(0xfffafa)},
            {"springgreen", HexToColor32(0x00ff7f)},
            {"steelblue", HexToColor32(0x4682b4)},
            {"tan", HexToColor32(0xd2b48c)},
            {"teal", HexToColor32(0x008080)},
            {"thistle", HexToColor32(0xd8bfd8)},
            {"tomato", HexToColor32(0xff6347)},
            {"transparent", new Color32(0, 0, 0, 0)},
            {"turquoise", HexToColor32(0x40e0d0)},
            {"violet", HexToColor32(0xee82ee)},
            {"wheat", HexToColor32(0xf5deb3)},
            {"white", HexToColor32(0xffffff)},
            {"whitesmoke", HexToColor32(0xf5f5f5)},
            {"yellow", HexToColor32(0xffff00)},
            {"yellowgreen", HexToColor32(0x9acd32)}
        };

        private static Color32 HexToColor32(uint color)
        {
            byte blue = (byte)(color & 255);
            byte green = (byte)((color >> 8) & 255);
            byte red = (byte)((color >> 16) & 255);

            return new Color32(red, green, blue, 255);
        }
    }
}
