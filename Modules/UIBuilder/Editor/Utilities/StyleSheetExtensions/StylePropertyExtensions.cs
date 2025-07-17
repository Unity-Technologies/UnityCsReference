// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class StylePropertyExtensions
    {
        internal static StylePropertyManipulator GetStylePropertyManipulator(
            this StyleSheet styleSheet,
            VisualElement element,
            StyleRule rule,
            string propertyName,
            bool editorExtensionMode)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            var manipulator = StylePropertyManipulator.GetPooled();
            manipulator.element = element;
            manipulator.styleSheet = styleSheet;
            manipulator.propertyName = propertyName;
            manipulator.styleRule = rule;
            manipulator.editorExtensionMode = editorExtensionMode;

            var property = manipulator.styleProperty;
            if (null == property)
                return manipulator;

            for (var i = 0; i < property.values.Length; ++i)
            {
                var index = i;
                var valueInfo = StylePropertyManipulator.ResolveValueOrVariable(styleSheet, element, rule, property, ref i, editorExtensionMode);
                valueInfo.offset = index;
                manipulator.stylePropertyParts.Add(valueInfo);
            }

            return manipulator;
        }
    }
}
