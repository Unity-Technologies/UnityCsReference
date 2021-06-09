// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
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
            if (yogaNode == null)
                yogaNode = new YogaNode();

            // Calculate pixel font size
            if (fontSize.unit == LengthUnit.Percent)
            {
                float parentSize = parentStyle.fontSize.value;
                float computedSize = parentSize * fontSize.value / 100;
                inheritedData.Write().fontSize = new Length(computedSize);
            }

            SyncWithLayout(yogaNode);
        }

        public void SyncWithLayout(YogaNode targetNode)
        {
            targetNode.Flex = float.NaN;

            targetNode.FlexGrow = flexGrow;
            targetNode.FlexShrink = flexShrink;
            targetNode.FlexBasis = flexBasis.ToYogaValue();
            targetNode.Left = left.ToYogaValue();
            targetNode.Top = top.ToYogaValue();
            targetNode.Right = right.ToYogaValue();
            targetNode.Bottom = bottom.ToYogaValue();
            targetNode.MarginLeft = marginLeft.ToYogaValue();
            targetNode.MarginTop = marginTop.ToYogaValue();
            targetNode.MarginRight = marginRight.ToYogaValue();
            targetNode.MarginBottom = marginBottom.ToYogaValue();
            targetNode.PaddingLeft = paddingLeft.ToYogaValue();
            targetNode.PaddingTop = paddingTop.ToYogaValue();
            targetNode.PaddingRight = paddingRight.ToYogaValue();
            targetNode.PaddingBottom = paddingBottom.ToYogaValue();
            targetNode.BorderLeftWidth = borderLeftWidth;
            targetNode.BorderTopWidth = borderTopWidth;
            targetNode.BorderRightWidth = borderRightWidth;
            targetNode.BorderBottomWidth = borderBottomWidth;
            targetNode.Width = width.ToYogaValue();
            targetNode.Height = height.ToYogaValue();

            targetNode.PositionType = (YogaPositionType)position;
            targetNode.Overflow = (YogaOverflow)overflow;
            targetNode.AlignSelf = (YogaAlign)alignSelf;
            targetNode.MaxWidth = maxWidth.ToYogaValue();
            targetNode.MaxHeight = maxHeight.ToYogaValue();
            targetNode.MinWidth = minWidth.ToYogaValue();
            targetNode.MinHeight = minHeight.ToYogaValue();

            targetNode.FlexDirection = (YogaFlexDirection)flexDirection;
            targetNode.AlignContent = (YogaAlign)alignContent;
            targetNode.AlignItems = (YogaAlign)alignItems;
            targetNode.JustifyContent = (YogaJustify)justifyContent;
            targetNode.Wrap = (YogaWrap)flexWrap;
            targetNode.Display = (YogaDisplay)display;
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

        public static VersionChangeType CompareChanges(ref ComputedStyle x, ref ComputedStyle y)
        {
            // This is a pre-emptive since we do not know if style changes actually cause a repaint or a layout
            // But those should be the only possible type of changes needed
            VersionChangeType changes = VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint;

            if (x.overflow != y.overflow)
                changes |= VersionChangeType.Overflow;

            if (x.borderBottomLeftRadius != y.borderBottomLeftRadius ||
                x.borderBottomRightRadius != y.borderBottomRightRadius ||
                x.borderTopLeftRadius != y.borderTopLeftRadius ||
                x.borderTopRightRadius != y.borderTopRightRadius)
            {
                changes |= VersionChangeType.BorderRadius;
            }

            if (x.borderLeftWidth != y.borderLeftWidth ||
                x.borderTopWidth != y.borderTopWidth ||
                x.borderRightWidth != y.borderRightWidth ||
                x.borderBottomWidth != y.borderBottomWidth)
            {
                changes |= VersionChangeType.BorderWidth;
            }

            if (x.opacity != y.opacity)
                changes |= VersionChangeType.Opacity;

            if (!ComputedTransitionUtils.SameTransitionProperty(ref x, ref y))
                changes |= VersionChangeType.TransitionProperty;

            if (x.transformOrigin != y.transformOrigin ||
                x.translate != y.translate ||
                x.scale != y.scale ||
                x.rotate != y.rotate)
            {
                changes |= VersionChangeType.Transform;
            }

            return changes;
        }

        public static bool StartAnimationInlineCursor(VisualElement element, ref ComputedStyle computedStyle, StyleCursor cursor, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = cursor.keyword == StyleKeyword.Initial ? InitialStyle.cursor : cursor.value;
            return element.styleAnimation.Start(StylePropertyId.Cursor, computedStyle.rareData.Read().cursor, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineTextShadow(VisualElement element, ref ComputedStyle computedStyle, StyleTextShadow textShadow, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = textShadow.keyword == StyleKeyword.Initial ? InitialStyle.textShadow : textShadow.value;
            return element.styleAnimation.Start(StylePropertyId.TextShadow, computedStyle.inheritedData.Read().textShadow, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineRotate(VisualElement element, ref ComputedStyle computedStyle, StyleRotate rotate, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = rotate.keyword == StyleKeyword.Initial ? InitialStyle.rotate : rotate.value;
            return element.styleAnimation.Start(StylePropertyId.Rotate, computedStyle.transformData.Read().rotate, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineTranslate(VisualElement element, ref ComputedStyle computedStyle, StyleTranslate translate, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = translate.keyword == StyleKeyword.Initial ? InitialStyle.translate : translate.value;
            return element.styleAnimation.Start(StylePropertyId.Translate, computedStyle.transformData.Read().translate, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineScale(VisualElement element, ref ComputedStyle computedStyle, StyleScale scale, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = scale.keyword == StyleKeyword.Initial ? InitialStyle.scale : scale.value;
            return element.styleAnimation.Start(StylePropertyId.Scale, computedStyle.transformData.Read().scale, to, durationMs, delayMs, easingCurve);
        }

        public static bool StartAnimationInlineTransformOrigin(VisualElement element, ref ComputedStyle computedStyle, StyleTransformOrigin transformOrigin, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            var to = transformOrigin.keyword == StyleKeyword.Initial ? InitialStyle.transformOrigin : transformOrigin.value;
            return element.styleAnimation.Start(StylePropertyId.TransformOrigin, computedStyle.transformData.Read().transformOrigin, to, durationMs, delayMs, easingCurve);
        }
    }
}
