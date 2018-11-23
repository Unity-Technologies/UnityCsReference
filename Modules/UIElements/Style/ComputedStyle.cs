// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal struct ComputedStyle
    {
        private VisualElement m_Element;

        private VisualElementStylesData stylesData
        {
            get { return m_Element.specifiedStyle; }
        }

        private InheritedStylesData inheritedStylesData
        {
            get { return m_Element.inheritedStyle; }
        }

        public ComputedStyle(VisualElement element)
        {
            m_Element = element;
        }

        public StyleLength width => stylesData.width;
        public StyleLength height => stylesData.height;
        public StyleLength maxWidth => stylesData.maxWidth.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.None);
        public StyleLength maxHeight => stylesData.maxHeight.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.None);
        public StyleLength minWidth => stylesData.minWidth.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        public StyleLength minHeight => stylesData.minHeight.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        public StyleLength flexBasis => stylesData.flexBasis.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        public StyleFloat flexGrow => stylesData.flexGrow.GetSpecifiedValueOrDefault<StyleFloat, float>(new StyleFloat(VisualElementStylesData.k_DefaultFlexGrow));
        public StyleFloat flexShrink => stylesData.flexShrink.GetSpecifiedValueOrDefault<StyleFloat, float>(new StyleFloat(VisualElementStylesData.k_DefaultFlexShrink));
        public StyleEnum<FlexDirection> flexDirection => stylesData.flexDirection.ToStyleEnum((FlexDirection)stylesData.flexDirection.value);
        public StyleEnum<Wrap> flexWrap => stylesData.flexWrap.ToStyleEnum((Wrap)stylesData.flexWrap.value);
        public StyleEnum<Overflow> overflow => stylesData.overflow.ToStyleEnum((Overflow)stylesData.overflow.value);
        public StyleLength left => stylesData.left;
        public StyleLength top => stylesData.top;
        public StyleLength right => stylesData.right;
        public StyleLength bottom => stylesData.bottom;
        public StyleLength marginLeft => stylesData.marginLeft;
        public StyleLength marginTop => stylesData.marginTop;
        public StyleLength marginRight => stylesData.marginRight;
        public StyleLength marginBottom => stylesData.marginBottom;
        public StyleLength paddingLeft => stylesData.paddingLeft;
        public StyleLength paddingTop => stylesData.paddingTop;
        public StyleLength paddingRight => stylesData.paddingRight;
        public StyleLength paddingBottom => stylesData.paddingBottom;
        public StyleEnum<Position> position => stylesData.position.ToStyleEnum((Position)stylesData.position.value);
        public StyleEnum<Align> alignSelf => stylesData.alignSelf.ToStyleEnum((Align)stylesData.alignSelf.value);
        public StyleColor backgroundColor => stylesData.backgroundColor;
        public StyleColor borderColor => stylesData.borderColor;
        public StyleBackground backgroundImage => stylesData.backgroundImage;
        public StyleEnum<ScaleMode> unityBackgroundScaleMode => stylesData.backgroundScaleMode.ToStyleEnum((ScaleMode)stylesData.backgroundScaleMode.value);
        public StyleEnum<Align> alignItems => (Align)stylesData.alignItems.GetSpecifiedValueOrDefault((int)VisualElementStylesData.k_DefaultAlignItems);
        public StyleEnum<Align> alignContent => (Align)stylesData.alignContent.GetSpecifiedValueOrDefault((int)VisualElementStylesData.k_DefaultAlignContent);
        public StyleEnum<Justify> justifyContent => stylesData.justifyContent.ToStyleEnum((Justify)stylesData.justifyContent.value);
        public StyleFloat borderLeftWidth => stylesData.borderLeftWidth;
        public StyleFloat borderTopWidth => stylesData.borderTopWidth;
        public StyleFloat borderRightWidth => stylesData.borderRightWidth;
        public StyleFloat borderBottomWidth => stylesData.borderBottomWidth;
        public StyleLength borderTopLeftRadius => stylesData.borderTopLeftRadius;
        public StyleLength borderTopRightRadius => stylesData.borderTopRightRadius;
        public StyleLength borderBottomRightRadius => stylesData.borderBottomRightRadius;
        public StyleLength borderBottomLeftRadius => stylesData.borderBottomLeftRadius;
        public StyleInt unitySliceLeft => stylesData.sliceLeft;
        public StyleInt unitySliceTop => stylesData.sliceTop;
        public StyleInt unitySliceRight => stylesData.sliceRight;
        public StyleInt unitySliceBottom => stylesData.sliceBottom;
        public StyleFloat opacity => stylesData.opacity.GetSpecifiedValueOrDefault(1.0f);
        public StyleEnum<DisplayStyle> display => stylesData.display.ToStyleEnum((DisplayStyle)stylesData.display.value);
        public StyleCursor cursor => stylesData.cursor;

        // Inherited properties
        public StyleColor color => stylesData.color.GetSpecifiedValueOrDefault<StyleColor, Color>(inheritedStylesData.color);
        public StyleFont unityFont => stylesData.font.GetSpecifiedValueOrDefault<StyleFont, Font>(inheritedStylesData.font);
        public StyleLength fontSize => stylesData.fontSize.GetSpecifiedValueOrDefault<StyleLength, Length>(inheritedStylesData.fontSize);

        public StyleEnum<FontStyle> unityFontStyleAndWeight
        {
            get
            {
                var styleInt = stylesData.fontStyleAndWeight.GetSpecifiedValueOrDefault<StyleInt, int>(inheritedStylesData.unityFontStyle);
                return styleInt.ToStyleEnum((FontStyle)styleInt.value);
            }
        }

        public StyleEnum<TextAnchor> unityTextAlign
        {
            get
            {
                var styleInt = stylesData.unityTextAlign.GetSpecifiedValueOrDefault<StyleInt, int>(inheritedStylesData.unityTextAlign);
                return styleInt.ToStyleEnum((TextAnchor)styleInt.value);
            }
        }

        public StyleEnum<Visibility> visibility
        {
            get
            {
                var styleInt = stylesData.visibility.GetSpecifiedValueOrDefault<StyleInt, int>(inheritedStylesData.visibility);
                return styleInt.ToStyleEnum((Visibility)styleInt.value);
            }
        }

        public StyleEnum<WhiteSpace> whiteSpace
        {
            get
            {
                var styleInt = stylesData.whiteSpace.GetSpecifiedValueOrDefault<StyleInt, int>(inheritedStylesData.whiteSpace);
                return styleInt.ToStyleEnum((WhiteSpace)styleInt.value);
            }
        }

        internal static void WriteToGUIStyle(ComputedStyle computedStyle, GUIStyle style)
        {
            style.alignment = computedStyle.unityTextAlign.GetSpecifiedValueOrDefault(style.alignment);
            style.wordWrap = computedStyle.whiteSpace.specificity > StyleValueExtensions.UndefinedSpecificity
                ? computedStyle.whiteSpace.value == WhiteSpace.Normal
                : style.wordWrap;
            bool overflowVisible = computedStyle.overflow.specificity > StyleValueExtensions.UndefinedSpecificity
                ? computedStyle.overflow.value == Overflow.Visible
                : style.clipping == TextClipping.Overflow;
            style.clipping = overflowVisible ? TextClipping.Overflow : TextClipping.Clip;
            if (computedStyle.unityFont.value != null)
            {
                style.font = computedStyle.unityFont.value;
            }

            style.fontSize = (int)computedStyle.fontSize.GetSpecifiedValueOrDefault((float)style.fontSize);
            style.fontStyle = computedStyle.unityFontStyleAndWeight.GetSpecifiedValueOrDefault(style.fontStyle);

            AssignRect(style.margin, computedStyle.marginLeft, computedStyle.marginTop, computedStyle.marginRight, computedStyle.marginBottom);
            AssignRect(style.padding, computedStyle.paddingLeft, computedStyle.paddingTop, computedStyle.paddingRight, computedStyle.paddingBottom);
            AssignRect(style.border, computedStyle.unitySliceLeft, computedStyle.unitySliceTop, computedStyle.unitySliceRight, computedStyle.unitySliceBottom);
            AssignState(computedStyle, style.normal);
            AssignState(computedStyle, style.focused);
            AssignState(computedStyle, style.hover);
            AssignState(computedStyle, style.active);
            AssignState(computedStyle, style.onNormal);
            AssignState(computedStyle, style.onFocused);
            AssignState(computedStyle, style.onHover);
            AssignState(computedStyle, style.onActive);
        }

        private static void AssignState(ComputedStyle computedStyle, GUIStyleState state)
        {
            state.textColor = computedStyle.color.GetSpecifiedValueOrDefault(state.textColor);
            if (computedStyle.backgroundImage.value.texture != null)
            {
                state.background = computedStyle.backgroundImage.value.texture;
                if (state.scaledBackgrounds == null || state.scaledBackgrounds.Length < 1 || state.scaledBackgrounds[0] != computedStyle.backgroundImage.value.texture)
                    state.scaledBackgrounds = new Texture2D[1] { computedStyle.backgroundImage.value.texture };
            }
        }

        private static void AssignRect(RectOffset rect, StyleLength left, StyleLength top, StyleLength right, StyleLength bottom)
        {
            rect.left = (int)left.GetSpecifiedValueOrDefault((float)rect.left);
            rect.top = (int)top.GetSpecifiedValueOrDefault((float)rect.top);
            rect.right = (int)right.GetSpecifiedValueOrDefault((float)rect.right);
            rect.bottom = (int)bottom.GetSpecifiedValueOrDefault((float)rect.bottom);
        }

        private static void AssignRect(RectOffset rect, StyleInt left, StyleInt top, StyleInt right, StyleInt bottom)
        {
            rect.left = left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }
    }
}
