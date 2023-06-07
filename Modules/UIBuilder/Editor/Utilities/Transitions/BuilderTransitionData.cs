// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Helper type to group the transition-related property manipulators together, since we are always using them together.
    /// </summary>
    readonly struct BuilderTransitionData : IDisposable
    {
        public BuilderTransitionData(StyleSheet styleSheet, StyleRule styleRule, VisualElement element, bool editorExtensionMode)
        {
            transitionProperty = styleSheet.GetStylePropertyManipulator(element, styleRule, StylePropertyId.TransitionProperty.UssName(), editorExtensionMode);
            transitionDuration = styleSheet.GetStylePropertyManipulator(element, styleRule, StylePropertyId.TransitionDuration.UssName(), editorExtensionMode);
            transitionTimingFunction = styleSheet.GetStylePropertyManipulator(element, styleRule, StylePropertyId.TransitionTimingFunction.UssName(), editorExtensionMode);
            transitionDelay = styleSheet.GetStylePropertyManipulator(element, styleRule, StylePropertyId.TransitionDelay.UssName(), editorExtensionMode);
        }

        public readonly StylePropertyManipulator transitionProperty;
        public readonly StylePropertyManipulator transitionDuration;
        public readonly StylePropertyManipulator transitionTimingFunction;
        public readonly StylePropertyManipulator transitionDelay;

        public int MaxCount()
        {
            return Mathf.Max(transitionProperty.GetValuesCount(),
                Mathf.Max(transitionDuration.GetValuesCount(),
                    Mathf.Max(transitionTimingFunction.GetValuesCount(),
                        transitionDelay.GetValuesCount())));
        }

        public TransitionChangeType GetOverrides()
        {
            var overrides = TransitionChangeType.None;
            if (null != transitionProperty.styleProperty)
                overrides |= TransitionChangeType.Property;

            if (null != transitionDuration.styleProperty)
                overrides |= TransitionChangeType.Duration;

            if (null != transitionTimingFunction.styleProperty)
                overrides |= TransitionChangeType.TimingFunction;

            if (null != transitionDelay.styleProperty)
                overrides |= TransitionChangeType.Delay;
            return overrides;
        }

        public TransitionChangeType GetKeywords()
        {
            var keywords = TransitionChangeType.None;
            if (IsKeyword(transitionProperty))
                keywords |= TransitionChangeType.Property;
            if (IsKeyword(transitionDuration))
                keywords |= TransitionChangeType.Duration;
            if (IsKeyword(transitionTimingFunction))
                keywords |= TransitionChangeType.TimingFunction;
            if (IsKeyword(transitionDelay))
                keywords |= TransitionChangeType.Delay;
            return keywords;
        }

        public TransitionChangeType GetBindings()
        {
            var bindings = TransitionChangeType.None;

            if (DataBindingUtility.TryGetLastUIBindingResult(VisualElement.StyleProperties.transitionPropertyProperty, transitionProperty.element, out var propertyBindingResult)
                && propertyBindingResult.status == BindingStatus.Success)
            {
                bindings |= TransitionChangeType.Property;
            }
            if (DataBindingUtility.TryGetLastUIBindingResult(VisualElement.StyleProperties.transitionDurationProperty, transitionDuration.element, out var durationBindingResult)
                && durationBindingResult.status == BindingStatus.Success)
            {
                bindings |= TransitionChangeType.Duration;
            }
            if (DataBindingUtility.TryGetLastUIBindingResult(VisualElement.StyleProperties.transitionTimingFunctionProperty, transitionTimingFunction.element, out var timingFunctionBindingResult)
                && timingFunctionBindingResult.status == BindingStatus.Success)
            {
                bindings |= TransitionChangeType.TimingFunction;
            }
            if (DataBindingUtility.TryGetLastUIBindingResult(VisualElement.StyleProperties.transitionDelayProperty, transitionDelay.element, out var delayBindingResult)
                && delayBindingResult.status == BindingStatus.Success)
            {
                bindings |= TransitionChangeType.Delay;
            }

            return bindings;
        }

        bool IsKeyword(StylePropertyManipulator manipulator)
        {
            return manipulator.GetValuesCount() == 1 &&
                   manipulator.GetValueContextAtIndex(0).handle.valueType == StyleValueType.Keyword;
        }

        public void Dispose()
        {
            transitionProperty?.Dispose();
            transitionDuration?.Dispose();
            transitionTimingFunction?.Dispose();
            transitionDelay?.Dispose();
        }
    }
}
