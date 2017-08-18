// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
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
            {"overflow", StylePropertyID.Overflow},
            {"position-left", StylePropertyID.PositionLeft},
            {"position-top", StylePropertyID.PositionTop},
            {"position-right", StylePropertyID.PositionRight},
            {"position-bottom", StylePropertyID.PositionBottom},
            {"margin-left", StylePropertyID.MarginLeft},
            {"margin-top", StylePropertyID.MarginTop},
            {"margin-right", StylePropertyID.MarginRight},
            {"margin-bottom", StylePropertyID.MarginBottom},
            {"border-left", StylePropertyID.BorderLeft},
            {"border-top", StylePropertyID.BorderTop},
            {"border-right", StylePropertyID.BorderRight},
            {"border-bottom", StylePropertyID.BorderBottom},
            {"padding-left", StylePropertyID.PaddingLeft},
            {"padding-top", StylePropertyID.PaddingTop},
            {"padding-right", StylePropertyID.PaddingRight},
            {"padding-bottom", StylePropertyID.PaddingBottom},
            {"position-type", StylePropertyID.PositionType},
            {"align-self", StylePropertyID.AlignSelf},
            {"text-alignment", StylePropertyID.TextAlignment},
            {"font-style", StylePropertyID.FontStyle},
            {"text-clipping", StylePropertyID.TextClipping},
            {"font", StylePropertyID.Font},
            {"font-size", StylePropertyID.FontSize},
            {"word-wrap", StylePropertyID.WordWrap},
            {"text-color", StylePropertyID.TextColor},
            {"flex-direction", StylePropertyID.FlexDirection},
            {"background-color", StylePropertyID.BackgroundColor},
            {"border-color", StylePropertyID.BorderColor},
            {"background-image", StylePropertyID.BackgroundImage},
            {"background-size", StylePropertyID.BackgroundSize},
            {"align-items", StylePropertyID.AlignItems},
            {"align-content", StylePropertyID.AlignContent},
            {"justify-content", StylePropertyID.JustifyContent},
            {"flex-wrap", StylePropertyID.FlexWrap},
            {"border-left-width", StylePropertyID.BorderLeftWidth},
            {"border-top-width", StylePropertyID.BorderTopWidth},
            {"border-right-width", StylePropertyID.BorderRightWidth},
            {"border-bottom-width", StylePropertyID.BorderBottomWidth},
            {"border-radius", StylePropertyID.BorderRadius},
            {"border-top-left-radius", StylePropertyID.BorderTopLeftRadius},
            {"border-top-right-radius", StylePropertyID.BorderTopRightRadius},
            {"border-bottom-right-radius", StylePropertyID.BorderBottomRightRadius},
            {"border-bottom-left-radius", StylePropertyID.BorderBottomLeftRadius},
            {"slice-left", StylePropertyID.SliceLeft},
            {"slice-top", StylePropertyID.SliceTop},
            {"slice-right", StylePropertyID.SliceRight},
            {"slice-bottom", StylePropertyID.SliceBottom},
            {"opacity", StylePropertyID.Opacity}
        };

        internal static void ClearCaches()
        {
            s_EnumToIntCache.Clear();
            s_RulePropertyIDsCache.Clear();
        }

        internal static int GetEnumValue<T>(StyleSheet sheet, StyleValueHandle handle)
        {
            Debug.Assert(handle.valueType == StyleValueType.Enum);

            SheetHandleKey key = new SheetHandleKey(sheet, handle.valueIndex);

            int value;
            if (!s_EnumToIntCache.TryGetValue(key, out value))
            {
                string enumValueName = sheet.ReadEnum(handle).Replace("-", string.Empty);
                object enumValue = Enum.Parse(typeof(T), enumValueName, true);
                value = (int)enumValue;
                s_EnumToIntCache.Add(key, value);
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
                    propertyIDs[i] = GetPropertyID(rule.properties[i].name);
                }
                s_RulePropertyIDsCache.Add(key, propertyIDs);
            }
            return propertyIDs;
        }

        static StylePropertyID GetPropertyID(string name)
        {
            StylePropertyID id;
            if (!s_NameToIDCache.TryGetValue(name, out id))
            {
                id = StylePropertyID.Custom;
            }
            return id;
        }
    }
}
