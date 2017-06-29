// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.CSSLayout;

namespace UnityEngine.Experimental.UIElements
{
    public partial class VisualElement : IStyle
    {
        // this allows us to not expose all styles accessors directly on VisualElement class
        // but still let the VisualElement be in control of proxying inline styles modifications
        // we avoid an extra allocation when returning that property and it's nice from an API perspective
        public IStyle style
        {
            get { return this; }
        }

        StyleValue<float> IStyle.width
        {
            get { return effectiveStyle.width; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.width.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.width = value;
                cssNode.Width = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.height
        {
            get { return effectiveStyle.height; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.height.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.height = value;
                cssNode.Height = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.maxWidth
        {
            get { return effectiveStyle.maxWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.maxWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.maxWidth = value;
                cssNode.MaxWidth = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.maxHeight
        {
            get { return effectiveStyle.maxHeight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.maxHeight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.maxHeight = value;
                cssNode.MaxHeight = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.minWidth
        {
            get { return effectiveStyle.minWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.minWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.minWidth = value;
                cssNode.MinWidth = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.minHeight
        {
            get { return effectiveStyle.minHeight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.minHeight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.minHeight = value;
                cssNode.MinHeight = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<float> IStyle.flex
        {
            get { return effectiveStyle.flex; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.flex.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.flex = value;
                cssNode.Flex = value.GetSpecifiedValueOrDefault(float.NaN);
            }
        }

        StyleValue<Overflow> IStyle.overflow
        {
            get
            {
                return new StyleValue<Overflow>((Overflow)effectiveStyle.overflow.value, effectiveStyle.overflow.specificity);
            }
            set
            {
                if (effectiveStyle.overflow.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.overflow = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.Overflow = (CSSOverflow)value.value;
            }
        }

        StyleValue<float> IStyle.positionLeft
        {
            get { return effectiveStyle.positionLeft; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.positionLeft.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.positionLeft = value;
                cssNode.SetPosition(CSSEdge.Left, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.positionTop
        {
            get { return effectiveStyle.positionTop; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.positionTop.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.positionTop = value;
                cssNode.SetPosition(CSSEdge.Top, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.positionRight
        {
            get { return effectiveStyle.positionRight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.positionRight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.positionRight = value;
                cssNode.SetPosition(CSSEdge.Right, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.positionBottom
        {
            get { return effectiveStyle.positionBottom; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.positionBottom.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.positionBottom = value;
                cssNode.SetPosition(CSSEdge.Bottom, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.marginLeft
        {
            get { return effectiveStyle.marginLeft; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.marginLeft.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.marginLeft = value;
                cssNode.SetMargin(CSSEdge.Left, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.marginTop
        {
            get { return effectiveStyle.marginTop; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.marginTop.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.marginTop = value;
                cssNode.SetMargin(CSSEdge.Top, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.marginRight
        {
            get { return effectiveStyle.marginRight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.marginRight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.marginRight = value;
                cssNode.SetMargin(CSSEdge.Right, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.marginBottom
        {
            get { return effectiveStyle.marginBottom; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.marginBottom.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.marginBottom = value;
                cssNode.SetMargin(CSSEdge.Bottom, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderLeft
        {
            get { return effectiveStyle.borderLeft; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderLeft.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderLeft = value;
                cssNode.SetBorder(CSSEdge.Left, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderTop
        {
            get { return effectiveStyle.borderTop; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderTop.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderTop = value;
                cssNode.SetBorder(CSSEdge.Top, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderRight
        {
            get { return effectiveStyle.borderRight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderRight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderRight = value;
                cssNode.SetBorder(CSSEdge.Right, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderBottom
        {
            get { return effectiveStyle.borderBottom; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderBottom.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderBottom = value;
                cssNode.SetBorder(CSSEdge.Bottom, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderLeftWidth
        {
            get { return effectiveStyle.borderLeftWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderLeftWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderLeftWidth = value;
                cssNode.SetBorder(CSSEdge.Left, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderTopWidth
        {
            get { return effectiveStyle.borderTopWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderTopWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderTopWidth = value;
                cssNode.SetBorder(CSSEdge.Top, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderRightWidth
        {
            get { return effectiveStyle.borderRightWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderRightWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderRightWidth = value;
                cssNode.SetBorder(CSSEdge.Right, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.borderBottomWidth
        {
            get { return effectiveStyle.borderBottomWidth; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderBottomWidth.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.borderBottomWidth = value;
                cssNode.SetBorder(CSSEdge.Bottom, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.paddingLeft
        {
            get { return effectiveStyle.paddingLeft; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.paddingLeft.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.paddingLeft = value;
                cssNode.SetPadding(CSSEdge.Left, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.paddingTop
        {
            get { return effectiveStyle.paddingTop; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.paddingTop.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.paddingTop = value;
                cssNode.SetPadding(CSSEdge.Top, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.paddingRight
        {
            get { return effectiveStyle.paddingRight; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.paddingRight.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.paddingRight = value;
                cssNode.SetPadding(CSSEdge.Right, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<float> IStyle.paddingBottom
        {
            get { return effectiveStyle.paddingBottom; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.paddingBottom.value, value.value))
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.paddingBottom = value;
                cssNode.SetPadding(CSSEdge.Bottom, value.GetSpecifiedValueOrDefault(float.NaN));
            }
        }

        StyleValue<PositionType> IStyle.positionType
        {
            get
            {
                return new StyleValue<PositionType>((PositionType)effectiveStyle.positionType.value, effectiveStyle.positionType.specificity);
            }
            set
            {
                if (effectiveStyle.positionType.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.positionType = new StyleValue<int>((int)value.value, value.specificity);
                PositionType posType = (PositionType)value.value;
                switch (posType)
                {
                    case PositionType.Absolute:
                    case PositionType.Manual:
                        cssNode.PositionType = CSSPositionType.Absolute;
                        break;
                    case PositionType.Relative:
                        cssNode.PositionType = CSSPositionType.Relative;
                        break;
                }
            }
        }

        StyleValue<Align> IStyle.alignSelf
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignSelf.value, effectiveStyle.alignSelf.specificity); }
            set
            {
                if (effectiveStyle.alignSelf.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.alignSelf = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.AlignSelf = (CSSAlign)value.value;
            }
        }

        StyleValue<TextAnchor> IStyle.textAlignment
        {
            get { return new StyleValue<TextAnchor>((TextAnchor)effectiveStyle.textAlignment.value, effectiveStyle.textAlignment.specificity); }
            set
            {
                if (effectiveStyle.textAlignment.value != (int)value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.textAlignment = new StyleValue<int>((int)value.value, value.specificity);
            }
        }

        StyleValue<FontStyle> IStyle.fontStyle
        {
            get { return new StyleValue<FontStyle>((FontStyle)effectiveStyle.fontStyle.value, effectiveStyle.fontStyle.specificity); }
            set
            {
                if (effectiveStyle.fontStyle.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.fontStyle = new StyleValue<int>((int)value.value, value.specificity);
            }
        }

        StyleValue<TextClipping> IStyle.textClipping
        {
            get { return new StyleValue<TextClipping>((TextClipping)effectiveStyle.textClipping.value, effectiveStyle.textClipping.specificity); }
            set
            {
                if (effectiveStyle.textClipping.value != (int)value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.textClipping = new StyleValue<int>((int)value.value, value.specificity);
            }
        }

        StyleValue<Font> IStyle.font
        {
            get { return effectiveStyle.font; }
            set
            {
                if (effectiveStyle.font.value != value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.font = value;
            }
        }

        StyleValue<int> IStyle.fontSize
        {
            get { return effectiveStyle.fontSize; }
            set
            {
                if (effectiveStyle.fontSize.value != value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.fontSize = value;
            }
        }

        StyleValue<bool> IStyle.wordWrap
        {
            get { return effectiveStyle.wordWrap; }
            set
            {
                if (effectiveStyle.wordWrap.value != value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.wordWrap = value;
            }
        }

        StyleValue<Color> IStyle.textColor
        {
            get { return effectiveStyle.textColor; }
            set
            {
                if (effectiveStyle.textColor.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.textColor = value;
            }
        }

        StyleValue<FlexDirection> IStyle.flexDirection
        {
            get { return new StyleValue<FlexDirection>((FlexDirection)effectiveStyle.flexDirection.value, effectiveStyle.flexDirection.specificity); }
            set
            {
                if (effectiveStyle.flexDirection.value != (int)value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.flexDirection = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.FlexDirection = (CSSFlexDirection)value.value;
            }
        }

        StyleValue<Color> IStyle.backgroundColor
        {
            get { return effectiveStyle.backgroundColor; }
            set
            {
                if (effectiveStyle.backgroundColor.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.backgroundColor = value;
            }
        }

        StyleValue<Color> IStyle.borderColor
        {
            get { return effectiveStyle.borderColor; }
            set
            {
                if (effectiveStyle.borderColor.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.borderColor = value;
            }
        }

        StyleValue<Texture2D> IStyle.backgroundImage
        {
            get { return effectiveStyle.backgroundImage; }
            set
            {
                if (effectiveStyle.backgroundImage.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.backgroundImage = value;
            }
        }

        StyleValue<ScaleMode> IStyle.backgroundSize
        {
            get { return new StyleValue<ScaleMode>((ScaleMode)effectiveStyle.backgroundSize.value, effectiveStyle.backgroundSize.specificity); }
            set
            {
                if (effectiveStyle.backgroundSize.value != (int)value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.backgroundSize = new StyleValue<int>((int)value.value, value.specificity);
            }
        }

        StyleValue<Align> IStyle.alignItems
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignItems.value, effectiveStyle.alignItems.specificity); }
            set
            {
                if (effectiveStyle.alignItems.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.alignItems = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.AlignItems = (CSSAlign)value.GetSpecifiedValueOrDefault(VisualElement.DefaultAlignItems);
            }
        }

        StyleValue<Align> IStyle.alignContent
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignContent.value, effectiveStyle.alignContent.specificity); }
            set
            {
                if (effectiveStyle.alignContent.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.alignContent = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.AlignContent = (CSSAlign)value.GetSpecifiedValueOrDefault(VisualElement.DefaultAlignContent);
            }
        }

        StyleValue<Justify> IStyle.justifyContent
        {
            get { return new StyleValue<Justify>((Justify)effectiveStyle.justifyContent.value, effectiveStyle.justifyContent.specificity); }
            set
            {
                if (effectiveStyle.justifyContent.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.justifyContent = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.JustifyContent = (CSSJustify)value.value;
            }
        }

        StyleValue<Wrap> IStyle.flexWrap
        {
            get { return new StyleValue<Wrap>((Wrap)effectiveStyle.flexWrap.value, effectiveStyle.flexWrap.specificity); }
            set
            {
                if (effectiveStyle.flexWrap.value != (int)value.value)
                {
                    Dirty(ChangeType.Layout);
                }
                inlineStyle.flexWrap = new StyleValue<int>((int)value.value, value.specificity);
                cssNode.Wrap = (CSSWrap)value.value;
            }
        }

        StyleValue<float> IStyle.borderRadius
        {
            get { return effectiveStyle.borderRadius; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.borderRadius.value, value.value))
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.borderRadius = value;
            }
        }

        StyleValue<int> IStyle.sliceLeft
        {
            get { return effectiveStyle.sliceLeft; }
            set
            {
                if (effectiveStyle.sliceLeft.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.sliceLeft = value;
            }
        }

        StyleValue<int> IStyle.sliceTop
        {
            get { return effectiveStyle.sliceTop; }
            set
            {
                if (effectiveStyle.sliceTop.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.sliceTop = value;
            }
        }

        StyleValue<int> IStyle.sliceRight
        {
            get { return effectiveStyle.sliceRight; }
            set
            {
                if (effectiveStyle.sliceRight.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.sliceRight = value;
            }
        }

        StyleValue<int> IStyle.sliceBottom
        {
            get { return effectiveStyle.sliceBottom; }
            set
            {
                if (effectiveStyle.sliceBottom.value != value.value)
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.sliceBottom = value;
            }
        }

        StyleValue<float> IStyle.opacity
        {
            get { return effectiveStyle.opacity; }
            set
            {
                if (!Mathf.Approximately(effectiveStyle.opacity.value, value.value))
                {
                    Dirty(ChangeType.Repaint);
                }
                inlineStyle.opacity = value;
            }
        }
    }
}
