// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public partial class VisualElement : IResolvedStyle
    {
        private InlineStyleAccess m_InlineStyleAccess;
        public IStyle style
        {
            get
            {
                if (m_InlineStyleAccess == null)
                    m_InlineStyleAccess = new InlineStyleAccess(this);

                return m_InlineStyleAccess;
            }
        }

        public ICustomStyle customStyle
        {
            get { return specifiedStyle; }
        }

        // this allows us to not expose all styles accessors directly on VisualElement class
        // but still let the VisualElement be in control of proxying inline styles modifications
        // we avoid an extra allocation when returning that property and it's nice from an API perspective
        public IResolvedStyle resolvedStyle
        {
            get { return this; }
        }

        float IResolvedStyle.width => yogaNode.LayoutWidth;
        float IResolvedStyle.height => yogaNode.LayoutHeight;
        StyleFloat IResolvedStyle.maxWidth => computedStyle.maxWidth.ToStyleFloat();
        StyleFloat IResolvedStyle.maxHeight => computedStyle.maxHeight.ToStyleFloat();
        StyleFloat IResolvedStyle.minWidth => computedStyle.minWidth.ToStyleFloat();
        StyleFloat IResolvedStyle.minHeight => computedStyle.minHeight.ToStyleFloat();
        StyleFloat IResolvedStyle.flexBasis => computedStyle.flexBasis.ToStyleFloat();
        float IResolvedStyle.flexGrow => computedStyle.flexGrow.value;
        float IResolvedStyle.flexShrink => computedStyle.flexShrink.value;
        FlexDirection IResolvedStyle.flexDirection => computedStyle.flexDirection.value;
        Wrap IResolvedStyle.flexWrap => computedStyle.flexWrap.value;
        float IResolvedStyle.left => yogaNode.LayoutX;
        float IResolvedStyle.top => yogaNode.LayoutY;
        float IResolvedStyle.right => yogaNode.LayoutRight;
        float IResolvedStyle.bottom => yogaNode.LayoutBottom;
        float IResolvedStyle.marginLeft => yogaNode.LayoutMarginLeft;
        float IResolvedStyle.marginTop => yogaNode.LayoutMarginTop;
        float IResolvedStyle.marginRight => yogaNode.LayoutMarginRight;
        float IResolvedStyle.marginBottom => yogaNode.LayoutMarginBottom;
        float IResolvedStyle.paddingLeft => yogaNode.LayoutPaddingLeft;
        float IResolvedStyle.paddingTop => yogaNode.LayoutPaddingTop;
        float IResolvedStyle.paddingRight => yogaNode.LayoutPaddingRight;
        float IResolvedStyle.paddingBottom => yogaNode.LayoutPaddingBottom;
        Position IResolvedStyle.position => computedStyle.position.value;
        Align IResolvedStyle.alignSelf => computedStyle.alignSelf.value;
        TextAnchor IResolvedStyle.unityTextAlign => computedStyle.unityTextAlign.value;
        FontStyle IResolvedStyle.unityFontStyleAndWeight => computedStyle.unityFontStyleAndWeight.value;
        float IResolvedStyle.fontSize => computedStyle.fontSize.value.value;
        WhiteSpace IResolvedStyle.whiteSpace => computedStyle.whiteSpace.value;
        Color IResolvedStyle.color => computedStyle.color.value;
        Color IResolvedStyle.backgroundColor => computedStyle.backgroundColor.value;
        Color IResolvedStyle.borderColor => computedStyle.borderColor.value;
        Font IResolvedStyle.unityFont => computedStyle.unityFont.value;
        ScaleMode IResolvedStyle.unityBackgroundScaleMode => computedStyle.unityBackgroundScaleMode.value;
        Color IResolvedStyle.unityBackgroundImageTintColor => computedStyle.unityBackgroundImageTintColor.value;
        Align IResolvedStyle.alignItems => computedStyle.alignItems.value;
        Align IResolvedStyle.alignContent => computedStyle.alignContent.value;
        Justify IResolvedStyle.justifyContent => computedStyle.justifyContent.value;
        float IResolvedStyle.borderLeftWidth => computedStyle.borderLeftWidth.value;
        float IResolvedStyle.borderRightWidth => computedStyle.borderRightWidth.value;
        float IResolvedStyle.borderTopWidth => computedStyle.borderTopWidth.value;
        float IResolvedStyle.borderBottomWidth => computedStyle.borderBottomWidth.value;
        float IResolvedStyle.borderTopLeftRadius => computedStyle.borderTopLeftRadius.value.value;
        float IResolvedStyle.borderTopRightRadius => computedStyle.borderTopRightRadius.value.value;
        float IResolvedStyle.borderBottomLeftRadius => computedStyle.borderBottomLeftRadius.value.value;
        float IResolvedStyle.borderBottomRightRadius => computedStyle.borderBottomRightRadius.value.value;
        int IResolvedStyle.unitySliceLeft => computedStyle.unitySliceLeft.value;
        int IResolvedStyle.unitySliceTop => computedStyle.unitySliceTop.value;
        int IResolvedStyle.unitySliceRight => computedStyle.unitySliceRight.value;
        int IResolvedStyle.unitySliceBottom => computedStyle.unitySliceBottom.value;
        float IResolvedStyle.opacity => computedStyle.opacity.value;
        Visibility IResolvedStyle.visibility => computedStyle.visibility.value;
        DisplayStyle IResolvedStyle.display => computedStyle.display.value;

        public VisualElementStyleSheetSet styleSheets => new VisualElementStyleSheetSet(this);

        internal List<StyleSheet> styleSheetList;

        internal void AddStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet)) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                return;
            }

            styleSheets.Add(sheetAsset);
        }

        internal bool HasStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet)) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                return false;
            }

            return styleSheets.Contains(sheetAsset);
        }

        internal void RemoveStyleSheetPath(string sheetPath)
        {
            StyleSheet sheetAsset = Panel.LoadResource(sheetPath, typeof(StyleSheet)) as StyleSheet;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", sheetPath));
                return;
            }
            styleSheets.Remove(sheetAsset);
        }
    }
}
