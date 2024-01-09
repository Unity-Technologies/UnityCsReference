// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial struct ComputedStyle
    {
        public int customPropertiesCount => customProperties?.Count ?? 0;
        public bool hasTransition => computedTransitions?.Length > 0;

        public static ComputedStyle Create()
        {
            return InitialStyle.Acquire();
        }

        public void FinalizeApply(ref ComputedStyle parentStyle)
        {
            // Calculate pixel font size
            if (fontSize.unit == LengthUnit.Percent)
            {
                float parentSize = parentStyle.fontSize.value;
                float computedSize = parentSize * fontSize.value / 100;
                inheritedData.Write().fontSize = new Length(computedSize);
            }
        }

        private bool ApplyGlobalKeyword(StylePropertyReader reader, ref ComputedStyle parentStyle)
        {
            var handle = reader.GetValue(0).handle;
            if (handle.valueType == StyleValueType.Keyword)
            {
                switch ((StyleValueKeyword)handle.valueIndex)
                {
                    case StyleValueKeyword.Initial:
                        ApplyInitialValue(reader);
                        return true;
                    case StyleValueKeyword.Unset:
                        ApplyUnsetValue(reader, ref parentStyle);
                        return true;
                }
            }

            return false;
        }

        private bool ApplyGlobalKeyword(StylePropertyId id, StyleKeyword keyword, ref ComputedStyle parentStyle)
        {
            if (keyword == StyleKeyword.Initial)
            {
                ApplyInitialValue(id);
                return true;
            }

            return false;
        }

        private void RemoveCustomStyleProperty(StylePropertyReader reader)
        {
            var name = reader.property.name;
            if (customProperties == null || !customProperties.ContainsKey(name))
                return;

            customProperties.Remove(name);
        }

        private void ApplyCustomStyleProperty(StylePropertyReader reader)
        {
            dpiScaling = reader.dpiScaling;
            if (customProperties == null)
            {
                customProperties = new Dictionary<string, StylePropertyValue>();
            }

            var styleProperty = reader.property;

            // Custom property only support one value
            StylePropertyValue customProp = reader.GetValue(0);
            customProperties[styleProperty.name] = customProp;
        }

        private void ApplyAllPropertyInitial()
        {
            CopyFrom(ref InitialStyle.Get());
        }

        private void ResetComputedTransitions()
        {
            computedTransitions = null;
        }

        public static bool StartAnimationInlineTextShadow(VisualElement element, ref ComputedStyle computedStyle, StyleTextShadow textShadow, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = textShadow.keyword == StyleKeyword.Initial ? InitialStyle.textShadow : textShadow.value;
            return element.styleAnimation.Start(StylePropertyId.TextShadow, computedStyle.inheritedData.Read().textShadow, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineRotate(VisualElement element, ref ComputedStyle computedStyle, StyleRotate rotate, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = rotate.keyword == StyleKeyword.Initial ? InitialStyle.rotate : rotate.value;
            var result = element.styleAnimation.Start(StylePropertyId.Rotate, computedStyle.transformData.Read().rotate, to, durationMs, delayMs, easingCurve);

            if (result && (element.usageHints & UsageHints.DynamicTransform) == 0)
            {
                element.usageHints |= UsageHints.DynamicTransform;
            }

            return result;
        }

        public static bool StartAnimationInlineTranslate(VisualElement element, ref ComputedStyle computedStyle, StyleTranslate translate, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = translate.keyword == StyleKeyword.Initial ? InitialStyle.translate : translate.value;
            var result = element.styleAnimation.Start(StylePropertyId.Translate, computedStyle.transformData.Read().translate, to, durationMs, delayMs, easingCurve);

            if (result && (element.usageHints & UsageHints.DynamicTransform) == 0)
            {
                element.usageHints |= UsageHints.DynamicTransform;
            }

            return result;
        }

        public static bool StartAnimationInlineScale(VisualElement element, ref ComputedStyle computedStyle, StyleScale scale, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = scale.keyword == StyleKeyword.Initial ? InitialStyle.scale : scale.value;
            var result = element.styleAnimation.Start(StylePropertyId.Scale, computedStyle.transformData.Read().scale, to, durationMs, delayMs, easingCurve);

            if (result && (element.usageHints & UsageHints.DynamicTransform) == 0)
            {
                element.usageHints |= UsageHints.DynamicTransform;
            }

            return result;
        }

        public static bool StartAnimationInlineTransformOrigin(VisualElement element, ref ComputedStyle computedStyle, StyleTransformOrigin transformOrigin, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = transformOrigin.keyword == StyleKeyword.Initial ? InitialStyle.transformOrigin : transformOrigin.value;
            var result = element.styleAnimation.Start(StylePropertyId.TransformOrigin, computedStyle.transformData.Read().transformOrigin, to, durationMs, delayMs, easingCurve);

            if (result && (element.usageHints & UsageHints.DynamicTransform) == 0)
            {
                element.usageHints |= UsageHints.DynamicTransform;
            }

            return result;
        }

        public static bool StartAnimationInlineBackgroundSize(VisualElement element, ref ComputedStyle computedStyle, StyleBackgroundSize backgroundSize, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = backgroundSize.keyword == StyleKeyword.Initial ? InitialStyle.backgroundSize : backgroundSize.value;
            return element.styleAnimation.Start(StylePropertyId.BackgroundSize, computedStyle.visualData.Read().backgroundSize, to, durationMs, delayMs, easingCurve);
        }
    }
}
