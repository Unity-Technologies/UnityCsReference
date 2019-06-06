// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static class StyleSheetCache
    {
        struct SheetHandleKey
        {
            public readonly int sheetInstanceID;
            public readonly int index;

            public SheetHandleKey(StyleSheet sheet, int index)
            {
                this.sheetInstanceID = sheet.GetInstanceID();
                this.index = index;
            }
        }

        class SheetHandleKeyComparer : IEqualityComparer<SheetHandleKey>
        {
            public bool Equals(SheetHandleKey x, SheetHandleKey y)
            {
                return x.sheetInstanceID == y.sheetInstanceID && x.index == y.index;
            }

            public int GetHashCode(SheetHandleKey key)
            {
                unchecked
                {
                    return key.sheetInstanceID.GetHashCode() ^ key.index.GetHashCode();
                }
            }
        }

        static SheetHandleKeyComparer s_Comparer = new SheetHandleKeyComparer();

        // cache of parsed enums for a given sheet and string value index
        static Dictionary<SheetHandleKey, int> s_EnumToIntCache = new Dictionary<SheetHandleKey, int>(s_Comparer);

        // cache of ordered propertyIDs for properties of a given rule
        static Dictionary<SheetHandleKey, StylePropertyID[]> s_RulePropertyIDsCache = new Dictionary<SheetHandleKey, StylePropertyID[]>(s_Comparer);

        // cache of builtin properties (e.g. "margin-left" to their enum equivalent)
        // this is static data and never changes at runtime
        static Dictionary<string, StylePropertyID> s_NameToIDCache = new Dictionary<string, StylePropertyID>()
        {
            {"width", StylePropertyID.Width},
            {"height", StylePropertyID.Height},
            {"max-width", StylePropertyID.MaxWidth},
            {"max-height", StylePropertyID.MaxHeight},
            {"min-width", StylePropertyID.MinWidth},
            {"min-height", StylePropertyID.MinHeight},
            {"flex", StylePropertyID.Flex},
            {"flex-wrap", StylePropertyID.FlexWrap},
            {"flex-basis", StylePropertyID.FlexBasis},
            {"flex-grow", StylePropertyID.FlexGrow},
            {"flex-shrink", StylePropertyID.FlexShrink},
            {"overflow", StylePropertyID.Overflow},
            {"-unity-overflow-clip-box", StylePropertyID.OverflowClipBox},
            {"left", StylePropertyID.PositionLeft},
            {"top", StylePropertyID.PositionTop},
            {"right", StylePropertyID.PositionRight},
            {"bottom", StylePropertyID.PositionBottom},
            {"margin-left", StylePropertyID.MarginLeft},
            {"margin-top", StylePropertyID.MarginTop},
            {"margin-right", StylePropertyID.MarginRight},
            {"margin-bottom", StylePropertyID.MarginBottom},
            {"padding-left", StylePropertyID.PaddingLeft},
            {"padding-top", StylePropertyID.PaddingTop},
            {"padding-right", StylePropertyID.PaddingRight},
            {"padding-bottom", StylePropertyID.PaddingBottom},
            {"position", StylePropertyID.Position},
            {"align-self", StylePropertyID.AlignSelf},
            {"-unity-text-align", StylePropertyID.UnityTextAlign},
            {"-unity-font-style", StylePropertyID.FontStyleAndWeight},
            {"-unity-font", StylePropertyID.Font},
            {"font-size", StylePropertyID.FontSize},
            {"white-space", StylePropertyID.WhiteSpace},
            {"color", StylePropertyID.Color},
            {"flex-direction", StylePropertyID.FlexDirection},
            {"background-color", StylePropertyID.BackgroundColor},
            {"border-color", StylePropertyID.BorderColor},
            {"background-image", StylePropertyID.BackgroundImage},
            {"-unity-background-scale-mode", StylePropertyID.BackgroundScaleMode},
            {"-unity-background-image-tint-color", StylePropertyID.BackgroundImageTintColor},
            {"align-items", StylePropertyID.AlignItems},
            {"align-content", StylePropertyID.AlignContent},
            {"justify-content", StylePropertyID.JustifyContent},
            {"border-left-width", StylePropertyID.BorderLeftWidth},
            {"border-top-width", StylePropertyID.BorderTopWidth},
            {"border-right-width", StylePropertyID.BorderRightWidth},
            {"border-bottom-width", StylePropertyID.BorderBottomWidth},
            {"border-top-left-radius", StylePropertyID.BorderTopLeftRadius},
            {"border-top-right-radius", StylePropertyID.BorderTopRightRadius},
            {"border-bottom-right-radius", StylePropertyID.BorderBottomRightRadius},
            {"border-bottom-left-radius", StylePropertyID.BorderBottomLeftRadius},
            {"-unity-slice-left", StylePropertyID.SliceLeft},
            {"-unity-slice-top", StylePropertyID.SliceTop},
            {"-unity-slice-right", StylePropertyID.SliceRight},
            {"-unity-slice-bottom", StylePropertyID.SliceBottom},
            {"opacity", StylePropertyID.Opacity},
            {"cursor", StylePropertyID.Cursor},
            {"visibility", StylePropertyID.Visibility},
            {"display", StylePropertyID.Display},
            // Shorthands
            {"border-radius", StylePropertyID.BorderRadius},
            {"border-width", StylePropertyID.BorderWidth},
            {"margin", StylePropertyID.Margin},
            {"padding", StylePropertyID.Padding}
        };

        // Used by tests
        internal static string GetPropertyIDUssName(StylePropertyID propertyId)
        {
            foreach (var kvp in s_NameToIDCache)
            {
                if (propertyId == kvp.Value)
                    return kvp.Key;
            }

            return string.Empty;
        }

        private static StyleValue[] s_InitialStyleValues = new StyleValue[(int)StylePropertyID.Custom];

        static StyleSheetCache()
        {
            // Values set to StyleKeyword.Initial require special treatment when applying them.
            s_InitialStyleValues[(int)StylePropertyID.MarginLeft] = StyleValue.Create(StylePropertyID.MarginLeft, 0f);
            s_InitialStyleValues[(int)StylePropertyID.MarginTop] = StyleValue.Create(StylePropertyID.MarginTop, 0f);
            s_InitialStyleValues[(int)StylePropertyID.MarginRight] = StyleValue.Create(StylePropertyID.MarginRight, 0f);
            s_InitialStyleValues[(int)StylePropertyID.MarginBottom] = StyleValue.Create(StylePropertyID.MarginBottom, 0f);
            s_InitialStyleValues[(int)StylePropertyID.PaddingLeft] = StyleValue.Create(StylePropertyID.PaddingLeft, 0f);
            s_InitialStyleValues[(int)StylePropertyID.PaddingTop] = StyleValue.Create(StylePropertyID.PaddingTop, 0f);
            s_InitialStyleValues[(int)StylePropertyID.PaddingRight] = StyleValue.Create(StylePropertyID.PaddingRight, 0f);
            s_InitialStyleValues[(int)StylePropertyID.PaddingBottom] = StyleValue.Create(StylePropertyID.PaddingBottom, 0f);
            s_InitialStyleValues[(int)StylePropertyID.Position] = StyleValue.Create(StylePropertyID.Position, (int)Position.Relative);
            s_InitialStyleValues[(int)StylePropertyID.PositionLeft] = StyleValue.Create(StylePropertyID.PositionLeft, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.PositionTop] = StyleValue.Create(StylePropertyID.PositionTop, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.PositionRight] = StyleValue.Create(StylePropertyID.PositionRight, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.PositionBottom] = StyleValue.Create(StylePropertyID.PositionBottom, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.Width] = StyleValue.Create(StylePropertyID.Width, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.Height] = StyleValue.Create(StylePropertyID.Height, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.MinWidth] = StyleValue.Create(StylePropertyID.MinWidth, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.MinHeight] = StyleValue.Create(StylePropertyID.MinHeight, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.MaxWidth] = StyleValue.Create(StylePropertyID.MaxWidth, StyleKeyword.None);
            s_InitialStyleValues[(int)StylePropertyID.MaxHeight] = StyleValue.Create(StylePropertyID.MaxHeight, StyleKeyword.None);
            s_InitialStyleValues[(int)StylePropertyID.FlexBasis] = StyleValue.Create(StylePropertyID.FlexBasis, StyleKeyword.Auto);
            s_InitialStyleValues[(int)StylePropertyID.FlexGrow] = StyleValue.Create(StylePropertyID.FlexGrow, 0f);
            s_InitialStyleValues[(int)StylePropertyID.FlexShrink] = StyleValue.Create(StylePropertyID.FlexShrink, 1f);
            s_InitialStyleValues[(int)StylePropertyID.BorderLeftWidth] = StyleValue.Create(StylePropertyID.BorderLeftWidth, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderTopWidth] = StyleValue.Create(StylePropertyID.BorderTopWidth, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderRightWidth] = StyleValue.Create(StylePropertyID.BorderRightWidth, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderBottomWidth] = StyleValue.Create(StylePropertyID.BorderBottomWidth, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderTopLeftRadius] = StyleValue.Create(StylePropertyID.BorderTopLeftRadius, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderTopRightRadius] = StyleValue.Create(StylePropertyID.BorderTopRightRadius, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderBottomLeftRadius] = StyleValue.Create(StylePropertyID.BorderBottomLeftRadius, 0f);
            s_InitialStyleValues[(int)StylePropertyID.BorderBottomRightRadius] = StyleValue.Create(StylePropertyID.BorderBottomRightRadius, 0f);
            s_InitialStyleValues[(int)StylePropertyID.FlexDirection] = StyleValue.Create(StylePropertyID.FlexDirection, (int)FlexDirection.Column);
            s_InitialStyleValues[(int)StylePropertyID.FlexWrap] = StyleValue.Create(StylePropertyID.FlexWrap, (int)Wrap.NoWrap);
            s_InitialStyleValues[(int)StylePropertyID.JustifyContent] = StyleValue.Create(StylePropertyID.JustifyContent, (int)Justify.FlexStart);
            s_InitialStyleValues[(int)StylePropertyID.AlignContent] = StyleValue.Create(StylePropertyID.AlignContent, (int)Align.FlexStart);
            s_InitialStyleValues[(int)StylePropertyID.AlignSelf] = StyleValue.Create(StylePropertyID.AlignSelf, (int)Align.Auto);
            s_InitialStyleValues[(int)StylePropertyID.AlignItems] = StyleValue.Create(StylePropertyID.AlignItems, (int)Align.Stretch);
            s_InitialStyleValues[(int)StylePropertyID.UnityTextAlign] = StyleValue.Create(StylePropertyID.UnityTextAlign, (int)TextAnchor.UpperLeft);
            s_InitialStyleValues[(int)StylePropertyID.WhiteSpace] = StyleValue.Create(StylePropertyID.WhiteSpace, (int)WhiteSpace.Normal);
            s_InitialStyleValues[(int)StylePropertyID.Font] = StyleValue.Create(StylePropertyID.Font); // null resource
            s_InitialStyleValues[(int)StylePropertyID.FontSize] = StyleValue.Create(StylePropertyID.FontSize, 0f);
            s_InitialStyleValues[(int)StylePropertyID.FontStyleAndWeight] = StyleValue.Create(StylePropertyID.FontStyleAndWeight, (int)FontStyle.Normal);
            s_InitialStyleValues[(int)StylePropertyID.BackgroundScaleMode] = StyleValue.Create(StylePropertyID.BackgroundScaleMode, (int)ScaleMode.StretchToFill);
            s_InitialStyleValues[(int)StylePropertyID.BackgroundImageTintColor] = StyleValue.Create(StylePropertyID.BackgroundImageTintColor, Color.white);
            s_InitialStyleValues[(int)StylePropertyID.Visibility] = StyleValue.Create(StylePropertyID.Visibility, (int)Visibility.Visible);
            s_InitialStyleValues[(int)StylePropertyID.Overflow] = StyleValue.Create(StylePropertyID.Overflow, (int)Overflow.Visible);
            s_InitialStyleValues[(int)StylePropertyID.OverflowClipBox] = StyleValue.Create(StylePropertyID.OverflowClipBox, (int)OverflowClipBox.PaddingBox);
            s_InitialStyleValues[(int)StylePropertyID.Display] = StyleValue.Create(StylePropertyID.Display, (int)DisplayStyle.Flex);
            s_InitialStyleValues[(int)StylePropertyID.BackgroundImage] = StyleValue.Create(StylePropertyID.BackgroundImage); // null resource
            s_InitialStyleValues[(int)StylePropertyID.Color] = StyleValue.Create(StylePropertyID.Color, Color.black);
            s_InitialStyleValues[(int)StylePropertyID.BackgroundColor] = StyleValue.Create(StylePropertyID.BackgroundColor, Color.clear);
            s_InitialStyleValues[(int)StylePropertyID.BorderColor] = StyleValue.Create(StylePropertyID.BorderColor, Color.clear);
            s_InitialStyleValues[(int)StylePropertyID.SliceLeft] = StyleValue.Create(StylePropertyID.SliceLeft, 0f);
            s_InitialStyleValues[(int)StylePropertyID.SliceTop] = StyleValue.Create(StylePropertyID.SliceTop, 0f);
            s_InitialStyleValues[(int)StylePropertyID.SliceRight] = StyleValue.Create(StylePropertyID.SliceRight, 0f);
            s_InitialStyleValues[(int)StylePropertyID.SliceBottom] = StyleValue.Create(StylePropertyID.SliceBottom, 0f);
            s_InitialStyleValues[(int)StylePropertyID.Opacity] = StyleValue.Create(StylePropertyID.Opacity, 1f);
            // Shorthand value
            s_InitialStyleValues[(int)StylePropertyID.BorderRadius] = StyleValue.Create(StylePropertyID.BorderRadius, StyleKeyword.Initial);
            s_InitialStyleValues[(int)StylePropertyID.BorderWidth] = StyleValue.Create(StylePropertyID.BorderWidth, StyleKeyword.Initial);
            s_InitialStyleValues[(int)StylePropertyID.Flex] = StyleValue.Create(StylePropertyID.Flex, StyleKeyword.Initial);
            s_InitialStyleValues[(int)StylePropertyID.Margin] = StyleValue.Create(StylePropertyID.Margin, StyleKeyword.Initial);
            s_InitialStyleValues[(int)StylePropertyID.Padding] = StyleValue.Create(StylePropertyID.Padding, StyleKeyword.Initial);
            // Complex value
            s_InitialStyleValues[(int)StylePropertyID.Cursor] = StyleValue.Create(StylePropertyID.Cursor, StyleKeyword.Initial);
        }

        internal static void ClearCaches()
        {
            s_EnumToIntCache.Clear();
            s_RulePropertyIDsCache.Clear();
        }

        internal static bool TryParseEnum<EnumType>(string enumValueName, out int intValue)
        {
            intValue = 0;

            try
            {
                enumValueName = enumValueName.Replace("-", string.Empty);
                object enumValue = Enum.Parse(typeof(EnumType), enumValueName, true);
                if (enumValue != null)
                {
                    intValue = (int)enumValue;
                    return true;
                }
            }
            catch (Exception)
            {
                Debug.LogError("Invalid value for " + typeof(EnumType).Name + ": " + enumValueName);
            }

            return false;
        }

        internal static int GetEnumValue<T>(StyleSheet sheet, StyleValueHandle handle)
        {
            Debug.Assert(handle.valueType == StyleValueType.Enum);

            SheetHandleKey key = new SheetHandleKey(sheet, handle.valueIndex);

            int value = 0;

            if (!s_EnumToIntCache.TryGetValue(key, out value))
            {
                if (TryParseEnum<T>(sheet.ReadEnum(handle), out value))
                {
                    s_EnumToIntCache.Add(key, value);
                    return value;
                }
            }
            Debug.Assert(Enum.GetName(typeof(T), value) != null);
            return value;
        }

        internal static StylePropertyID[] GetPropertyIDs(StyleSheet sheet, int ruleIndex)
        {
            SheetHandleKey key = new SheetHandleKey(sheet, ruleIndex);

            StylePropertyID[] propertyIDs;
            if (!s_RulePropertyIDsCache.TryGetValue(key, out propertyIDs))
            {
                StyleRule rule = sheet.rules[ruleIndex];
                propertyIDs = new StylePropertyID[rule.properties.Length];
                for (int i = 0; i < propertyIDs.Length; i++)
                {
                    propertyIDs[i] = GetPropertyID(sheet, rule, i);
                }
                s_RulePropertyIDsCache.Add(key, propertyIDs);
            }
            return propertyIDs;
        }

        internal static StyleValue GetInitialValue(StylePropertyID propertyId)
        {
            Debug.Assert(propertyId != StylePropertyID.Unknown && propertyId != StylePropertyID.Custom);
            return s_InitialStyleValues[(int)propertyId];
        }

        static Dictionary<string, string> s_DeprecatedNames = new Dictionary<string, string>()
        {
            {"position-left", "left"},
            {"position-top", "top"},
            {"position-right", "right"},
            {"position-bottom", "bottom"},
            {"text-color", "color"},
            {"slice-left", "-unity-slice-left" },
            {"slice-top", "-unity-slice-top" },
            {"slice-right", "-unity-slice-right" },
            {"slice-bottom", "-unity-slice-bottom" },
            {"text-alignment", "-unity-text-align" },
            {"word-wrap", "-unity-word-wrap" },
            {"font", "-unity-font" },
            {"background-size", "-unity-background-scale-mode" },
            {"font-style", "-unity-font-style" },
            {"position-type", "position" },
            {"border-left", "border-left-width"},
            {"border-top", "border-top-width"},
            {"border-right", "border-right-width"},
            {"border-bottom", "border-bottom-width"}
        };

        static string MapDeprecatedPropertyName(string name, string styleSheetName, int line)
        {
            string validName;
            s_DeprecatedNames.TryGetValue(name, out validName);


            return validName ?? name;
        }

        // Used by tests
        internal static StylePropertyID GetPropertyIDFromName(string name)
        {
            StylePropertyID id;
            if (s_NameToIDCache.TryGetValue(name, out id))
                return id;

            return StylePropertyID.Unknown;
        }

        static StylePropertyID GetPropertyID(StyleSheet sheet, StyleRule rule, int index)
        {
            string name = rule.properties[index].name;
            StylePropertyID id;

            name = MapDeprecatedPropertyName(name, sheet.name, rule.line);
            if (!s_NameToIDCache.TryGetValue(name, out id))
            {
                if (name.StartsWith("--"))
                {
                    id = StylePropertyID.Custom;
                }
                else
                {
                    id = StylePropertyID.Unknown;
                }
            }
            return id;
        }
    }
}
