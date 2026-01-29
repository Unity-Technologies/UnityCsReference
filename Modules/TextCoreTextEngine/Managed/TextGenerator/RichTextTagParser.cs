// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

#nullable enable

namespace UnityEngine.TextCore
{

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class RichTextTagParser
    {
        internal static readonly Color32 k_HighlightColor = new Color32(255, 255, 0, 64);
        internal static readonly char k_PrivateArea = '\uE000';
		internal static Color s_AtgHyperlinkColor = new Color(0x4C / 255f, 0x7E / 255f, 0xFF / 255f, 1f);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static readonly Dictionary<string, System.IntPtr> s_FontAssetCache = new();
        internal static readonly Dictionary<string, WeakReference<SpriteAsset>> s_SpriteAssetCache = new();
        internal static readonly Dictionary<string, System.IntPtr> s_GradientAssetCache = new();

        public enum TagType
        {
            Hyperlink,
            Align,
            AllCaps,
            Alpha,
            Bold,
            Br,
            Color,
            CSpace,
            Font,
            FontWeight,
            Gradient,
            Italic,
            Indent,
            LineHeight,
            LineIndent,
            Link,
            Lowercase,
            Margin,
            MarginLeft,
            MarginRight,
            Mark,
            Mspace,
            NoBr,
            NoParse,
            Strikethrough,
            Size,
            SmallCaps,
            Space,
            Sprite,
            Style,
            Subscript,
            Superscript,
            Underline,
            Uppercase,
            Unknown // Not a real tag, used to indicate an error

            //gradient: pos, rotate , width, voffset will not be supported
        }

        public enum ValueID
        {
            Color,
            Padding,
            AssetID,
            GlyphMetrics,
            Scale,
            Tint,
            SpriteColor,
            Gradient
        }

        internal record TagTypeInfo
        {
            internal TagTypeInfo(TagType tagType, string name, TagValueType valueType = TagValueType.None, TagUnitType unitType = TagUnitType.Unknown)
            {
                TagType = tagType;
                this.name = name;
                this.valueType = valueType;
                this.unitType = unitType;
            }

            public TagType TagType;
            public string name;
            public TagValueType valueType;
            public TagUnitType unitType;
        }

        internal readonly static TagTypeInfo[] TagsInfo =
            {
            new TagTypeInfo(TagType.Hyperlink, "a"),
            new TagTypeInfo(TagType.Align,"align"), //"left", "center", "right", "justified", "flush"
            new TagTypeInfo(TagType.AllCaps,  "allcaps"), //none
            new TagTypeInfo(TagType.Alpha, "alpha"), //<alpha=#FF>FF <alpha=#CC>CC <alpha=#AA>AA <alpha=#88>88 <alpha=#66>66 <alpha=#44>44 <alpha=#22>22 <alpha=#00>00
            new TagTypeInfo(TagType.Bold,"b" ),
            new TagTypeInfo(TagType.Br,"br"),
            new TagTypeInfo(TagType.Color,"color",TagValueType.ColorValue), //<color="red">Red <color=#005500>Dark Green <#0000FF>Blue <color=#FF000088>Semitransparent Red
            new TagTypeInfo(TagType.CSpace,"cspace" ), //<cspace=1em>Spacing</cspace> is just as important as <cspace=-0.5em>timing.
            new TagTypeInfo(TagType.Font,"font"), //<font="Impact SDF">a different font?</font> or just <font="NotoSans" material="NotoSans Outline">a different material?
            new TagTypeInfo(TagType.FontWeight,"font-weight"), //<font-weight="100">Thin</font-weight>
            new TagTypeInfo(TagType.Gradient,"gradient"),
            new TagTypeInfo(TagType.Italic,"i"),
            new TagTypeInfo(TagType.Indent,"indent"), //<indent=15%> pixels, font units, or percentages.
            new TagTypeInfo(TagType.LineHeight,"line-height"), //pixels, font units, or percentages. <line-height=100%>Rather cozy.
            new TagTypeInfo(TagType.LineIndent,"line-indent" ), //pixels, font units, or percentages.
            new TagTypeInfo(TagType.Link, "link"), //<link="ID">my link</link>
            new TagTypeInfo(TagType.Lowercase,"lowercase"),//none
            new TagTypeInfo(TagType.Margin,"margin"),//pixels, font units, or percentages. Only positive. Does both left and right.
            new TagTypeInfo(TagType.MarginLeft,"margin-left"),
            new TagTypeInfo(TagType.MarginRight,"margin-right"),
            new TagTypeInfo(TagType.Mark,"mark" ), //<mark=#ffff00aa>
            new TagTypeInfo(TagType.Mspace,"mspace" ), // monospace : pixels or font units.
            new TagTypeInfo(TagType.NoBr,"nobr"), // none
            new TagTypeInfo(TagType.NoParse,"noparse"), // none
            new TagTypeInfo(TagType.Strikethrough,"s"), //striketrhough
            new TagTypeInfo(TagType.Size,"size"), // <size=20%>  // pixels, font units, or percentage. Pixel adjustments can be absolute (5px, 10px, and so on) or relative (+1 or -1, for example). Relative sizes are based on the original font size, so they're not cumulative.
            new TagTypeInfo(TagType.SmallCaps,"smallcaps" ),//none
            new TagTypeInfo(TagType.Space,"space" ), //pixels or font units.
            new TagTypeInfo(TagType.Sprite,"sprite"),
            // <sprite index=1>  <sprite name="spriteName">  <sprite=1>, <sprite="assetName" index=1> or by name <sprite="assetName" name="spriteName">.
            //tint=1 attribute to the tag tints the sprite with the TextMesh Pro object's Vertex Color. You can choose a different color by adding a color attribute to the tag (color=#FFFFFF).
            new TagTypeInfo ( TagType.Style,"style"), //<style="Title">Styles</style>
            new TagTypeInfo ( TagType.Subscript,"sub" ),
            new TagTypeInfo ( TagType.Superscript,"sup" ),
            new TagTypeInfo ( TagType.Underline,"u"),
            new TagTypeInfo ( TagType.Uppercase,"uppercase"),//none
            // page

            };

        internal enum TagValueType
        {
            None = 0,
            NumericalValue = 1,
            StringValue = 2,
            ColorValue = 3,
            Vector4Value = 4,
            GlyphMetricsValue = 5,
            BoolValue = 6
        }

        internal enum TagUnitType
        {
            Unknown = 0,
            Pixels = 1,
            FontUnits = 2,
            Percentage = 3
        }

        //TODO : change this for an union when development is over to save memory
        // we possibly could remove the type check on getter unless in debug mode
        //[StructLayout(LayoutKind.Explicit)]
        internal record TagValue
        {
            internal TagValue(float value, TagUnitType tagUnitType = TagUnitType.Unknown, ValueID? id = null)
            {
                type = TagValueType.NumericalValue;
                unit = tagUnitType;
                m_numericalValue = value;
                m_ID = id;
            }

            internal TagValue(Color value, ValueID? id = null)
            {
                type = TagValueType.ColorValue;
                m_colorValue = value;
                m_ID = id;
            }

            internal TagValue(string value, ValueID? id = null)
            {
                type = TagValueType.StringValue;
                m_stringValue = value;
                m_ID = id;
            }

            internal TagValue(Vector4 value, ValueID? id = null)
            {
                type = TagValueType.Vector4Value;
                m_vector4Value = value;
                m_ID = id;
            }

            internal TagValue(GlyphMetrics value, ValueID? id = null)
            {
                type = TagValueType.GlyphMetricsValue;
                m_glyphMetricsValue = value;
                m_ID = id;
            }

            internal TagValue(bool value, ValueID? id = null)
            {
                type = TagValueType.BoolValue;
                m_boolValue = value;
                m_ID = id;
            }

            //[FieldOffset(0)]
            internal TagValueType type;

            //[FieldOffset(4)]
            internal TagUnitType unit;

            //[FieldOffset(8)]
            private string? m_stringValue;

            //[FieldOffset(8)]
            private float m_numericalValue;

            //[FieldOffset(8)]
            private Color m_colorValue;

            private Vector4 m_vector4Value;

            private GlyphMetrics m_glyphMetricsValue;

            private bool m_boolValue;

            private ValueID? m_ID;


            internal string? StringValue
            {
                get
                {
                    if (type != TagValueType.StringValue)
                        throw new InvalidOperationException("Not a string value");
                    return m_stringValue;
                }
            }

            internal float NumericalValue
            {
                get
                {
                    if (type != TagValueType.NumericalValue)
                        throw new InvalidOperationException("Not a numerical value");
                    return m_numericalValue;
                }
            }

            internal Color ColorValue
            {
                get
                {
                    if (type != TagValueType.ColorValue)
                        throw new InvalidOperationException("Not a color value");
                    return m_colorValue;
                }
            }

            internal Vector4 Vector4Value
            {
                get
                {
                    if (type != TagValueType.Vector4Value)
                        throw new InvalidOperationException("Not a vector4 value");
                    return m_vector4Value;
                }
            }

            internal GlyphMetrics GlyphMetricsValue
            {
                get
                {
                    if (type != TagValueType.GlyphMetricsValue)
                        throw new InvalidOperationException("Not a GlyphMetrics value");
                    return m_glyphMetricsValue;
                }
            }

            internal bool BoolValue
            {
                get
                {
                    if (type != TagValueType.BoolValue)
                        throw new InvalidOperationException("Not a Bool value");
                    return m_boolValue;
                }
            }

            internal ValueID? ID
            {
                get
                {
                    return m_ID;
                }
            }
        }


        internal struct Tag
        {
            public TagType tagType;
            public bool isClosing;
            public int start; //position of the '<' character
            public int end; //position of the '>' character
            public TagValue? value; //could be replaced by a nullable struct?
            public TagValue? value2;
            public TagValue? value3;
            public TagValue? value4;
            public TagValue? value5;
        }

        public struct Segment
        {
            public List<Tag>? tags;
            public int start;
            public int end;
        }

        internal record ParseError
        {
            internal ParseError(string message, int position)
            {
                this.message = message;
                this.position = position;
            }
            public readonly int position;
            public readonly string message;
        }

        static bool tagMatch(ReadOnlySpan<char> tagCandidate, string tagName)
        {
            return tagCandidate.StartsWith(tagName.AsSpan()) && (tagCandidate.Length == tagName.Length || (!char.IsLetter(tagCandidate[tagName.Length]) && tagCandidate[tagName.Length] != '-'));
        }

        //Return true if there is a match
        static bool SpanToEnum(ReadOnlySpan<char> tagCandidate, out TagType tagType, out string? error, out ReadOnlySpan<char> attribute)
        {
            for (int i = 0; i < TagsInfo.Length; i++)
            {
                string tagName = TagsInfo[i].name;
                if (tagMatch(tagCandidate, tagName))
                {
                    tagType = TagsInfo[i].TagType;
                    error = null;
                    attribute = tagCandidate.Slice(tagName.Length);//Support only one attribute for now
                    return true;
                }
            }

            //Special case for color where there is no tag, just the attribute.
            if (tagCandidate.Length > 4 && tagCandidate[0] == '#')
            {
                tagType = TagType.Color;
                error = null;
                attribute = tagCandidate;
                return true;
            }

            error = "Unknown tag: " + tagCandidate.ToString();
            tagType = TagType.Unknown;
            attribute = null;
            return false;
        }

        static TagValue? ParseColorAttribute(ReadOnlySpan<char> attributeSection)
        {
            attributeSection = GetAttributeSpan(attributeSection);

            if (ColorUtility.TryParseHtmlString(attributeSection, out Color color))
                return new TagValue(color, ValueID.Color);

            return null;
        }

        static TagValue? ParsePaddingAttribute(ReadOnlySpan<char> value)
        {
            Span<int> paddings = stackalloc int[4];
            int index = 0;

            while (!value.IsEmpty && index < 4)
            {
                int commaIndex = value.IndexOf(',');

                ReadOnlySpan<char> num;
                if (commaIndex >= 0)
                {
                    num = value.Slice(0, commaIndex);
                    value = value.Slice(commaIndex + 1);
                }
                else
                {
                    num = value;
                    value = ReadOnlySpan<char>.Empty;
                }

                if (!int.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out paddings[index]))
                    return null;

                index++;
            }

            if (index != 4)
                return null;

            return new TagValue(new Vector4(paddings[0], paddings[1], paddings[2], paddings[3]), ValueID.Padding);
        }

