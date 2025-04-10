// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/******************************************************************************/
//
//                             DO NOT MODIFY
//          This file has been generated by the UIElementsGenerator tool
//              See ResolvedStyleAccessCsGenerator class for details
//
/******************************************************************************/
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal partial class ResolvedStyleAccess : IResolvedStyle
    {
        public Align alignContent => ve.computedStyle.alignContent;
        public Align alignItems => ve.computedStyle.alignItems;
        public Align alignSelf => ve.computedStyle.alignSelf;
        public Color backgroundColor => ve.computedStyle.backgroundColor;
        public Background backgroundImage => ve.computedStyle.backgroundImage;
        public BackgroundPosition backgroundPositionX => ve.computedStyle.backgroundPositionX;
        public BackgroundPosition backgroundPositionY => ve.computedStyle.backgroundPositionY;
        public BackgroundRepeat backgroundRepeat => ve.computedStyle.backgroundRepeat;
        public BackgroundSize backgroundSize => ve.computedStyle.backgroundSize;
        public Color borderBottomColor => ve.computedStyle.borderBottomColor;
        public float borderBottomLeftRadius => ve.computedStyle.borderBottomLeftRadius.value;
        public float borderBottomRightRadius => ve.computedStyle.borderBottomRightRadius.value;
        public float borderBottomWidth => ve.layoutNode.LayoutBorderBottom;
        public Color borderLeftColor => ve.computedStyle.borderLeftColor;
        public float borderLeftWidth => ve.layoutNode.LayoutBorderLeft;
        public Color borderRightColor => ve.computedStyle.borderRightColor;
        public float borderRightWidth => ve.layoutNode.LayoutBorderRight;
        public Color borderTopColor => ve.computedStyle.borderTopColor;
        public float borderTopLeftRadius => ve.computedStyle.borderTopLeftRadius.value;
        public float borderTopRightRadius => ve.computedStyle.borderTopRightRadius.value;
        public float borderTopWidth => ve.layoutNode.LayoutBorderTop;
        public float bottom => ve.layoutNode.LayoutBottom;
        public Color color => ve.computedStyle.color;
        public DisplayStyle display => ve.computedStyle.display;
        public StyleFloat flexBasis => new StyleFloat(ve.layoutNode.ComputedFlexBasis);
        public FlexDirection flexDirection => ve.computedStyle.flexDirection;
        public float flexGrow => ve.computedStyle.flexGrow;
        public float flexShrink => ve.computedStyle.flexShrink;
        public Wrap flexWrap => ve.computedStyle.flexWrap;
        public float fontSize => ve.computedStyle.fontSize.value;
        public float height => ve.layoutNode.LayoutHeight;
        public Justify justifyContent => ve.computedStyle.justifyContent;
        public float left => ve.layoutNode.LayoutX;
        public float letterSpacing => ve.computedStyle.letterSpacing.value;
        public float marginBottom => ve.layoutNode.LayoutMarginBottom;
        public float marginLeft => ve.layoutNode.LayoutMarginLeft;
        public float marginRight => ve.layoutNode.LayoutMarginRight;
        public float marginTop => ve.layoutNode.LayoutMarginTop;
        public StyleFloat maxHeight => ve.ResolveLengthValue(ve.computedStyle.maxHeight, false);
        public StyleFloat maxWidth => ve.ResolveLengthValue(ve.computedStyle.maxWidth, true);
        public StyleFloat minHeight => ve.ResolveLengthValue(ve.computedStyle.minHeight, false);
        public StyleFloat minWidth => ve.ResolveLengthValue(ve.computedStyle.minWidth, true);
        public float opacity => ve.computedStyle.opacity;
        public float paddingBottom => ve.layoutNode.LayoutPaddingBottom;
        public float paddingLeft => ve.layoutNode.LayoutPaddingLeft;
        public float paddingRight => ve.layoutNode.LayoutPaddingRight;
        public float paddingTop => ve.layoutNode.LayoutPaddingTop;
        public Position position => ve.computedStyle.position;
        public float right => ve.layoutNode.LayoutRight;
        public Rotate rotate => ve.computedStyle.rotate;
        public Scale scale => ve.computedStyle.scale;
        public TextOverflow textOverflow => ve.computedStyle.textOverflow;
        public float top => ve.layoutNode.LayoutY;
        public Vector3 transformOrigin => ve.ResolveTransformOrigin();
        public IEnumerable<TimeValue> transitionDelay => ve.computedStyle.transitionDelay;
        public IEnumerable<TimeValue> transitionDuration => ve.computedStyle.transitionDuration;
        public IEnumerable<StylePropertyName> transitionProperty => ve.computedStyle.transitionProperty;
        public IEnumerable<EasingFunction> transitionTimingFunction => ve.computedStyle.transitionTimingFunction;
        public Vector3 translate => ve.ResolveTranslate();
        public Color unityBackgroundImageTintColor => ve.computedStyle.unityBackgroundImageTintColor;
        public EditorTextRenderingMode unityEditorTextRenderingMode => ve.computedStyle.unityEditorTextRenderingMode;
        public Font unityFont => ve.computedStyle.unityFont;
        public FontDefinition unityFontDefinition => ve.computedStyle.unityFontDefinition;
        public FontStyle unityFontStyleAndWeight => ve.computedStyle.unityFontStyleAndWeight;
        public float unityParagraphSpacing => ve.computedStyle.unityParagraphSpacing.value;
        public int unitySliceBottom => ve.computedStyle.unitySliceBottom;
        public int unitySliceLeft => ve.computedStyle.unitySliceLeft;
        public int unitySliceRight => ve.computedStyle.unitySliceRight;
        public float unitySliceScale => ve.computedStyle.unitySliceScale;
        public int unitySliceTop => ve.computedStyle.unitySliceTop;
        public SliceType unitySliceType => ve.computedStyle.unitySliceType;
        public TextAnchor unityTextAlign => ve.computedStyle.unityTextAlign;
        public TextGeneratorType unityTextGenerator => ve.computedStyle.unityTextGenerator;
        public Color unityTextOutlineColor => ve.computedStyle.unityTextOutlineColor;
        public float unityTextOutlineWidth => ve.computedStyle.unityTextOutlineWidth;
        public TextOverflowPosition unityTextOverflowPosition => ve.computedStyle.unityTextOverflowPosition;
        public Visibility visibility => ve.computedStyle.visibility;
        public WhiteSpace whiteSpace => ve.computedStyle.whiteSpace;
        public float width => ve.layoutNode.LayoutWidth;
        public float wordSpacing => ve.computedStyle.wordSpacing.value;
    }

    public partial class VisualElement : IResolvedStyle
    {
        Align IResolvedStyle.alignContent => resolvedStyle.alignContent;
        Align IResolvedStyle.alignItems => resolvedStyle.alignItems;
        Align IResolvedStyle.alignSelf => resolvedStyle.alignSelf;
        Color IResolvedStyle.backgroundColor => resolvedStyle.backgroundColor;
        Background IResolvedStyle.backgroundImage => resolvedStyle.backgroundImage;
        BackgroundPosition IResolvedStyle.backgroundPositionX => resolvedStyle.backgroundPositionX;
        BackgroundPosition IResolvedStyle.backgroundPositionY => resolvedStyle.backgroundPositionY;
        BackgroundRepeat IResolvedStyle.backgroundRepeat => resolvedStyle.backgroundRepeat;
        BackgroundSize IResolvedStyle.backgroundSize => resolvedStyle.backgroundSize;
        Color IResolvedStyle.borderBottomColor => resolvedStyle.borderBottomColor;
        float IResolvedStyle.borderBottomLeftRadius => resolvedStyle.borderBottomLeftRadius;
        float IResolvedStyle.borderBottomRightRadius => resolvedStyle.borderBottomRightRadius;
        float IResolvedStyle.borderBottomWidth => resolvedStyle.borderBottomWidth;
        Color IResolvedStyle.borderLeftColor => resolvedStyle.borderLeftColor;
        float IResolvedStyle.borderLeftWidth => resolvedStyle.borderLeftWidth;
        Color IResolvedStyle.borderRightColor => resolvedStyle.borderRightColor;
        float IResolvedStyle.borderRightWidth => resolvedStyle.borderRightWidth;
        Color IResolvedStyle.borderTopColor => resolvedStyle.borderTopColor;
        float IResolvedStyle.borderTopLeftRadius => resolvedStyle.borderTopLeftRadius;
        float IResolvedStyle.borderTopRightRadius => resolvedStyle.borderTopRightRadius;
        float IResolvedStyle.borderTopWidth => resolvedStyle.borderTopWidth;
        float IResolvedStyle.bottom => resolvedStyle.bottom;
        Color IResolvedStyle.color => resolvedStyle.color;
        DisplayStyle IResolvedStyle.display => resolvedStyle.display;
        StyleFloat IResolvedStyle.flexBasis => resolvedStyle.flexBasis;
        FlexDirection IResolvedStyle.flexDirection => resolvedStyle.flexDirection;
        float IResolvedStyle.flexGrow => resolvedStyle.flexGrow;
        float IResolvedStyle.flexShrink => resolvedStyle.flexShrink;
        Wrap IResolvedStyle.flexWrap => resolvedStyle.flexWrap;
        float IResolvedStyle.fontSize => resolvedStyle.fontSize;
        float IResolvedStyle.height => resolvedStyle.height;
        Justify IResolvedStyle.justifyContent => resolvedStyle.justifyContent;
        float IResolvedStyle.left => resolvedStyle.left;
        float IResolvedStyle.letterSpacing => resolvedStyle.letterSpacing;
        float IResolvedStyle.marginBottom => resolvedStyle.marginBottom;
        float IResolvedStyle.marginLeft => resolvedStyle.marginLeft;
        float IResolvedStyle.marginRight => resolvedStyle.marginRight;
        float IResolvedStyle.marginTop => resolvedStyle.marginTop;
        StyleFloat IResolvedStyle.maxHeight => resolvedStyle.maxHeight;
        StyleFloat IResolvedStyle.maxWidth => resolvedStyle.maxWidth;
        StyleFloat IResolvedStyle.minHeight => resolvedStyle.minHeight;
        StyleFloat IResolvedStyle.minWidth => resolvedStyle.minWidth;
        float IResolvedStyle.opacity => resolvedStyle.opacity;
        float IResolvedStyle.paddingBottom => resolvedStyle.paddingBottom;
        float IResolvedStyle.paddingLeft => resolvedStyle.paddingLeft;
        float IResolvedStyle.paddingRight => resolvedStyle.paddingRight;
        float IResolvedStyle.paddingTop => resolvedStyle.paddingTop;
        Position IResolvedStyle.position => resolvedStyle.position;
        float IResolvedStyle.right => resolvedStyle.right;
        Rotate IResolvedStyle.rotate => resolvedStyle.rotate;
        Scale IResolvedStyle.scale => resolvedStyle.scale;
        TextOverflow IResolvedStyle.textOverflow => resolvedStyle.textOverflow;
        float IResolvedStyle.top => resolvedStyle.top;
        Vector3 IResolvedStyle.transformOrigin => resolvedStyle.transformOrigin;
        IEnumerable<TimeValue> IResolvedStyle.transitionDelay => resolvedStyle.transitionDelay;
        IEnumerable<TimeValue> IResolvedStyle.transitionDuration => resolvedStyle.transitionDuration;
        IEnumerable<StylePropertyName> IResolvedStyle.transitionProperty => resolvedStyle.transitionProperty;
        IEnumerable<EasingFunction> IResolvedStyle.transitionTimingFunction => resolvedStyle.transitionTimingFunction;
        Vector3 IResolvedStyle.translate => resolvedStyle.translate;
        Color IResolvedStyle.unityBackgroundImageTintColor => resolvedStyle.unityBackgroundImageTintColor;
        EditorTextRenderingMode IResolvedStyle.unityEditorTextRenderingMode => resolvedStyle.unityEditorTextRenderingMode;
        Font IResolvedStyle.unityFont => resolvedStyle.unityFont;
        FontDefinition IResolvedStyle.unityFontDefinition => resolvedStyle.unityFontDefinition;
        FontStyle IResolvedStyle.unityFontStyleAndWeight => resolvedStyle.unityFontStyleAndWeight;
        float IResolvedStyle.unityParagraphSpacing => resolvedStyle.unityParagraphSpacing;
        int IResolvedStyle.unitySliceBottom => resolvedStyle.unitySliceBottom;
        int IResolvedStyle.unitySliceLeft => resolvedStyle.unitySliceLeft;
        int IResolvedStyle.unitySliceRight => resolvedStyle.unitySliceRight;
        float IResolvedStyle.unitySliceScale => resolvedStyle.unitySliceScale;
        int IResolvedStyle.unitySliceTop => resolvedStyle.unitySliceTop;
        SliceType IResolvedStyle.unitySliceType => resolvedStyle.unitySliceType;
        TextAnchor IResolvedStyle.unityTextAlign => resolvedStyle.unityTextAlign;
        TextGeneratorType IResolvedStyle.unityTextGenerator => resolvedStyle.unityTextGenerator;
        Color IResolvedStyle.unityTextOutlineColor => resolvedStyle.unityTextOutlineColor;
        float IResolvedStyle.unityTextOutlineWidth => resolvedStyle.unityTextOutlineWidth;
        TextOverflowPosition IResolvedStyle.unityTextOverflowPosition => resolvedStyle.unityTextOverflowPosition;
        Visibility IResolvedStyle.visibility => resolvedStyle.visibility;
        WhiteSpace IResolvedStyle.whiteSpace => resolvedStyle.whiteSpace;
        float IResolvedStyle.width => resolvedStyle.width;
        float IResolvedStyle.wordSpacing => resolvedStyle.wordSpacing;
    }
}
