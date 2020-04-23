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

        // cache of ordered propertyIDs for properties of a given rule
        static Dictionary<SheetHandleKey, StylePropertyId[]> s_RulePropertyIdsCache = new Dictionary<SheetHandleKey, StylePropertyId[]>(s_Comparer);

        internal static void ClearCaches()
        {
            s_RulePropertyIdsCache.Clear();
        }

        internal static StylePropertyId[] GetPropertyIds(StyleSheet sheet, int ruleIndex)
        {
            SheetHandleKey key = new SheetHandleKey(sheet, ruleIndex);

            StylePropertyId[] propertyIds;
            if (!s_RulePropertyIdsCache.TryGetValue(key, out propertyIds))
            {
                StyleRule rule = sheet.rules[ruleIndex];
                propertyIds = new StylePropertyId[rule.properties.Length];
                for (int i = 0; i < propertyIds.Length; i++)
                {
                    propertyIds[i] = GetPropertyId(rule, i);
                }
                s_RulePropertyIdsCache.Add(key, propertyIds);
            }
            return propertyIds;
        }

        internal static StylePropertyId[] GetPropertyIds(StyleRule rule)
        {
            var propertyIds = new StylePropertyId[rule.properties.Length];
            for (int i = 0; i < propertyIds.Length; i++)
            {
                propertyIds[i] = GetPropertyId(rule, i);
            }

            return propertyIds;
        }

        static StylePropertyId GetPropertyId(StyleRule rule, int index)
        {
            var property = rule.properties[index];
            string name = property.name;

            StylePropertyId id;
            if (!StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                id = property.isCustomProperty ? StylePropertyId.Custom : StylePropertyId.Unknown;
            }
            return id;
        }
    }
}
