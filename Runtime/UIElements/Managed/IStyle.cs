// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        StyleValue<float> flex { get; set; }
        StyleValue<Overflow> overflow { get; set; }
        StyleValue<float> positionLeft { get; set; }
        StyleValue<float> positionTop { get; set; }
        StyleValue<float> positionRight { get; set; }
        StyleValue<float> positionBottom { get; set; }
        StyleValue<float> marginLeft { get; set; }
        StyleValue<float> marginTop { get; set; }
        StyleValue<float> marginRight { get; set; }
        StyleValue<float> marginBottom { get; set; }
        StyleValue<float> borderLeft { get; set; }
        StyleValue<float> borderTop { get; set; }
        StyleValue<float> borderRight { get; set; }
        StyleValue<float> borderBottom { get; set; }
        StyleValue<float> paddingLeft { get; set; }
        StyleValue<float> paddingTop { get; set; }
        StyleValue<float> paddingRight { get; set; }
        StyleValue<float> paddingBottom { get; set; }
        StyleValue<PositionType> positionType { get; set; }
        StyleValue<Align> alignSelf { get; set; }
        StyleValue<TextAnchor> textAlignment { get; set; }
        StyleValue<FontStyle> fontStyle { get; set; }
        StyleValue<TextClipping> textClipping { get; set; }
        StyleValue<Font> font { get; set; }
        StyleValue<int> fontSize { get; set; }
        StyleValue<bool> wordWrap { get; set; }
        StyleValue<Color> textColor { get; set; }
        StyleValue<FlexDirection> flexDirection { get; set; }
        StyleValue<Color> backgroundColor { get; set; }
        StyleValue<Color> borderColor { get; set; }
        StyleValue<Texture2D> backgroundImage { get; set; }
        StyleValue<ScaleMode> backgroundSize { get; set; }
        StyleValue<Align> alignItems { get; set; }
        StyleValue<Align> alignContent { get; set; }
        StyleValue<Justify> justifyContent { get; set; }
        StyleValue<Wrap> flexWrap { get; set; }
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
    }
}
