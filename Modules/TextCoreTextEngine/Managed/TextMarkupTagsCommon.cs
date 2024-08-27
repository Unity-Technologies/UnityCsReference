// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Rich Text Tags and Attribute definitions and their respective HashCode values.
    /// </summary>
    internal enum MarkupTag : int
    {
        BOLD = 66,                          // <b>
        SLASH_BOLD = 1613,                  // </b>
        ITALIC = 73,                        // <i>
        SLASH_ITALIC = 1606,                // </i>
        UNDERLINE = 85,                     // <u>
        SLASH_UNDERLINE = 1626,             // </u>
        STRIKETHROUGH = 83,                 // <s>
        SLASH_STRIKETHROUGH = 1628,         // </s>
        MARK = 2699125,                     // <mark>
        SLASH_MARK = 57644506,              // </mark>
        SUBSCRIPT = 92132,                  // <sub>
        SLASH_SUBSCRIPT = 1770219,          // </sub>
        SUPERSCRIPT = 92150,                // <sup>
        SLASH_SUPERSCRIPT = 1770233,        // </sup>
        COLOR = 81999901,                   // <color>
        SLASH_COLOR = 1909026194,           // </color>
        ALPHA = 75165780,                   // <alpha>
        A = 65,                             // <a>
        SLASH_A = 1614,                     // </a>
        SIZE = 3061285,                     // <size>
        SLASH_SIZE = 58429962,              // </size>
        SPRITE = -991527447,                // <sprite>
        NO_BREAK = 2856657,                 // <nobr>
        SLASH_NO_BREAK = 57477502,          // </nobr>
        STYLE = 100252951,                  // <style>
        SLASH_STYLE = 1927738392,           // </style>
        FONT = 2586451,                     // <font>
        SLASH_FONT = 57747708,              // </font>
        SLASH_MATERIAL = -1100708252,       // </material>
        LINK = 2656128,                     // <link>
        SLASH_LINK = 57686191,              // </link>
        FONT_WEIGHT = -1889896162,          // <font-weight=xxx>
        SLASH_FONT_WEIGHT = -757976431,     // </font-weight>
        NO_PARSE = -408011596,              // <noparse>
        SLASH_NO_PARSE = -294095813,        // </noparse>
        POSITION = 85420,                   // <pos>
        SLASH_POSITION = 1777699,           // </pos>
        VERTICAL_OFFSET = 1952379995,       // <voffset>
        SLASH_VERTICAL_OFFSET = -11107948,  // </voffset>
        SPACE = 100083556,                  // <space>
        SLASH_SPACE = 1927873067,           // </space>
        PAGE = 2808691,                     // <page>
        SLASH_PAGE = 58683868,              // </page>
        ALIGN = 75138797,                   // <align>
        SLASH_ALIGN = 1916026786,           // </align>
        WIDTH = 105793766,                  // <width>
        SLASH_WIDTH = 1923459625,           // </width>
        GRADIENT = -1999759898,             // <gradient>
        SLASH_GRADIENT = -1854491959,       // </gradient>
        CHARACTER_SPACE = -1584382009,      // <cspace>
        SLASH_CHARACTER_SPACE = -1394426712,// </cspace>
        MONOSPACE = -1340221943,            // <mspace>
        SLASH_MONOSPACE = -1638865562,      // </mspace>
        CLASS = 82115566,                   // <class>
        INDENT = -1514123076,               // <indent>
        SLASH_INDENT = -1496889389,         // </indent>
        LINE_INDENT = -844305121,           // <line-indent>
        SLASH_LINE_INDENT = 93886352,       // </line-indent>
        MARGIN = -1355614050,               // <margin>
        SLASH_MARGIN = -1649644303,         // </margin>
        MARGIN_LEFT = -272933656,           // <margin-left>
        MARGIN_RIGHT = -447416589,          // <margin-right>
        LINE_HEIGHT = -799081892,           // <line-height>
        SLASH_LINE_HEIGHT = 200452819,      // </line-height>
        ACTION = -1827519330,               // <action>
        SLASH_ACTION = -1187217679,         // </action>
        SCALE = 100553336,                  // <scale>
        SLASH_SCALE = 1928413879,           // </scale>
        ROTATE = -1000007783,               // <rotate>
        SLASH_ROTATE = -764695562,          // </rotate>

        TABLE = 226476955,                  // <table>
        SLASH_TABLE = -979118220,           // </table>
        TH = 5862489,                       // <th>
        SLASH_TH = 193346070,               // </th>
        TR = 5862467,                       // <tr>
        SLASH_TR = 193346060,               // </tr>
        TD = 5862485,                       // <td>
        SLASH_TD = 193346074,               // </td>

        LOWERCASE = -1506899689,            // <lowercase>
        SLASH_LOWERCASE = -1451284584,      // </lowercase>
        ALLCAPS = 218273952,                // <allcaps>
        SLASH_ALLCAPS = -797437649,         // </allcaps>
        UPPERCASE = -305409418,             // <uppercase>
        SLASH_UPPERCASE = -582368199,       // </uppercase>
        SMALLCAPS = -766062114,             // <smallcaps>
        SLASH_SMALLCAPS = 199921873,        // </smallcaps>

        // Font Features
        LIGA = 2655971,                 // <liga>
        SLASH_LIGA = 57686604,          // </liga>
        FRAC = 2598518,                 // <frac>
        SLASH_FRAC = 57774681,          // </frac>

        // Attributes
        NAME = 2875623,                 // <sprite name="Name of Sprite">
        INDEX = 84268030,               // <sprite index=7>
        TINT = 2960519,                 // <tint=bool>
        ANIM = 2283339,                 // <anim="first frame, last frame, frame rate">
        MATERIAL = 825491659,           // <font="Name of font asset" material="Name of material">
        HREF = 2535353,                 // <a href="url">text to be displayed.</a>
        ANGLE = 75347905,               // <i angle="40">Italic Slant Angle</i>
        PADDING = -2144568463,
        FAMILYNAME = 704251153,
        STYLENAME = -1207081936,
        DUOSPACE = 582810522,           // <mspace="value" duospace=1> to enable this mode

        // Named Colors
        RED = 91635,
        GREEN = 87065851,
        BLUE = 2457214,
        YELLOW = -882444668,
        ORANGE = -1108587920,
        BLACK = 81074727,
        WHITE = 105680263,
        PURPLE = -1250222130,
        GREY = 2638345,
        LIGHTBLUE= 341063360,
        TEAL = 2947772,
        CYAN= 2504597,
        DARKBLUE= -1960309918,
        FUCHSIA= -1002715645,
        SILVER= -960329321,
        BROWN= 81017702,
        MAROON= -1355621936,
        OLIVE= 95492953,
        NAVY= 2876352,
        AQUA = 2284356,
        MAGENTA= -1812576107,
        TRANSPARENT= -1014785338,
        LIME= 2656045,

        // Unicode Characters
        BR = 2256,                          // <br> Line Feed (LF) \u0A
        CR = 2289,                          // <cr> Carriage Return (CR) \u000D
        ZWSP = 3288238,                     // <zwsp> Zero Width Space \u200B
        ZWJ = 99623,                        // <zwj> Zero Width Joiner \u200D
        NBSP = 2869039,                     // <nbsp> Non Breaking Space \u00A0
        SHY = 92674,                        // <SHY> Soft Hyphen \u00AD

        // Alignment
        LEFT = 2660507,                 // <align=left>
        RIGHT = 99937376,               // <align=right>
        CENTER = -1591113269,           // <align=center>
        JUSTIFIED = 817091359,          // <align=justified>
        FLUSH = 85552164,               // <align=flush>

        // Prefix and Unit suffix
        NONE = 2857034,
        PLUS = 43,
        MINUS = 45,
        PX = 2568,
        PLUS_PX = 49507,
        MINUS_PX = 47461,
        EM = 2216,
        PLUS_EM = 49091,
        MINUS_EM = 46789,
        PCT = 85031,
        PLUS_PCT = 1634348,
        MINUS_PCT = 1567082,
        PERCENTAGE = 37,
        PLUS_PERCENTAGE = 1454,
        MINUS_PERCENTAGE = 1512,

        TRUE = 2932022,
        FALSE = 85422813,

        INVALID = 1585415185,
        NOTDEF = 612146780,             // .notdef

        NORMAL = -1183493901,           // <style="Normal">
        DEFAULT = -620974005,           // <font="Default">
        REGULAR = 1291372090,
    }

    /// <summary>
    /// Defines the type of value used by a rich text tag or tag attribute.
    /// </summary>
    internal enum TagValueType
    {
        None = 0x0,
        NumericalValue = 0x1,
        StringValue = 0x2,
        ColorValue = 0x4,
    }

    internal enum TagUnitType
    {
        Pixels = 0x0,
        FontUnits = 0x1,
        Percentage = 0x2,
    }

    /// <summary>
    /// Commonly referenced Unicode characters in the text generation process.
    /// </summary>
    internal static class CodePoint
    {
        public const uint SPACE = 0x20;
        public const uint DOUBLE_QUOTE = 0x22;
        public const uint NUMBER_SIGN = 0x23;
        public const uint PERCENTAGE = 0x25;
        public const uint PLUS = 0x2B;
        public const uint MINUS = 0x2D;
        public const uint PERIOD = 0x2E;

        public const uint HYPHEN_MINUS = 0x2D;
        public const uint SOFT_HYPHEN = 0xAD;
        public const uint HYPHEN = 0x2010;
        public const uint NON_BREAKING_HYPHEN = 0x2011;
        public const uint ZERO_WIDTH_SPACE = 0x200B;
        public const uint RIGHT_SINGLE_QUOTATION = 0x2019;
        public const uint APOSTROPHE = 0x27;
        public const uint WORD_JOINER = 0x2060;
        public const uint HIGH_SURROGATE_START = 0xD800;
        public const uint HIGH_SURROGATE_END = 0xDBFF;
        public const uint LOW_SURROGATE_START = 0xDC00;
        public const uint LOW_SURROGATE_END = 0xDFFF;
        public const uint UNICODE_PLANE01_START = 0x10000;
        public const uint UNICODE_PLANE16_END = 0x10FFFF;
        public const uint LOWEST_10BITS_MASK = 0x3FF;
    }
}
