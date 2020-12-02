using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal partial class ComputedStyle : ICustomStyle
    {
        internal readonly bool isShared;
        internal YogaNode yogaNode;

        internal Dictionary<string, StylePropertyValue> m_CustomProperties;

        public int customPropertiesCount
        {
            get { return m_CustomProperties != null ? m_CustomProperties.Count : 0; }
        }

        public static ComputedStyle Create(bool isShared = true)
        {
            var cs = new ComputedStyle(isShared);
            cs.CopyFrom(InitialStyle.Get());
            return cs;
        }

        public static ComputedStyle Create(ComputedStyle parentStyle, bool isShared = true)
        {
            var cs = Create(isShared);
            if (parentStyle != null)
            {
                cs.inheritedData = parentStyle.inheritedData;
            }
            return cs;
        }

        public static ComputedStyle CreateUninitialized(bool isShared = true)
        {
            return new ComputedStyle(isShared);
        }

        private ComputedStyle(bool isShared)
        {
            this.isShared = isShared;
        }

        public void CopyShared(ComputedStyle sharedStyle)
        {
            // Always just copy the reference to custom properties, since they can't be overriden per instance
            m_CustomProperties = sharedStyle.m_CustomProperties;

            CopyFrom(sharedStyle);
        }

        public void FinalizeApply(ComputedStyle parentStyle)
        {
            if (yogaNode == null)
                yogaNode = new YogaNode();

            if (parentStyle != null)
            {
                // Calculate pixel font size
                if (fontSize.unit == LengthUnit.Percent)
                {
                    float parentSize = parentStyle.fontSize.value;
                    float computedSize = parentSize * fontSize.value / 100;
                    inheritedData.fontSize = new Length(computedSize);
                }
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

        private bool ApplyGlobalKeyword(StylePropertyReader reader, ComputedStyle parentStyle)
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
                    if (parentStyle == null)
                        ApplyInitialValue(reader);
                    else
                        ApplyUnsetValue(reader, parentStyle);
                    return true;
                }
            }

            return false;
        }

        private float dpiScaling = 1.0f;

        private bool ApplyGlobalKeyword(StyleValue sv, ComputedStyle parentStyle)
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
            if (m_CustomProperties == null || !m_CustomProperties.ContainsKey(name))
                return;

            m_CustomProperties.Remove(name);
        }

        private void ApplyCustomStyleProperty(StylePropertyReader reader)
        {
            dpiScaling = reader.dpiScaling;
            if (m_CustomProperties == null)
            {
                m_CustomProperties = new Dictionary<string, StylePropertyValue>();
            }

            var styleProperty = reader.property;

            // Custom property only support one value
            StylePropertyValue customProp = reader.GetValue(0);
            m_CustomProperties[styleProperty.name] = customProp;
        }

        public bool TryGetValue(CustomStyleProperty<float> property, out float value)
        {
            if (TryGetValue(property.name, StyleValueType.Float, out var customProp))
            {
                if (customProp.sheet.TryReadFloat(customProp.handle, out value))
                    return true;
            }

            value = 0f;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<int> property, out int value)
        {
            if (TryGetValue(property.name, StyleValueType.Float, out var customProp))
            {
                float tmp = 0f;
                if (customProp.sheet.TryReadFloat(customProp.handle, out tmp))
                {
                    value = (int)tmp;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<bool> property, out bool value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                value = customProp.sheet.ReadKeyword(customProp.handle) == StyleValueKeyword.True;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Color> property, out Color value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                var handle = customProp.handle;
                switch (handle.valueType)
                {
                    case StyleValueType.Enum:
                    {
                        var colorName = customProp.sheet.ReadAsString(handle);
                        return StyleSheetColor.TryGetColor(colorName.ToLower(), out value);
                    }
                    case StyleValueType.Color:
                    {
                        if (customProp.sheet.TryReadColor(customProp.handle, out value))
                            return true;
                        break;
                    }
                    default:
                        LogCustomPropertyWarning(property.name, StyleValueType.Color, customProp);
                        break;
                }
            }

            value = Color.clear;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Texture2D> property, out Texture2D value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                var source = new ImageSource();
                if (StylePropertyReader.TryGetImageSourceFromValue(customProp, dpiScaling, out source) && source.texture != null)
                {
                    value = source.texture;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Sprite> property, out Sprite value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                var source = new ImageSource();
                if (StylePropertyReader.TryGetImageSourceFromValue(customProp, dpiScaling, out source) && source.sprite != null)
                {
                    value = source.sprite;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<VectorImage> property, out VectorImage value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                var source = new ImageSource();
                if (StylePropertyReader.TryGetImageSourceFromValue(customProp, dpiScaling, out source) && source.vectorImage != null)
                {
                    value = source.vectorImage;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<string> property, out string value)
        {
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out var customProp))
            {
                value = customProp.sheet.ReadAsString(customProp.handle);
                return true;
            }

            value = string.Empty;
            return false;
        }

        private bool TryGetValue(string propertyName, StyleValueType valueType, out StylePropertyValue customProp)
        {
            customProp = new StylePropertyValue();
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out customProp))
            {
                // CustomProperty only support one value
                var handle = customProp.handle;
                if (handle.valueType != valueType)
                {
                    LogCustomPropertyWarning(propertyName, valueType, customProp);
                    return false;
                }

                return true;
            }

            return false;
        }

        private static void LogCustomPropertyWarning(string propertyName, StyleValueType valueType, StylePropertyValue customProp)
        {
            Debug.LogWarning($"Trying to read custom property {propertyName} value as {valueType} while parsed type is {customProp.handle.valueType}");
        }
    }
}
