// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal static partial class StyleDebug
    {
        internal const int UnitySpecificity = -1;
        internal const int UndefinedSpecificity = 0;
        internal const int InheritedSpecificity = int.MaxValue - 1;
        internal const int InlineSpecificity = int.MaxValue;

        public static string[] GetStylePropertyNames()
        {
            var list = StylePropertyUtil.s_NameToId.Keys.ToList();
            list.Sort();
            return list.ToArray();
        }

        public static string[] GetLonghandPropertyNames(string shorthandName)
        {
            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(shorthandName, out id))
            {
                if (IsShorthandProperty(id))
                    return GetLonghandPropertyNames(id);
            }
            return null;
        }

        public static StylePropertyId GetStylePropertyIdFromName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return StylePropertyId.Unknown;

            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
                return id;

            return StylePropertyId.Unknown;
        }

        public static object GetComputedStyleValue(in ComputedStyle computedStyle, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                // Support obsolete -unity-background-scale-mode
                if (id == StylePropertyId.UnityBackgroundScaleMode)
                {
                    return BackgroundPropertyHelper.ResolveUnityBackgroundScaleMode(computedStyle.backgroundPositionX,
                        computedStyle.backgroundPositionY, computedStyle.backgroundRepeat,
                        computedStyle.backgroundSize);
                }

                return GetComputedStyleValue(computedStyle, id);
            }

            return null;
        }

        public static object GetInlineStyleValue(IStyle style, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                // Support obsolete -unity-background-scale-mode
#pragma warning disable 0618
                if (id == StylePropertyId.UnityBackgroundScaleMode)
                    return style.unityBackgroundScaleMode;
#pragma warning restore 0618

                return GetInlineStyleValue(style, id);
            }

            return null;
        }

        public static void SetInlineStyleValue(IStyle style, string name, object value)
        {
            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                SetInlineStyleValue(style, id, value);
            }
        }

        public static Type GetInlineStyleType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                if (id == StylePropertyId.UnityBackgroundScaleMode)
                    return typeof(StyleEnum<ScaleMode>);

                if (!IsShorthandProperty(id))
                    return GetInlineStyleType(id);
            }

            return null;
        }

        public static Type GetComputedStyleType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            StylePropertyId id;
            if (StylePropertyUtil.s_NameToId.TryGetValue(name, out id))
            {
                if (id == StylePropertyId.UnityBackgroundScaleMode)
                    return typeof(ScaleMode);

                if (!IsShorthandProperty(id))
                    return GetComputedStyleType(id);
            }

            return null;
        }

        public static void FindSpecifiedStyles(in ComputedStyle computedStyle, IEnumerable<SelectorMatchRecord> matchRecords, Dictionary<StylePropertyId, int> result)
        {
            result.Clear();

            // Find matched styles
            foreach (var record in matchRecords)
            {
                int specificity = record.complexSelector.specificity;
                if (record.sheet.isDefaultStyleSheet)
                    specificity = UnitySpecificity;

                var properties = record.complexSelector.rule.properties;
                foreach (var property in properties)
                {
                    StylePropertyId id;
                    if (StylePropertyUtil.s_NameToId.TryGetValue(property.name, out id))
                    {
                        if (IsShorthandProperty(id))
                        {
                            var longhands = GetLonghandPropertyNames(id);
                            foreach (var longhand in longhands)
                            {
                                var longhandId = GetStylePropertyIdFromName(longhand);
                                result[longhandId] = specificity;
                            }
                        }
                        else
                        {
                            result[id] = specificity;
                        }
                    }
                }
            }

            // Find inherited properties
            var inheritedPropId = StyleDebug.GetInheritedProperties();
            foreach (var id in inheritedPropId)
            {
                if (result.ContainsKey(id))
                    continue;

                var value = StyleDebug.GetComputedStyleValue(computedStyle, id);
                var initialValue = StyleDebug.GetComputedStyleValue(InitialStyle.Get(), id);

                if (value != null && !value.Equals(initialValue))
                {
                    result[id] = InheritedSpecificity;
                }
            }
        }
    }
}
