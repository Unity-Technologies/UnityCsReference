// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public interface IResolvedStyle
    {
        float width { get; }
        float height { get; }
        StyleFloat maxWidth { get; }
        StyleFloat maxHeight { get; }
        StyleFloat minWidth { get; }
        StyleFloat minHeight { get; }
        StyleFloat flexBasis { get; }
        float flexGrow { get; }
        float flexShrink { get; }
        FlexDirection flexDirection { get; }
        Wrap flexWrap { get; }
        float left { get; }
        float top { get; }
        float right { get; }
        float bottom { get; }
        float marginLeft { get; }
        float marginTop { get; }
        float marginRight { get; }
        float marginBottom { get; }
        float paddingLeft { get; }
        float paddingTop { get; }
        float paddingRight { get; }
        float paddingBottom { get; }
        Position position { get; }
        Align alignSelf { get; }
        TextAnchor unityTextAlign { get; }
        FontStyle unityFontStyleAndWeight { get; }
        float fontSize { get; }
        WhiteSpace whiteSpace { get; }
        Color color { get; }
        Color backgroundColor { get; }
        [Obsolete("IResolvedStyle.borderColor is deprecated. Use left/right/top/bottom border properties instead.")]
        Color borderColor { get; }
        Font unityFont { get; }
        ScaleMode unityBackgroundScaleMode { get; }
        Color unityBackgroundImageTintColor { get; }
        Align alignItems { get; }
        Align alignContent { get; }
        Justify justifyContent { get; }
        Color borderLeftColor { get; }
        Color borderRightColor { get; }
        Color borderTopColor { get; }
        Color borderBottomColor { get; }
        float borderLeftWidth { get; }
        float borderRightWidth { get; }
        float borderTopWidth { get; }
        float borderBottomWidth { get; }
        float borderTopLeftRadius { get; }
        float borderTopRightRadius { get; }
        float borderBottomRightRadius { get; }
        float borderBottomLeftRadius { get; }
        int unitySliceLeft  { get; }
        int unitySliceTop { get; }
        int unitySliceRight { get; }
        int unitySliceBottom { get; }
        float opacity { get; }
        Visibility visibility { get; }
        DisplayStyle display { get; }
    }
}
