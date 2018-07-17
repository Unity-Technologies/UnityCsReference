// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    internal class SplitSheetData
    {
        public StyleSheet common;
        public StyleSheet s1;
        public StyleSheet s2;
    }

    internal class PropertyPair
    {
        public string name;
        public StyleProperty p1;
        public StyleProperty p2;
    }

    internal class StyleSheetSplitter
    {
        public StyleSheetBuilderHelper s1Builder;
        public StyleSheetCache s1Cache;
        public UssComments s1Comments;
        public StyleSheetBuilderHelper s2Builder;
        public StyleSheetCache s2Cache;
        public UssComments s2Comments;
        public StyleSheetBuilderHelper commonBuilder;

        #region ConversionAPI
        public SplitSheetData Split(StyleSheet s1, StyleSheet s2, UssComments s1SrcComments = null, UssComments s2SrcComments = null)
        {
            s1Cache = new StyleSheetCache(s1);
            s1Comments = s1SrcComments ?? new UssComments();

            s2Cache = new StyleSheetCache(s2);
            s2Comments = s2SrcComments ?? new UssComments();

            s1Builder = new StyleSheetBuilderHelper();
            s2Builder = new StyleSheetBuilderHelper();
            commonBuilder = new StyleSheetBuilderHelper();

            var allSelectors = new HashSet<string>(s1Cache.selectors.Keys);
            allSelectors.UnionWith(s2Cache.selectors.Keys);

            foreach (var selectorStr in allSelectors)
            {
                StyleComplexSelector complexSelector1;
                s1Cache.selectors.TryGetValue(selectorStr, out complexSelector1);

                StyleComplexSelector complexSelector2;
                s2Cache.selectors.TryGetValue(selectorStr, out complexSelector2);

                if (complexSelector1 != null)
                {
                    if (complexSelector2 != null)
                    {
                        // Common rules, write common properties
                        Split(complexSelector1, complexSelector2);
                    }
                    else
                    {
                        // Rules only existing in S1: copy it straight
                        StyleSheetBuilderHelper.CopySelector(s1, s1Comments, complexSelector1, s1Builder);
                    }
                }
                else
                {
                    // Rules only existing in S2: copy it straight
                    StyleSheetBuilderHelper.CopySelector(s2, s2Comments, complexSelector2, s2Builder);
                }
            }

            commonBuilder.PopulateSheet();
            s1Builder.PopulateSheet();
            s2Builder.PopulateSheet();

            var result = new SplitSheetData { common = commonBuilder.sheet, s1 = s1Builder.sheet, s2 = s2Builder.sheet };
            return result;
        }

        #endregion

        #region Implementation
        private bool CompareProperty(StyleProperty p1, StyleProperty p2)
        {
            if (p1.values.Length != p2.values.Length)
            {
                return false;
            }

            for (var i = 0; i < p1.values.Length; i++)
            {
                var v1 = p1.values[i];
                var v2 = p2.values[i];

                if (v1.valueType != v2.valueType)
                    return false;

                switch (v1.valueType)
                {
                    case StyleValueType.Color:
                        var c1 = s1Cache.sheet.ReadColor(v1);
                        var c2 = s2Cache.sheet.ReadColor(v2);
                        if (!GUISkinCompare.CompareTo(c1, c2))
                        {
                            return false;
                        }

                        break;
                    case StyleValueType.Enum:
                        var e1 = s1Cache.sheet.ReadEnum(v1);
                        var e2 = s2Cache.sheet.ReadEnum(v2);
                        if (e1 != e2)
                        {
                            return false;
                        }

                        break;
                    case StyleValueType.Float:
                        var f1 = s1Cache.sheet.ReadFloat(v1);
                        var f2 = s2Cache.sheet.ReadFloat(v2);
                        if (!GUISkinCompare.CompareTo(f1, f2))
                        {
                            return false;
                        }

                        break;
                    case StyleValueType.Keyword:
                        var k1 = s1Cache.sheet.ReadKeyword(v1);
                        var k2 = s2Cache.sheet.ReadKeyword(v2);
                        if (k1 != k2)
                        {
                            return false;
                        }
                        break;
                    case StyleValueType.ResourcePath:
                        var rp1 = s1Cache.sheet.ReadResourcePath(v1);
                        var rp2 = s2Cache.sheet.ReadResourcePath(v2);
                        if (rp1 != rp2)
                        {
                            return false;
                        }
                        break;
                    case StyleValueType.String:
                        var s1 = s1Cache.sheet.ReadString(v1);
                        var s2 = s2Cache.sheet.ReadString(v2);
                        if (s1 != s2)
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        private void Split(StyleComplexSelector complexSelector1, StyleComplexSelector complexSelector2)
        {
            var comment1 = s1Comments.Get(complexSelector1.rule);
            s1Builder.BeginRule(comment1);
            StyleSheetBuilderHelper.BuildSelector(complexSelector1, s1Builder);

            var comment2 = s2Comments.Get(complexSelector2.rule);
            s2Builder.BeginRule(comment2);
            StyleSheetBuilderHelper.BuildSelector(complexSelector2, s2Builder);

            commonBuilder.BeginRule(string.IsNullOrEmpty(comment1) ? comment2 : comment1);
            StyleSheetBuilderHelper.BuildSelector(complexSelector2, commonBuilder);

            // This is a common selector to both s1 and s2, for each properties determine what is common:
            var properties = new Dictionary<string, PropertyPair>();
            StyleSheetBuilderHelper.PopulateProperties(complexSelector1.rule.properties, properties, true);
            StyleSheetBuilderHelper.PopulateProperties(complexSelector2.rule.properties, properties, false);

            foreach (var propertyPair in properties.Values)
            {
                if (propertyPair.p1 != null)
                {
                    if (propertyPair.p2 != null)
                    {
                        // Extend needs to be in common, s1 and s2:
                        if (propertyPair.p1.name == ConverterUtils.k_Extend)
                        {
                            StyleSheetBuilderHelper.CopyProperty(s1Cache.sheet, s1Comments, propertyPair.p1, commonBuilder);
                            StyleSheetBuilderHelper.CopyProperty(s1Cache.sheet, s1Comments, propertyPair.p1, s1Builder);
                            StyleSheetBuilderHelper.CopyProperty(s2Cache.sheet, s2Comments, propertyPair.p2, s2Builder);
                        }
                        // Possibly common property
                        else if (CompareProperty(propertyPair.p1, propertyPair.p2))
                        {
                            StyleSheetBuilderHelper.CopyProperty(s1Cache.sheet, s1Comments, propertyPair.p1, commonBuilder);
                        }
                        else
                        {
                            StyleSheetBuilderHelper.CopyProperty(s1Cache.sheet, s1Comments, propertyPair.p1, s1Builder);
                            StyleSheetBuilderHelper.CopyProperty(s2Cache.sheet, s2Comments, propertyPair.p2, s2Builder);
                        }
                    }
                    else
                    {
                        // Only in s1: copy straight
                        StyleSheetBuilderHelper.CopyProperty(s1Cache.sheet, s1Comments, propertyPair.p1, s1Builder);
                    }
                }
                else
                {
                    // Only in s2: copy straight
                    StyleSheetBuilderHelper.CopyProperty(s2Cache.sheet, s2Comments, propertyPair.p2, s2Builder);
                }
            }

            s1Builder.EndRule();
            s2Builder.EndRule();
            commonBuilder.EndRule();
        }

        #endregion
    }
}
