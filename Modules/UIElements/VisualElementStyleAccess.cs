// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.CSSLayout;
using UnityEngine.StyleSheets;

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
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.width, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.Width = value.value;
                }
            }
        }

        StyleValue<float> IStyle.height
        {
            get { return effectiveStyle.height; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.height, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.Height = value.value;
                }
            }
        }

        StyleValue<float> IStyle.maxWidth
        {
            get { return effectiveStyle.maxWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.maxWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.MaxWidth = value.value;
                }
            }
        }

        StyleValue<float> IStyle.maxHeight
        {
            get { return effectiveStyle.maxHeight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.maxHeight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.MaxHeight = value.value;
                }
            }
        }

        StyleValue<float> IStyle.minWidth
        {
            get { return effectiveStyle.minWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.minWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.MinWidth = value.value;
                }
            }
        }

        StyleValue<float> IStyle.minHeight
        {
            get { return effectiveStyle.minHeight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.minHeight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.MinHeight = value.value;
                }
            }
        }

        StyleValue<float> IStyle.flex
        {
            get { return effectiveStyle.flex; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flex, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.Flex = value.value;
                }
            }
        }


        StyleValue<float> IStyle.flexBasis
        {
            get { return effectiveStyle.flexBasis; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flexBasis, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.FlexBasis = value.value;
                }
            }
        }

        StyleValue<float> IStyle.flexGrow
        {
            get { return effectiveStyle.flexGrow; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flexGrow, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.FlexGrow = value.value;
                }
            }
        }

        StyleValue<float> IStyle.flexShrink
        {
            get { return effectiveStyle.flexShrink; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flexShrink, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.FlexShrink = value.value;
                }
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
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.overflow, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.Overflow = (CSSOverflow)value.value;
                }
            }
        }

        StyleValue<float> IStyle.positionLeft
        {
            get { return effectiveStyle.positionLeft; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.positionLeft, value))
                {
                    ChangeType saveChangedNeeded = changesNeeded;

                    Dirty(ChangeType.Layout);

                    if ((saveChangedNeeded & ChangeType.Repaint) == 0)
                        ClearDirty(ChangeType.Repaint);

                    cssNode.SetPosition(CSSEdge.Left, value.value);
                }
            }
        }

        StyleValue<float> IStyle.positionTop
        {
            get { return effectiveStyle.positionTop; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.positionTop, value))
                {
                    ChangeType saveChangedNeeded = changesNeeded;

                    Dirty(ChangeType.Layout);

                    if ((saveChangedNeeded & ChangeType.Repaint) == 0)
                        ClearDirty(ChangeType.Repaint);

                    cssNode.SetPosition(CSSEdge.Top, value.value);
                }
            }
        }

        StyleValue<float> IStyle.positionRight
        {
            get { return effectiveStyle.positionRight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.positionRight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPosition(CSSEdge.Right, value.value);
                }
            }
        }

        StyleValue<float> IStyle.positionBottom
        {
            get { return effectiveStyle.positionBottom; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.positionBottom, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPosition(CSSEdge.Bottom, value.value);
                }
            }
        }

        StyleValue<float> IStyle.marginLeft
        {
            get { return effectiveStyle.marginLeft; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.marginLeft, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetMargin(CSSEdge.Left, value.value);
                }
            }
        }

        StyleValue<float> IStyle.marginTop
        {
            get { return effectiveStyle.marginTop; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.marginTop, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetMargin(CSSEdge.Top, value.value);
                }
            }
        }

        StyleValue<float> IStyle.marginRight
        {
            get { return effectiveStyle.marginRight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.marginRight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetMargin(CSSEdge.Right, value.value);
                }
            }
        }

        StyleValue<float> IStyle.marginBottom
        {
            get { return effectiveStyle.marginBottom; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.marginBottom, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetMargin(CSSEdge.Bottom, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderLeft
        {
            get { return effectiveStyle.borderLeft; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderLeft, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Left, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderTop
        {
            get { return effectiveStyle.borderTop; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderTop, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Top, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderRight
        {
            get { return effectiveStyle.borderRight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderRight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Right, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderBottom
        {
            get { return effectiveStyle.borderBottom; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderBottom, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Bottom, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderLeftWidth
        {
            get { return effectiveStyle.borderLeftWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderLeftWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Left, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderTopWidth
        {
            get { return effectiveStyle.borderTopWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderTopWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Top, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderRightWidth
        {
            get { return effectiveStyle.borderRightWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderRightWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Right, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderBottomWidth
        {
            get { return effectiveStyle.borderBottomWidth; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderBottomWidth, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetBorder(CSSEdge.Bottom, value.value);
                }
            }
        }

        StyleValue<float> IStyle.borderRadius
        {
            get { return style.borderTopLeftRadius; }
            set
            {
                style.borderTopLeftRadius = value;
                style.borderTopRightRadius = value;
                style.borderBottomLeftRadius = value;
                style.borderBottomRightRadius = value;
            }
        }

        StyleValue<float> IStyle.borderTopLeftRadius
        {
            get { return effectiveStyle.borderTopLeftRadius; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderTopLeftRadius, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<float> IStyle.borderTopRightRadius
        {
            get { return effectiveStyle.borderTopRightRadius; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderTopRightRadius, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<float> IStyle.borderBottomRightRadius
        {
            get { return effectiveStyle.borderBottomRightRadius; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderBottomRightRadius, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<float> IStyle.borderBottomLeftRadius
        {
            get { return effectiveStyle.borderBottomLeftRadius; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderBottomLeftRadius, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<float> IStyle.paddingLeft
        {
            get { return effectiveStyle.paddingLeft; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.paddingLeft, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPadding(CSSEdge.Left, value.value);
                }
            }
        }

        StyleValue<float> IStyle.paddingTop
        {
            get { return effectiveStyle.paddingTop; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.paddingTop, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPadding(CSSEdge.Top, value.value);
                }
            }
        }

        StyleValue<float> IStyle.paddingRight
        {
            get { return effectiveStyle.paddingRight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.paddingRight, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPadding(CSSEdge.Right, value.value);
                }
            }
        }

        StyleValue<float> IStyle.paddingBottom
        {
            get { return effectiveStyle.paddingBottom; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.paddingBottom, value))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.SetPadding(CSSEdge.Bottom, value.value);
                }
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
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.positionType, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
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
        }

        StyleValue<Align> IStyle.alignSelf
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignSelf.value, effectiveStyle.alignSelf.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.alignSelf, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.AlignSelf = (CSSAlign)value.value;
                }
            }
        }

        StyleValue<TextAnchor> IStyle.textAlignment
        {
            get { return new StyleValue<TextAnchor>((TextAnchor)effectiveStyle.textAlignment.value, effectiveStyle.textAlignment.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.textAlignment, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<FontStyle> IStyle.fontStyle
        {
            get { return new StyleValue<FontStyle>((FontStyle)effectiveStyle.fontStyle.value, effectiveStyle.fontStyle.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.fontStyle, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                }
            }
        }

        StyleValue<TextClipping> IStyle.textClipping
        {
            get { return new StyleValue<TextClipping>((TextClipping)effectiveStyle.textClipping.value, effectiveStyle.textClipping.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.textClipping, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<Font> IStyle.font
        {
            get { return effectiveStyle.font; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.font, value))
                {
                    Dirty(ChangeType.Layout);
                }
            }
        }

        StyleValue<int> IStyle.fontSize
        {
            get { return effectiveStyle.fontSize; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.fontSize, value))
                {
                    Dirty(ChangeType.Layout);
                }
            }
        }

        StyleValue<bool> IStyle.wordWrap
        {
            get { return effectiveStyle.wordWrap; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.wordWrap, value))
                {
                    Dirty(ChangeType.Layout);
                }
            }
        }

        StyleValue<Color> IStyle.textColor
        {
            get { return effectiveStyle.textColor; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.textColor, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<FlexDirection> IStyle.flexDirection
        {
            get { return new StyleValue<FlexDirection>((FlexDirection)effectiveStyle.flexDirection.value, effectiveStyle.flexDirection.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flexDirection, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Repaint);
                    cssNode.FlexDirection = (CSSFlexDirection)value.value;
                }
            }
        }

        StyleValue<Color> IStyle.backgroundColor
        {
            get { return effectiveStyle.backgroundColor; }
            set
            {
                if (value.specificity == 0 && value == default(Color))
                {
                    inlineStyle.backgroundColor = sharedStyle.backgroundColor;
                    Dirty(ChangeType.Repaint);
                }
                else if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.backgroundColor, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<Color> IStyle.borderColor
        {
            get { return effectiveStyle.borderColor; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.borderColor, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<Texture2D> IStyle.backgroundImage
        {
            get { return effectiveStyle.backgroundImage; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.backgroundImage, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<ScaleMode> IStyle.backgroundSize
        {
            get { return new StyleValue<ScaleMode>((ScaleMode)effectiveStyle.backgroundSize.value, effectiveStyle.backgroundSize.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.backgroundSize, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<Align> IStyle.alignItems
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignItems.value, effectiveStyle.alignItems.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.alignItems, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.AlignItems = (CSSAlign)value.value;
                }
            }
        }

        StyleValue<Align> IStyle.alignContent
        {
            get { return new StyleValue<Align>((Align)effectiveStyle.alignContent.value, effectiveStyle.alignContent.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.alignContent, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.AlignContent = (CSSAlign)value.value;
                }
            }
        }

        StyleValue<Justify> IStyle.justifyContent
        {
            get { return new StyleValue<Justify>((Justify)effectiveStyle.justifyContent.value, effectiveStyle.justifyContent.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.justifyContent, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.JustifyContent = (CSSJustify)value.value;
                }
            }
        }

        StyleValue<Wrap> IStyle.flexWrap
        {
            get { return new StyleValue<Wrap>((Wrap)effectiveStyle.flexWrap.value, effectiveStyle.flexWrap.specificity); }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.flexWrap, new StyleValue<int>((int)value.value, value.specificity)))
                {
                    Dirty(ChangeType.Layout);
                    cssNode.Wrap = (CSSWrap)value.value;
                }
            }
        }

        StyleValue<int> IStyle.sliceLeft
        {
            get { return effectiveStyle.sliceLeft; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.sliceLeft, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<int> IStyle.sliceTop
        {
            get { return effectiveStyle.sliceTop; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.sliceTop, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<int> IStyle.sliceRight
        {
            get { return effectiveStyle.sliceRight; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.sliceRight, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<int> IStyle.sliceBottom
        {
            get { return effectiveStyle.sliceBottom; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.sliceBottom, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<float> IStyle.opacity
        {
            get { return effectiveStyle.opacity; }
            set
            {
                if (StyleValueUtils.ApplyAndCompare(ref inlineStyle.opacity, value))
                {
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        StyleValue<CursorStyle> IStyle.cursor
        {
            get { return effectiveStyle.cursor; }
            set
            {
                StyleValueUtils.ApplyAndCompare(ref inlineStyle.cursor, value);
            }
        }

        private List<StyleSheet> m_StyleSheets;


        internal IList<StyleSheet> styleSheets
        {
            get
            {
                if (m_StyleSheets == null && m_StyleSheetPaths != null)
                {
                    LoadStyleSheetsFromPaths();
                }
                return m_StyleSheets;
            }
        }

        private List<string> m_StyleSheetPaths;

        public void AddStyleSheetPath(string sheetPath)
        {
            if (m_StyleSheetPaths == null)
            {
                m_StyleSheetPaths = new List<string>();
            }
            m_StyleSheetPaths.Add(sheetPath);
            //will trigger a reload on next access
            m_StyleSheets = null;
            Dirty(ChangeType.Styles);
        }

        public void RemoveStyleSheetPath(string sheetPath)
        {
            if (m_StyleSheetPaths == null)
            {
                Debug.LogWarning("Attempting to remove from null style sheet path list");
                return;
            }
            m_StyleSheetPaths.Remove(sheetPath);
            //will trigger a reload on next access
            m_StyleSheets = null;
            Dirty(ChangeType.Styles);
        }

        public bool HasStyleSheetPath(string sheetPath)
        {
            return m_StyleSheetPaths != null && m_StyleSheetPaths.Contains(sheetPath);
        }

        internal void ReplaceStyleSheetPath(string oldSheetPath, string newSheetPath)
        {
            if (m_StyleSheetPaths == null)
            {
                Debug.LogWarning("Attempting to replace a style from null style sheet path list");
                return;
            }

            int index = m_StyleSheetPaths.IndexOf(oldSheetPath);
            if (index >= 0)
            {
                m_StyleSheetPaths[index] = newSheetPath;
                //will trigger a reload on next access
                m_StyleSheets = null;
                Dirty(ChangeType.Styles);
            }
        }

        internal void LoadStyleSheetsFromPaths()
        {
            if (m_StyleSheetPaths == null || elementPanel == null)
            {
                return;
            }

            m_StyleSheets = new List<StyleSheet>();
            foreach (var styleSheetPath in m_StyleSheetPaths)
            {
                StyleSheet sheetAsset = Panel.loadResourceFunc(styleSheetPath, typeof(StyleSheet)) as StyleSheet;

                if (sheetAsset != null)
                {
                    // Every time we load a new style sheet, we cache some data on them
                    for (int i = 0, count = sheetAsset.complexSelectors.Length; i < count; i++)
                    {
                        sheetAsset.complexSelectors[i].CachePseudoStateMasks();
                    }
                    m_StyleSheets.Add(sheetAsset);
                }
                else
                {
                    Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", styleSheetPath));
                }
            }
        }
    }
}