        static TagValue? ParseHref(ReadOnlySpan<char> attributeSection)
        {
            if (TryGetSimpleHref(attributeSection, out string hrefValue))
            {
                return new TagValue(hrefValue);
            }
            else
            {
                // It's a complex link with multiple attributes. Store the entire string
                var attributes = attributeSection.TrimStart();
                return new TagValue(attributes.ToString());
            }
        }

        static bool TryGetSimpleHref(ReadOnlySpan<char> attributeSection, out string hrefValue)
        {
            hrefValue = "";
            attributeSection = attributeSection.Trim();

            if (!attributeSection.StartsWith("href=".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return false;

            // We can't simply return true here because <a href=... data=...> is also valid and not a simple href.

            var valueSection = attributeSection.Slice("href=".Length);

            char quote = valueSection.Length > 0 ? valueSection[0] : '\0';
            if (quote == '"' || quote == '\'')
            {
                var valueWithoutQuotes = valueSection.Slice(1);
                int endQuoteIndex = valueWithoutQuotes.IndexOf(quote);
                if (endQuoteIndex == -1) return false;

                // Check if there is any other text after the closing quote
                if (valueWithoutQuotes.Slice(endQuoteIndex + 1).Trim().Length > 0)
                    return false;

                hrefValue = valueWithoutQuotes.Slice(0, endQuoteIndex).ToString();
            }
            else
            {
                // If there's a space, it means there are other attributes.
                if (valueSection.Contains(new ReadOnlySpan<char>(new char[] { ' ' }), StringComparison.OrdinalIgnoreCase))
                    return false;

                hrefValue = valueSection.ToString();
            }

            return true;
        }

        private static bool ParseSpriteAttributes(ReadOnlySpan<char> attributeSection, TextSettings textSettings, out char unicode, out TagValue? spriteAssetValue, out TagValue? glyphMetricsValue, out TagValue? tintValue, out TagValue? scaleValue, out TagValue? colorValue, out string? spriteAssetNameOut)
        {
            int spriteIndex = -1;
            unicode = default;
            spriteAssetValue = null;
            glyphMetricsValue = null;
            tintValue = null;
            scaleValue = null;
            colorValue = null;
            spriteAssetNameOut = null;
            ReadOnlySpan<char> spriteAssetName = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> spriteName = ReadOnlySpan<char>.Empty;
            SpriteAsset? spriteAsset = null;

            while (!attributeSection.IsEmpty)
            {
                attributeSection = attributeSection.TrimStart();
                if (attributeSection.IsEmpty) break;

                ReadOnlySpan<char> key;
                ReadOnlySpan<char> val;

                int eqIndex = attributeSection.IndexOf('=');
                if (eqIndex == -1) break; // Malformed

                key = attributeSection.Slice(0, eqIndex).Trim();
                var valueAndRest = attributeSection.Slice(eqIndex + 1).TrimStart();

                char quote = valueAndRest.Length > 0 ? valueAndRest[0] : '\0';
                if (quote == '"' || quote == '\'')
                {
                    var valueWithoutQuotes = valueAndRest.Slice(1);
                    int endQuoteIndex = valueWithoutQuotes.IndexOf(quote);
                    if (endQuoteIndex == -1) break; // Malformed

                    val = valueWithoutQuotes.Slice(0, endQuoteIndex);
                    attributeSection = valueWithoutQuotes.Slice(endQuoteIndex + 1);
                }
                else
                {
                    int spaceIndex = valueAndRest.IndexOf(' ');
                    if (spaceIndex == -1)
                    {
                        val = valueAndRest;
                        attributeSection = ReadOnlySpan<char>.Empty;
                    }
                    else
                    {
                        val = valueAndRest.Slice(0, spaceIndex);
                        attributeSection = valueAndRest.Slice(spaceIndex);
                    }
                }

                if (key.IsEmpty) // This is the shorthand case, e.g., <sprite=1> or <sprite="asset Name">
                {
                    if (int.TryParse(val, out int index))
                    {
                        spriteIndex = index;
                    }
                    else
                    {
                        spriteAssetName = val;
                    }
                }
                else if (key.SequenceEqual("name"))
                {
                    spriteName = val;
                }
                else if (key.SequenceEqual("index"))
                {
                    if (int.TryParse(val, out int index))
                    {
                        spriteIndex = index;
                    }
                }
                else if (key.SequenceEqual("tint"))
                {
                    if (int.TryParse(val, out int tint) && tint == 1)
                    {
                        tintValue = new TagValue(true, ValueID.Tint);
                    }
                }
                else if (key.SequenceEqual("color"))
                {
                    val = GetAttributeSpan(val);

                    if (ColorUtility.TryParseHtmlString(val, out Color color))
                        colorValue = new TagValue(color, ValueID.SpriteColor);
                }
            }

            // We specified the SpriteAsset
            if (!spriteAssetName.IsEmpty)
            {
                spriteAssetNameOut = spriteAssetName.ToString();

                // Check cache for preloaded sprite asset
                if (!s_SpriteAssetCache.TryGetValue(spriteAssetNameOut, out var weakRef) ||
                    !weakRef.TryGetTarget(out spriteAsset))
                {
                    // Asset not preloaded or was GC'd, return false but keep the asset name for HasSpriteTags extraction
                    return false;
                }
            }
            // We use the default Sprite Asset
            else
            {
                // No Sprite Asset is assigned to the text object
                if (textSettings.defaultSpriteAsset != null)
                {
                    spriteAsset = textSettings.defaultSpriteAsset;
                }
                else if (TextSettings.s_GlobalSpriteAsset != null)
                {
                    spriteAsset = TextSettings.s_GlobalSpriteAsset;
                }

                // No valid sprite asset available
                if (spriteAsset == null)
                    return false;
            }

            if (!spriteName.IsEmpty)
            {
                // TODO optimize string allocation
                spriteIndex = spriteAsset.GetSpriteIndexFromName(spriteName.ToString());
            }
            if (spriteIndex == -1)
                return false;

            if (spriteAsset.spriteCharacterTable.Count <= spriteIndex)
                return false;

            var sprite = spriteAsset.spriteCharacterTable[spriteIndex];

            spriteAssetValue = new TagValue(spriteAsset.instanceID, TagUnitType.Unknown, ValueID.AssetID);
            glyphMetricsValue = new TagValue(sprite.glyph.metrics, ValueID.GlyphMetrics);
            scaleValue = new TagValue(sprite.scale, TagUnitType.Unknown, ValueID.Scale);
            // Sprites are assigned in the E000 Private Area + sprite Index
            unicode = (char)(k_PrivateArea + spriteIndex);

            return true;
        }

        public static int GetHashCode(ReadOnlySpan<char> span)
        {
            var hash = new HashCode();
            foreach (char c in span)
            {
                hash.Add(c);
            }
            return hash.ToHashCode();
        }


        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void PreloadFontAssetsFromTags(string text, TextSettings textSettings)
        {
            if (!HasFontTags(text, textSettings, out var fontAssetNames))
                return;

            foreach (var fontAssetName in fontAssetNames)
            {
                // Skip if already cached
                if (s_FontAssetCache.ContainsKey(fontAssetName))
                    continue;

                var fontAsset = Resources.Load<FontAsset>(textSettings.defaultFontAssetPath + fontAssetName);
                if (fontAsset == null)
                    continue;

                fontAsset.EnsureNativeFontAssetIsCreated();
                s_FontAssetCache[fontAssetName] = fontAsset.nativeFontAsset;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void PreloadSpriteAssetsFromTags(string text, TextSettings textSettings)
        {
            if (!HasSpriteTags(text, textSettings, out var spriteAssetNames))
                return;

            foreach (var spriteAssetName in spriteAssetNames)
            {
                // Skip if already cached
                if (s_SpriteAssetCache.ContainsKey(spriteAssetName))
                    continue;

                var spriteAsset = Resources.Load<SpriteAsset>(textSettings.defaultSpriteAssetPath + spriteAssetName);
                if (spriteAsset == null)
                    continue;

                spriteAsset.UpdateLookupTables();
                s_SpriteAssetCache[spriteAssetName] = new WeakReference<SpriteAsset>(spriteAsset);
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void PreloadGradientAssetsFromTags(string text, TextSettings textSettings)
        {
            if (!HasGradientTags(text, textSettings, out var gradientAssetNames))
                return;

            foreach (var gradientAssetName in gradientAssetNames)
            {
                // Skip if already cached
                if (s_GradientAssetCache.ContainsKey(gradientAssetName))
                    continue;

                var gradientAsset = Resources.Load<TextColorGradient>(textSettings.defaultColorGradientPresetsPath + gradientAssetName);
                if (gradientAsset == null)
                    continue;

                gradientAsset.MarkNativeDirty();
                s_GradientAssetCache[gradientAssetName] = gradientAsset.nativeInstance;
            }
        }

        internal static List<Tag> FindTags(ref string inputStr, TextSettings textSettings, bool preprocessingOnly = false, List<ParseError>? errors = null)
        {
            var input = inputStr.ToCharArray();
            var result = new List<Tag>();
            int pos = 0;

            while (true)
            {
                var start = Array.IndexOf(input, '<', pos);
                if (start == -1) // no tag
                    break;

                var end = Array.IndexOf(input, '>', start);
                if (end == -1)
                    break;

                bool isClosing = (input.Length > start + 1 && input[start + 1] == '/');

                if (end == start + 1)
                {
                    errors?.Add(new("Empty tag", start));
                    pos = end + 1;
                    continue;
                }

                pos = end + 1;

                if (!isClosing)
                {
                    var span = input.AsSpan(start + 1, end - start - 1);
                    if (SpanToEnum(span, out TagType tagType, out string? error, out var attributeSection))
                    {
                        // TODO Manual parsing of color need to be moved elsewhere
                        TagValue? value = null;
                        TagValue? value2 = null;

                        if (tagType == TagType.Color)
                        {
                            value = ParseColorAttribute(attributeSection);

                            if (value is null)
                            {
                                errors?.Add(new("Invalid color value", start));
                                pos = start + 1; //malformed tag, skip the '<' character
                                continue;
                            }
                        }

                        if (tagType == TagType.Mark)
                        {
                            // try the simple mark=myColor
                            value = ParseColorAttribute(attributeSection);

                            if (value == null)
                            {
                                while (!attributeSection.IsEmpty)
                                {
                                    int spaceIndex = attributeSection.IndexOf(' ');
                                    ReadOnlySpan<char> pair;

                                    if (spaceIndex >= 0)
                                    {
                                        pair = attributeSection.Slice(0, spaceIndex);

                                        if (spaceIndex + 1 < attributeSection.Length)
                                            attributeSection = attributeSection.Slice(spaceIndex + 1);
                                        else
                                            attributeSection = ReadOnlySpan<char>.Empty;
                                    }
                                    else
                                    {
                                        pair = attributeSection;
                                        attributeSection = ReadOnlySpan<char>.Empty;
                                    }

                                    int eqIndex = pair.IndexOf('=');
                                    if (eqIndex <= 0 || eqIndex >= pair.Length - 1)
                                        continue; // malformed

                                    var key = pair.Slice(0, eqIndex);
                                    var val = pair.Slice(eqIndex + 1);

                                    if (key.SequenceEqual("color"))
                                    {
                                        value = ParseColorAttribute(val);
                                    }
                                    else if (key.SequenceEqual("padding"))
                                    {
                                        value2 = ParsePaddingAttribute(val);
                                    }
                                }
                            }
                        }

                        if (tagType == TagType.Hyperlink)
                        {
                            value = ParseHref(attributeSection);
                        }
                        if (tagType == TagType.Link)
                        {
                            attributeSection = GetAttributeSpan(attributeSection);
                            var str = attributeSection.ToString();

                            value = new TagValue(str);
                        }

                        if (tagType == TagType.Sprite)
                        {
                            bool success = ParseSpriteAttributes(attributeSection, textSettings, out char unicode, out value, out value2, out TagValue? value3, out TagValue? value4, out TagValue? value5, out string? spriteAssetName);

                            if (!success)
                            {
                                // Only add incomplete tag during preprocessing (for HasSpriteTags extraction)
                                // During rendering, skip the tag so it remains visible in output text
                                if (preprocessingOnly && spriteAssetName != null)
                                    result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = false, value = new TagValue(spriteAssetName) });

                                continue;
                            }

                            result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = false, value = value, value2 = value2, value3 = value3, value4 = value4, value5 = value5 });
                            // TODO: This is really inefficient, we should do this at the end of parsing instead, which isn't straightforward.
                            inputStr = inputStr.Insert(end + 1, unicode + "/");
                            input = inputStr.ToCharArray();
                            result.Add(new Tag { tagType = tagType, start = end + 2, end = end + 2, isClosing = true, value = value, value2 = value2, value3 = value3, value4 = value4, value5 = value5 });
                            // Adjust position to continue after the newly inserted text
                            pos = end + 2;
                            continue;
                        }

                        if (tagType == TagType.Br)
                        {
                            if (!attributeSection.IsEmpty)
                                continue;
                            // TODO: This is really inefficient, we should do this at the end of parsing instead, which isn't straightforward.
                            result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = false, value = null });
                            inputStr = inputStr.Insert(end + 1, "\n/");
                            input = inputStr.ToCharArray();
                            result.Add(new Tag { tagType = tagType, start = end + 2, end = end + 2, isClosing = true, value = null });
                            // Adjust position to continue after the newly inserted text
                            pos = end + 2;
                            continue;
                        }

                        if (tagType == TagType.Align)
                        {
                            attributeSection = GetAttributeSpan(attributeSection);
                            var str = attributeSection.ToString();

                            if (Enum.TryParse<HorizontalAlignment>(str, true, out _))
                            {
                                value = new TagValue(str);
                            }

                            if (value is null)
                            {
                                errors?.Add(new($"Invalid {tagType} value", start));
                                pos = start + 1; //malformed tag, skip the '<' character
                                continue;
                            }
                        }

                        if (tagType == TagType.Mspace || tagType == TagType.CSpace)
                        {
                            var tagUnitType = ParseTagUnitType(ref attributeSection);

                            if (tagUnitType == TagUnitType.Percentage)
                            {
                                errors?.Add(new($"Invalid {tagUnitType} value", start));
                                pos = start + 1; //malformed tag, skip the '<' character
                                continue;
                            }

                            // Not specifying a unit is same as using px.
                            if (tagUnitType == TagUnitType.Unknown)
                                tagUnitType = TagUnitType.Pixels;

                            attributeSection = GetAttributeSpan(attributeSection);
                            float parsedValue;
                            if (!float.TryParse(attributeSection, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
                            {
                                // Handle parse error, e.g. skip or log
                                errors?.Add(new("Invalid numerical value", start));
                                pos = start + 1;
                                continue;
                            }
                            value = new TagValue(parsedValue, tagUnitType);
                        }

                        if (tagType == TagType.Margin || tagType == TagType.MarginLeft || tagType == TagType.MarginRight)
                        {
                            var tagUnitType = ParseTagUnitType(ref attributeSection);

                            if (tagUnitType == TagUnitType.Unknown)
                                tagUnitType = TagUnitType.Pixels;

                            attributeSection = GetAttributeSpan(attributeSection);
                            float parsedValue;
                            if (!float.TryParse(attributeSection, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
                            {
                                // Handle parse error, e.g. skip or log
                                errors?.Add(new("Invalid numerical value", start));
                                pos = start + 1;
                                continue;
                            }
                            value = new TagValue(parsedValue, tagUnitType);
                        }

                        if (tagType == TagType.Font)
                        {
                            attributeSection = GetAttributeSpan(attributeSection);
                            var fontAssetName = attributeSection.ToString();

                            if (string.IsNullOrEmpty(fontAssetName))
                            {
                                errors?.Add(new("Font name cannot be empty", start));
                                pos = start + 1;
                                continue;
                            }

                            // Check if font asset is preloaded in cache
                            bool fontIsPreloaded = s_FontAssetCache.ContainsKey(fontAssetName);

                            if (!fontIsPreloaded)
                            {
                                // Only add incomplete tag during preprocessing (for HasFontTags extraction)
                                // During rendering, skip the tag so it remains visible in output text
                                if (preprocessingOnly)
                                    result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = false, value = new TagValue(fontAssetName) });

                                pos = start + 1;
                                continue;
                            }

                            value = new TagValue(fontAssetName);
                        }

                        if (tagType == TagType.Size)
                        {
                            var tagUnitType = ParseTagUnitType(ref attributeSection);

                            if (tagUnitType == TagUnitType.Unknown)
                                tagUnitType = TagUnitType.Pixels;

                            attributeSection = GetAttributeSpan(attributeSection);

                            // Check for relative sizing indicators (+ or - prefix)
                            bool isRelative = false;
                            if (attributeSection.Length > 0 && (attributeSection[0] == '+' || attributeSection[0] == '-'))
                            {
                                isRelative = true;
                            }

                            float parsedValue;
                            if (!float.TryParse(attributeSection, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
                            {
                                errors?.Add(new("Invalid size value", start));
                                pos = start + 1;
                                continue;
                            }

                            value = new TagValue(parsedValue, tagUnitType);
                            value2 = new TagValue(isRelative); // Store relative flag in value2
                        }

                        if (tagType == TagType.FontWeight)
                        {
                            attributeSection = GetAttributeSpan(attributeSection);
                            if (int.TryParse(attributeSection, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weightValue))
                            {
                                if (Enum.IsDefined(typeof(TextFontWeight), weightValue))
                                {
                                    value = new TagValue(weightValue);
                                }
                                else
                                {
                                    errors?.Add(new($"Invalid font-weight value: {weightValue}", start));
                                    pos = start + 1;
                                    continue;
                                }
                            }
                            else
                            {
                                errors?.Add(new("Invalid font-weight value", start));
                                pos = start + 1;
                                continue;
                            }
                        }

                        if (tagType == TagType.Gradient)
                        {
                            attributeSection = GetAttributeSpan(attributeSection);
                            var gradientAssetName = attributeSection.ToString();

                            if (string.IsNullOrEmpty(gradientAssetName))
                            {
                                errors?.Add(new("Gradient name cannot be empty", start));
                                pos = start + 1;
                                continue;
                            }

                            // Check if gradient asset is preloaded in cache
                            bool gradientIsPreloaded = s_GradientAssetCache.ContainsKey(gradientAssetName);

                            if (!gradientIsPreloaded)
                            {
                                // Only add incomplete tag during preprocessing (for HasGradientTags extraction)
                                // During rendering, skip the tag so it remains visible in output text
                                if (preprocessingOnly)
                                    result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = false, value = new TagValue(gradientAssetName) });

                                pos = start + 1;
                                continue;
                            }

                            value = new TagValue(gradientAssetName, ValueID.Gradient);
                        }

                        result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = isClosing, value = value, value2 = value2 });

                        if (tagType == TagType.NoParse)
                        {
                            //Not uisng the real loop to skip all "malformed tags" errors.
                            if ((start = input.AsSpan(pos).IndexOf("</noparse>")) == -1)
                            {
                                break; // no closing noparse tag, no need to cleanup
                            }
                            start += pos; //The start index was relative to the span, we need to make it relative to the input
                            end = start + "</noparse>".Length - 1;
                            result.Add(new Tag { tagType = TagType.NoParse, start = start, end = end, isClosing = true });
                            pos = end + 1;
                        }
                    }
                    else
                    {
                        if (error is not null)
                            errors?.Add(new(error, start));

                        pos = start + 1; //malformed tag, skip the '<' character
                    }
                }
                else
                {
                    if (SpanToEnum(input.AsSpan(start + 2, end - start - 2), out TagType tagType, out string? error, out var _))
                    {
                        result.Add(new Tag { tagType = tagType, start = start, end = end, isClosing = isClosing });
                    }
                    else
                    {
                        if (error is not null)
                            errors?.Add(new(error, start));

                        pos = start + 1; //malformed tag, skip the '<' character
                    }
                }


            }

            return result;
        }

