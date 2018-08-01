// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements
{
    public interface IStyle
    {
        StyleValue<float> width { get; set; }
        StyleValue<float> height { get; set; }
        StyleValue<float> maxWidth { get; set; }
        StyleValue<float> maxHeight { get; set; }
        StyleValue<float> minWidth { get; set; }
        StyleValue<float> minHeight { get; set; }
        StyleValue<Flex> flex { get; set; }
        StyleValue<float> flexBasis { get; set; }
        StyleValue<float> flexGrow { get; set; }
        StyleValue<float> flexShrink { get; set; }
        StyleValue<FlexDirection> flexDirection { get; set; }
        StyleValue<Wrap> flexWrap { get; set; }

        StyleValue<Overflow> overflow { get; set; }
        StyleValue<float> positionLeft { get; set; }
        StyleValue<float> positionTop { get; set; }
        StyleValue<float> positionRight { get; set; }
        StyleValue<float> positionBottom { get; set; }
        StyleValue<float> marginLeft { get; set; }
        StyleValue<float> marginTop { get; set; }
        StyleValue<float> marginRight { get; set; }
        StyleValue<float> marginBottom { get; set; }
        [Obsolete("Use borderLeftWidth instead")]
        StyleValue<float> borderLeft { get; set; }
        [Obsolete("Use borderTopWidth instead")]
        StyleValue<float> borderTop { get; set; }
        [Obsolete("Use borderRightWidth instead")]
        StyleValue<float> borderRight { get; set; }
        [Obsolete("Use borderBottomWidth instead")]
        StyleValue<float> borderBottom { get; set; }
        StyleValue<float> paddingLeft { get; set; }
        StyleValue<float> paddingTop { get; set; }
        StyleValue<float> paddingRight { get; set; }
        StyleValue<float> paddingBottom { get; set; }
        StyleValue<PositionType> positionType { get; set; }
        StyleValue<Align> alignSelf { get; set; }
        [Obsolete("Use unityTextAlign instead")]
        StyleValue<TextAnchor> textAlignment { get; set; }
        StyleValue<TextAnchor> unityTextAlign { get; set; }
        [Obsolete("Use fontStyleAndWeight instead")]
        StyleValue<FontStyle> fontStyle { get; set; }
        StyleValue<FontStyle> fontStyleAndWeight { get; set; }
        StyleValue<TextClipping> textClipping { get; set; }
        StyleValue<Font> font { get; set; }
        StyleValue<int> fontSize { get; set; }
        StyleValue<bool> wordWrap { get; set; }
        [Obsolete("Use color instead")]
        StyleValue<Color> textColor { get; set; }
        StyleValue<Color> color { get; set; }
        StyleValue<Color> backgroundColor { get; set; }
        StyleValue<Color> borderColor { get; set; }
        StyleValue<Texture2D> backgroundImage { get; set; }
        [Obsolete("Use backgroundScaleMode instead")]
        StyleValue<ScaleMode> backgroundSize { get; set; }
        StyleValue<ScaleMode> backgroundScaleMode { get; set; }
        StyleValue<Align> alignItems { get; set; }
        StyleValue<Align> alignContent { get; set; }
        StyleValue<Justify> justifyContent { get; set; }
        StyleValue<float> borderLeftWidth { get; set; }
        StyleValue<float> borderTopWidth { get; set; }
        StyleValue<float> borderRightWidth { get; set; }
        StyleValue<float> borderBottomWidth { get; set; }
        StyleValue<float> borderRadius { get; set; }
        StyleValue<float> borderTopLeftRadius { get; set; }
        StyleValue<float> borderTopRightRadius { get; set; }
        StyleValue<float> borderBottomRightRadius { get; set; }
        StyleValue<float> borderBottomLeftRadius { get; set; }
        StyleValue<int> sliceLeft { get; set; }
        StyleValue<int> sliceTop { get; set; }
        StyleValue<int> sliceRight { get; set; }
        StyleValue<int> sliceBottom { get; set; }
        StyleValue<float> opacity { get; set; }
        StyleValue<CursorStyle> cursor { get; set; }
        StyleValue<Visibility> visibility { get; set; }
    }
}
