// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.RMGUI.StyleSheets
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
        // never changes even after style sheet reload
        static Dictionary<string, StylePropertyID> s_NameToIDCache = new Dictionary<string, StylePropertyID>();

        static StyleSheetCache()
        {

            var members = typeof(VisualElementStyles).GetFields();
            foreach (System.Reflection.FieldInfo field in members)
            {
                StylePropertyAttribute attribute = (StylePropertyAttribute)Attribute.GetCustomAttribute(field, typeof(StylePropertyAttribute));

                if (attribute != null)
                {
                    s_NameToIDCache.Add(attribute.propertyName, attribute.propertyID);
                }
            }
        }

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
