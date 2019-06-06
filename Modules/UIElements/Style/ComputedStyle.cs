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
        public StyleLength maxWidth => stylesData.maxWidth;
        public StyleLength maxHeight => stylesData.maxHeight;
        public StyleLength minWidth => stylesData.minWidth;
        public StyleLength minHeight => stylesData.minHeight;
        public StyleLength flexBasis => stylesData.flexBasis;
        public StyleFloat flexGrow => stylesData.flexGrow;
        public StyleFloat flexShrink => stylesData.flexShrink;
        public StyleEnum<FlexDirection> flexDirection => stylesData.flexDirection.ToStyleEnum((FlexDirection)stylesData.flexDirection.value);
        public StyleEnum<Wrap> flexWrap => stylesData.flexWrap.ToStyleEnum((Wrap)stylesData.flexWrap.value);
        public StyleEnum<Overflow> overflow => stylesData.overflow.ToStyleEnum((Overflow)stylesData.overflow.value);
        public StyleEnum<OverflowClipBox> unityOverflowClipBox => stylesData.unityOverflowClipBox.ToStyleEnum((OverflowClipBox)stylesData.unityOverflowClipBox.value);
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
        public StyleEnum<ScaleMode> unityBackgroundScaleMode => stylesData.unityBackgroundScaleMode.ToStyleEnum((ScaleMode)stylesData.unityBackgroundScaleMode.value);
        public StyleColor unityBackgroundImageTintColor => stylesData.unityBackgroundImageTintColor;
        public StyleEnum<Align> alignItems => stylesData.alignItems.ToStyleEnum((Align)stylesData.alignItems.value);
        public StyleEnum<Align> alignContent => stylesData.alignContent.ToStyleEnum((Align)stylesData.alignContent.value);
        public StyleEnum<Justify> justifyContent => stylesData.justifyContent.ToStyleEnum((Justify)stylesData.justifyContent.value);
        public StyleFloat borderLeftWidth => stylesData.borderLeftWidth;
        public StyleFloat borderTopWidth => stylesData.borderTopWidth;
        public StyleFloat borderRightWidth => stylesData.borderRightWidth;
        public StyleFloat borderBottomWidth => stylesData.borderBottomWidth;
        public StyleLength borderTopLeftRadius => stylesData.borderTopLeftRadius;
        public StyleLength borderTopRightRadius => stylesData.borderTopRightRadius;
        public StyleLength borderBottomRightRadius => stylesData.borderBottomRightRadius;
        public StyleLength borderBottomLeftRadius => stylesData.borderBottomLeftRadius;
        public StyleInt unitySliceLeft => stylesData.unitySliceLeft;
        public StyleInt unitySliceTop => stylesData.unitySliceTop;
        public StyleInt unitySliceRight => stylesData.unitySliceRight;
        public StyleInt unitySliceBottom => stylesData.unitySliceBottom;
        public StyleFloat opacity => stylesData.opacity;
        public StyleEnum<DisplayStyle> display => stylesData.display.ToStyleEnum((DisplayStyle)stylesData.display.value);
        public StyleCursor cursor => stylesData.cursor;

        // Inherited properties
        public StyleColor color => stylesData.color.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.color : inheritedStylesData.color;
        public StyleFont unityFont => stylesData.unityFont.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.unityFont : inheritedStylesData.font;

        public StyleLength fontSize
        {
            get
            {
                int specificity = stylesData.fontSize.specificity;
                if (specificity != StyleValueExtensions.UndefinedSpecificity)
                {
                    // If it's a relative length (percentage) the computed value needs to be absolute
                    float pixelSize = CalculatePixelFontSize(m_Element);
                    return new StyleLength(pixelSize) {specificity = specificity};
                }

                return inheritedStylesData.fontSize;
            }
        }

        public StyleEnum<FontStyle> unityFontStyleAndWeight
        {
            get
            {
                var styleInt = stylesData.unityFontStyleAndWeight.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.unityFontStyleAndWeight : inheritedStylesData.unityFontStyle;
                return styleInt.ToStyleEnum((FontStyle)styleInt.value);
            }
        }

        public StyleEnum<TextAnchor> unityTextAlign
        {
            get
            {
                var styleInt = stylesData.unityTextAlign.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.unityTextAlign : inheritedStylesData.unityTextAlign;
                return styleInt.ToStyleEnum((TextAnchor)styleInt.value);
            }
        }

        public StyleEnum<Visibility> visibility
        {
            get
            {
                var styleInt = stylesData.visibility.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.visibility : inheritedStylesData.visibility;
                return styleInt.ToStyleEnum((Visibility)styleInt.value);
            }
        }

        public StyleEnum<WhiteSpace> whiteSpace
        {
            get
            {
                var styleInt = stylesData.whiteSpace.specificity != StyleValueExtensions.UndefinedSpecificity ? stylesData.whiteSpace : inheritedStylesData.whiteSpace;
                return styleInt.ToStyleEnum((WhiteSpace)styleInt.value);
            }
        }

        public static float CalculatePixelFontSize(VisualElement ve)
        {
            var fontSize = ve.specifiedStyle.fontSize.value;
            if (fontSize.unit == LengthUnit.Percent)
            {
                var parent = ve.hierarchy.parent;
                float parentSize = parent != null ? parent.resolvedStyle.fontSize : 0f;
                float computedSize = parentSize * fontSize.value / 100;
                fontSize = new Length(computedSize);
            }
            return fontSize.value;
        }
    }
}