        private static ReadOnlySpan<char> GetAttributeSpan(ReadOnlySpan<char> attributeSection)
        {
            if (attributeSection.Length >= 1 && attributeSection[0] == '=')
                attributeSection = attributeSection.Slice(1);

            // Handle quoted values
            if (attributeSection.Length >= 2 &&
                ((attributeSection[0] == '"' && attributeSection[^1] == '"') ||
                 (attributeSection[0] == '\'' && attributeSection[^1] == '\'')))
            {
                return attributeSection.Slice(1, attributeSection.Length - 2);
            }
            else
            {
                // Unquoted value
                return attributeSection;
            }
        }

        private static TagUnitType ParseTagUnitType(ref ReadOnlySpan<char> attributeSection)
        {

            if (attributeSection.EndsWith("em".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                attributeSection = attributeSection.Slice(0, attributeSection.Length - 2);
                return TagUnitType.FontUnits;
            }
            else if (attributeSection.EndsWith("px".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                attributeSection = attributeSection.Slice(0, attributeSection.Length - 2);
                return TagUnitType.Pixels;
            }
            else if (attributeSection.EndsWith("%".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                attributeSection = attributeSection.Slice(0, attributeSection.Length - 1);
                return TagUnitType.Percentage;
            }
            else
            {
                return TagUnitType.Unknown;
            }
        }


        // Return a list of tags that will be applied to the text
        // previousTags can be empty to skip the allocation of a new list

        internal static List<Tag> PickResultingTags(List<Tag> allTags, string input, int atPosition, List<Tag>? applicableTags = null)
        {
            if (applicableTags == null)
            {
                applicableTags = new List<Tag>();
            }
            else
            {
                applicableTags.Clear();
            }

            int startingPos = 0; //Assument the starting position is always 0 as we do not backup the stack infos..
            // TODO incremental procesing, will have to review methods parameter and structure...
            // TOOD clean the checks belos

            Debug.Assert(string.IsNullOrEmpty(input) || (atPosition < input.Length && atPosition >= 0), "Invalid position");
            Debug.Assert(startingPos <= atPosition && startingPos >= 0, "Invalid starting position");

            int previousTagPosition = 0;
            foreach (var tag in allTags)
            {
                Debug.Assert(tag.start >= previousTagPosition, "Tags are not sorted");
                previousTagPosition = tag.end + 1;
            }

            foreach (var tag in applicableTags)
            {
                Debug.Assert(tag.end <= startingPos, "Tag end pass the point where we should start parsing");
                Debug.Assert(allTags.Contains(tag));
            }
            Span<int?> parents = stackalloc int?[allTags.Count];
            Span<int?> lastTagOfType = stackalloc int?[TagsInfo.Length];

            int i = -1;
            foreach (var tag in allTags)
            {
                i++;
                if (tag.end < startingPos)
                {
                    continue;
                }

                if (tag.tagType == TagType.NoParse)
                {
                    continue;
                }

                if (tag.start > atPosition)
                {
                    break; // we are done.
                }


                if (tag.isClosing)
                {
                    if (lastTagOfType[(int)tag.tagType].HasValue)
                    {
                        if (parents[i].HasValue)
                        {
                            lastTagOfType[(int)tag.tagType] = parents[i];
                        }
                        else
                            lastTagOfType[(int)tag.tagType] = null;
                    }
                }
                else
                {
                    //New tag, set as last tag open and nest under parent if there was one already

                    var currentLastTagIndex = lastTagOfType[(int)tag.tagType];
                    if (currentLastTagIndex.HasValue)
                    {
                        parents[i] = currentLastTagIndex;
                    }

                    lastTagOfType[(int)tag.tagType] = i;

                }

            }

            //The order in the resulting list is important: we cannot iterate only adding lastTagOfType
            int currentTagIndex = 0;
            foreach (var tag in allTags)
            {
                var lastTag = lastTagOfType[(int)tag.tagType];
                if (lastTag.HasValue && currentTagIndex == lastTag.Value)
                    applicableTags.Add(tag);

                currentTagIndex++;
            }

            return applicableTags;
        }


        //Return a list of text setgment that will share the same text generation settings between two tags
        internal static Segment[] GenerateSegments(string input, List<Tag> tags)
        {
            var segments = new List<Segment>();
            int afterPreviousTagEnd = 0;
            for (int i = 0; i < tags.Count; i++)
            {
                Debug.Assert(tags[i].start >= afterPreviousTagEnd);
                //If the tag is consecutive, no segment is generated
                if (tags[i].start > afterPreviousTagEnd)
                {
                    segments.Add(new Segment { start = afterPreviousTagEnd, end = tags[i].start - 1 }); // tags[i].start-1 wont be negative because afterPreviousTagEnd start at 0 and we are greater
                }
                afterPreviousTagEnd = tags[i].end + 1;
            }

            //Check if there is a segment after the last tag
            if (afterPreviousTagEnd < input.Length)
            {
                segments.Add(new Segment { start = afterPreviousTagEnd, end = input.Length - 1 });
            }

            //This is ugly, need to be able to modify a reference in a loop later
            return segments.ToArray();
        }

        internal static void ApplyStateToSegment(string input, List<Tag> tags, Segment[] segments)
        {

            //tmp list = new List<Tag>();
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].tags = PickResultingTags(tags, input, segments[i].start);
                // TODO change to the non alloc version
                //segments[i].state.tags = PickResultingTags(tags, input, segments[i].start, i>0?segments[i-1].start :0, list).Copy? ;
                //
            }

        }

        static private int AddLink(TagType type, string value, List<(int, TagType, string)> links)
        {
            foreach (var (index, listType, listValue) in links)
            {
                if (type == listType && value == listValue)
                    return index;
            }

            int nextIndex = links.Count;
            links.Add((nextIndex, type, value));

            return nextIndex;
        }

        static TextSpan CreateTextSpan(Segment segment, ref NativeTextGenerationSettings tgs, List<(int, TagType, string)> links, Color hyperlinkColor, float pixelsPerPoint)
        {
            var textSpan = tgs.CreateTextSpan();

            if (segment.tags is null)
                return textSpan;

            for (int i = 0; i < segment.tags.Count; i++)
            {
                switch (segment.tags[i].tagType)
                {
                    //Font Style
                    case TagType.Bold:
                        textSpan.fontWeight = TextCore.Text.TextFontWeight.Bold;
                        break;
                    case TagType.Italic:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Italic;
                        break;
                    case TagType.Underline:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Underline;
                        break;
                    case TagType.Strikethrough:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Strikethrough;
                        break;
                    case TagType.Subscript:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Subscript;
                        break;
                    case TagType.Superscript:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Superscript;
                        break;
                    case TagType.AllCaps:
                    case TagType.Uppercase:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.UpperCase;
                        break;
                    case TagType.SmallCaps:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.SmallCaps;
                        break;
                    case TagType.Lowercase:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.LowerCase;
                        break;

                    //Color and appeareance
                    case TagType.Color:
                        textSpan.color = segment.tags[i].value!.ColorValue;
                        break;
                    case TagType.Alpha:
                        //TODO tgs.color.a = segment.state.tags[i].value.NumericalValue;
                        break;
                    case TagType.Mark:
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Highlight;

                        if (segment.tags[i].value?.ID == ValueID.Color)
                            textSpan.highlightColor = segment.tags[i].value!.ColorValue;
                        else
                            textSpan.highlightColor = k_HighlightColor;

                        if (segment.tags[i].value2?.ID == ValueID.Padding)
                            textSpan.highlightPadding = segment.tags[i].value2!.Vector4Value;

                        break;
                    case TagType.Style:
                        Debug.Assert(false, "Style tags should be handled by the preprocessor.");
                        break;
                    case TagType.Font:
                        string fontAssetName = segment.tags[i].value?.StringValue ?? "";
                        if (!string.IsNullOrEmpty(fontAssetName))
                        {
                            // Check static cache first
                            if (s_FontAssetCache.TryGetValue(fontAssetName, out var cachedNativeFontAsset))
                            {
                                textSpan.fontAsset = cachedNativeFontAsset;
                            }
                            // Otherwise, fail silently and use the default font asset
                        }
                        break;
                    case TagType.FontWeight:
                        if (segment.tags[i].value?.type == TagValueType.NumericalValue)
                        {
                            textSpan.fontWeight = (TextFontWeight)(int)segment.tags[i].value!.NumericalValue;
                        }
                        break;
                    case TagType.Hyperlink:
                        textSpan.linkID = AddLink(TagType.Hyperlink, segment.tags[i].value?.StringValue ?? "", links);
                        textSpan.color = hyperlinkColor;
                        textSpan.fontStyle |= TextCore.Text.FontStyles.Underline;
                        break;
                    case TagType.Link:
                        textSpan.linkID = AddLink(TagType.Link, segment.tags[i].value?.StringValue ?? "", links);
                        break;
                    case TagType.Gradient:
                        string gradientName = segment.tags[i].value?.StringValue ?? "";
                        if (!string.IsNullOrEmpty(gradientName))
                        {
                            // Check static cache first (Pattern matching Font implementation)
                            if (s_GradientAssetCache.TryGetValue(gradientName, out var cachedNativeGradientAsset))
                            {
                                textSpan.gradientAsset = cachedNativeGradientAsset;
                            }
                        }
                        break;

                    case TagType.Sprite:
                        if (segment.tags[i].value?.ID == ValueID.AssetID)
                            textSpan.spriteID = (int)segment.tags[i].value!.NumericalValue;
                        if (segment.tags[i].value2?.ID == ValueID.GlyphMetrics)
                            textSpan.spriteMetrics = segment.tags[i].value2!.GlyphMetricsValue;
                        if (segment.tags[i].value3?.ID == ValueID.Tint)
                            textSpan.spriteTint = segment.tags[i].value3!.BoolValue;
                        if (segment.tags[i].value4?.ID == ValueID.Scale)
                            textSpan.spriteScale = (int)segment.tags[i].value4!.NumericalValue;
                        if (segment.tags[i].value5?.ID == ValueID.SpriteColor)
                            textSpan.spriteColor = segment.tags[i].value5!.ColorValue;
                        else
                            textSpan.spriteColor = Color.white;
                        //TODO : Add support for sprite
                        break;

                    //Layout/Positioning
                    case TagType.Size:
                        float sizeValue = segment.tags[i].value!.NumericalValue;
                        TagUnitType sizeUnit = segment.tags[i].value!.unit;
                        bool isRelative = segment.tags[i].value2?.BoolValue ?? false;

                        if (isRelative)
                        {
                            // For relative sizing, compute final size by adding/subtracting from base fontSize
                            float baseFontSizeInUnits = tgs.fontSize / 64.0f;
                            float relativeSizeInUnits = sizeValue * pixelsPerPoint;
                            float finalSize = baseFontSizeInUnits + relativeSizeInUnits;
                            textSpan.fontSize = (int)Math.Round(finalSize * 64.0f, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            // For absolute sizing, handle unit conversion
                            if (sizeValue <= 0)
                            {
                                // Invalid absolute size - use 0 to indicate fallback to global fontSize
                                textSpan.fontSize = 0;
                            }
                            else
                            {
                                switch (sizeUnit)
                                {
                                    case TagUnitType.FontUnits: // em units
                                        {
                                            float baseFontSizeInUnits = tgs.fontSize / 64.0f;
                                            float finalSize = sizeValue * baseFontSizeInUnits;
                                            textSpan.fontSize = (int)Math.Round(finalSize * 64.0f, MidpointRounding.AwayFromZero);
                                            break;
                                        }
                                    case TagUnitType.Percentage:
                                        {
                                            float baseFontSizeInUnits = tgs.fontSize / 64.0f;
                                            float finalSize = (sizeValue / 100.0f) * baseFontSizeInUnits;
                                            textSpan.fontSize = (int)Math.Round(finalSize * 64.0f, MidpointRounding.AwayFromZero);
                                            break;
                                        }
                                    case TagUnitType.Pixels:
                                    default:
                                        textSpan.fontSize = (int)Math.Round((sizeValue) * pixelsPerPoint * 64.0f, MidpointRounding.AwayFromZero);
                                        break;
                                }
                            }
                        }
                        break;
                    case TagType.CSpace:
                        float cspaceMult = segment.tags[i].value!.unit == TagUnitType.Pixels ? (pixelsPerPoint * 64.0f) : 64.0f;
                        textSpan.cspace = (int)(segment.tags[i].value!.NumericalValue * cspaceMult);
                        textSpan.cspaceUnitType = segment.tags[i].value!.unit;
                        break;
                    case TagType.Br:
                        //TODO : Add support for br
                        break;
                    case TagType.Mspace:
                        float mspaceMult = segment.tags[i].value!.unit == TagUnitType.Pixels ? (pixelsPerPoint * 64.0f) : 64.0f;
                        textSpan.mspace = (int)(segment.tags[i].value!.NumericalValue * mspaceMult);
                        textSpan.mspaceUnitType = segment.tags[i].value!.unit;
                        break;
                    case TagType.LineIndent:
                        //TODO : Add support for lineindent
                        break;
                    case TagType.Space:
                        //TODO : Add support for space
                        break;
                    case TagType.NoBr:
                        //TODO : Add support for nobr
                        break;
                    case TagType.Align:
                        Enum.TryParse<HorizontalAlignment>(segment.tags[i].value!.StringValue, true, out textSpan.alignment);
                        break;
                    case TagType.LineHeight:
                        //TODO : Add support for lineheight
                        break;
                    case TagType.Margin:
                    case TagType.MarginLeft:
                    case TagType.MarginRight:
                        float mult = segment.tags[i].value!.unit == TagUnitType.Pixels ? (pixelsPerPoint * 64.0f) : 64.0f;
                        textSpan.margin = (int)(segment.tags[i].value!.NumericalValue * mult);
                        textSpan.marginUnitType = segment.tags[i].value!.unit;
                        textSpan.marginDirection = segment.tags[i].tagType switch
                        {
                            TagType.Margin => MarginDirection.Both,
                            TagType.MarginLeft => MarginDirection.Left,
                            TagType.MarginRight => MarginDirection.Right,
                            _ => MarginDirection.Both
                        };
                        break;


                    case TagType.NoParse://Noparse should not be reach here/Should be trimmed
                    case TagType.Unknown:
                        throw new InvalidOperationException("Invalid tag type" + segment.tags[i].tagType);

                }
            }


            return textSpan;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void CreateTextGenerationSettingsArray(ref NativeTextGenerationSettings tgs, List<(int, TagType, string)> links, Color hyperlinkColor, float pixelsPerPoint, TextSettings textSettings)
        {
            links.Clear();

            var tags = FindTags(ref tgs.text, textSettings);
            var segments = GenerateSegments(tgs.text, tags);
            ApplyStateToSegment(tgs.text, tags, segments);

            var parsedTextBuilder = new StringBuilder(tgs.text.Length);
            tgs.textSpans = new TextSpan[segments.Length];
            int parsedIndex = 0;

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                string segmentText = tgs.text.Substring(segment.start, segment.end + 1 - segment.start);

                var textSpan = CreateTextSpan(segment, ref tgs, links, hyperlinkColor, pixelsPerPoint);
                textSpan.startIndex = parsedIndex;
                textSpan.length = segmentText.Length;
                tgs.textSpans[i] = textSpan;
                parsedTextBuilder.Append(segmentText);
                parsedIndex += segmentText.Length;
            }

            tgs.text = parsedTextBuilder.ToString();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool MayNeedParsing(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            ReadOnlySpan<char> source = text.AsSpan();
            int openIndex = source.IndexOf('<');

            if (openIndex < 0 || openIndex >= source.Length - 1)
                return false;

            return source.Slice(openIndex + 1).IndexOf('>') >= 0;
        }

        const string k_FontTag = "<font=";
        static bool ContainsFontTag(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            ReadOnlySpan<char> source = text.AsSpan();
            ReadOnlySpan<char> tag = k_FontTag.AsSpan();

            int tagIndex = source.IndexOf(tag, StringComparison.Ordinal);

            if (tagIndex < 0)
                return false;

            int startIndex = tagIndex + tag.Length;
            for (int i = startIndex; i < source.Length; i++)
            {
                if (source[i] == '>')
                    return true;
            }

            return false;
        }

        const string k_SpriteTag = "<sprite";
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static bool ContainsSpriteTag(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            ReadOnlySpan<char> source = text.AsSpan();
            ReadOnlySpan<char> tag = k_SpriteTag.AsSpan();

            int tagIndex = source.IndexOf(tag, StringComparison.Ordinal);

            if (tagIndex < 0)
                return false;

            int startIndex = tagIndex + tag.Length;
            for (int i = startIndex; i < source.Length; i++)
            {
                if (source[i] == '>')
                    return true;
            }

            return false;
        }

		const string k_StyleTag = "<style=\"";
        internal static bool ContainsStyleTags(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            ReadOnlySpan<char> source = text.AsSpan();
            ReadOnlySpan<char> tag = k_StyleTag.AsSpan();

            int tagIndex = source.IndexOf(tag, StringComparison.Ordinal);

            if (tagIndex < 0)
                return false;

            int startIndex = tagIndex + tag.Length;
            for (int i = startIndex; i < source.Length; i++)
            {
                if (source[i] == '>')
                    return true;
            }

            return false;
        }


        const string k_GradientTag = "<gradient";
        internal static bool ContainsGradientTag(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            ReadOnlySpan<char> source = text.AsSpan();
            ReadOnlySpan<char> tag = k_GradientTag.AsSpan();

            int tagIndex = source.IndexOf(tag, StringComparison.Ordinal);

            if (tagIndex < 0)
                return false;

            int startIndex = tagIndex + tag.Length;
            for (int i = startIndex; i < source.Length; i++)
            {
                if (source[i] == '>')
                    return true;
            }

            return false;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal static bool HasFontTags(string text, TextSettings textSettings, out List<string> fontAssetNames)
        {
            fontAssetNames = new();

            if (!ContainsFontTag(text))
                return false;

            var tags = FindTags(ref text, textSettings, preprocessingOnly: true);

            foreach (var tag in tags)
            {
                if (tag.tagType == TagType.Font && !tag.isClosing && tag.value?.StringValue != null)
                {
                    string fontName = tag.value.StringValue;
                    if (!fontAssetNames.Contains(fontName))
                        fontAssetNames.Add(fontName);
                }
            }

            return fontAssetNames.Count > 0;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
        internal static bool HasSpriteTags(string text, TextSettings textSettings, out List<string> spriteAssetNames)
        {
            spriteAssetNames = new();

            if (!ContainsSpriteTag(text))
                return false;

            var tags = FindTags(ref text, textSettings, preprocessingOnly: true);

            foreach (var tag in tags)
            {
                if (tag.tagType == TagType.Sprite && !tag.isClosing)
                {
                    // Check if there's a sprite asset name specified
                    // The sprite asset name is stored as a StringValue when tag parsing fails (asset not cached)
                    // Successfully parsed sprites use EntityId values, not strings
                    if (tag.value?.type == TagValueType.StringValue)
                    {
                        string? spriteAssetName = tag.value.StringValue;
                        if (!string.IsNullOrEmpty(spriteAssetName) && !spriteAssetNames.Contains(spriteAssetName))
                            spriteAssetNames.Add(spriteAssetName);
                    }
                }
            }

            return spriteAssetNames.Count > 0;
        }

        internal static bool HasGradientTags(string text, TextSettings textSettings, out List<string> gradientAssetNames)
        {
            gradientAssetNames = new();

            if (!ContainsGradientTag(text))
                return false;

            var tags = FindTags(ref text, textSettings, preprocessingOnly: true);

            foreach (var tag in tags)
            {
                if (tag.tagType == TagType.Gradient && !tag.isClosing && tag.value?.type == TagValueType.StringValue)
                {
                    string? gradientAssetName = tag.value.StringValue;
                    if (!string.IsNullOrEmpty(gradientAssetName) && !gradientAssetNames.Contains(gradientAssetName))
                        gradientAssetNames.Add(gradientAssetName);
                }
            }

            return gradientAssetNames.Count > 0;
        }
    }
}

