using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal partial struct ComputedStyle
    {
        public int customPropertiesCount => customProperties?.Count ?? 0;

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
                if ((StyleValueKeyword)handle.valueIndex == StyleValueKeyword.Initial)
                {
                    ApplyInitialValue(reader);
                    return true;
                }
                if ((StyleValueKeyword)handle.valueIndex == StyleValueKeyword.Unset)
                {
                    ApplyUnsetValue(reader, ref parentStyle);
                    return true;
                }
            }

            return false;
        }

        private bool ApplyGlobalKeyword(StyleValue sv, ref ComputedStyle parentStyle)
        {
            if (sv.keyword == StyleKeyword.Initial)
            {
                ApplyInitialValue(sv.id);
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
    }
}
