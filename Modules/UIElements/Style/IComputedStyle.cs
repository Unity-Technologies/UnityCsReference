// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal interface IComputedStyle
    {
        StyleLength width { get; }
        StyleLength height { get; }
        StyleLength maxWidth { get; }
        StyleLength maxHeight { get; }
        StyleLength minWidth { get; }
        StyleLength minHeight { get; }
        StyleLength flexBasis { get; }
        StyleFloat flexGrow { get; }
        StyleFloat flexShrink { get; }
        StyleEnum<FlexDirection> flexDirection { get; }
        StyleEnum<Wrap> flexWrap { get; }
        StyleEnum<Overflow> overflow { get; }
        StyleLength left { get; }
        StyleLength top { get; }
        StyleLength right { get; }
        StyleLength bottom { get; }
        StyleLength marginLeft { get; }
        StyleLength marginTop { get; }
        StyleLength marginRight { get; }
        StyleLength marginBottom { get; }
        StyleLength paddingLeft { get; }
        StyleLength paddingTop { get; }
        StyleLength paddingRight { get; }
        StyleLength paddingBottom { get; }
        StyleEnum<Position> position { get; }
        StyleEnum<Align> alignSelf { get; }
        StyleEnum<TextAnchor> unityTextAlign { get; }
        StyleEnum<FontStyle> unityFontStyleAndWeight { get; }
        StyleFont unityFont { get; }
        StyleLength fontSize { get; }
        StyleEnum<WhiteSpace> whiteSpace { get; }
        StyleColor color { get; }
        StyleColor backgroundColor { get; }
        StyleColor borderColor { get; }
        StyleBackground backgroundImage { get; }
        StyleEnum<ScaleMode> unityBackgroundScaleMode { get; }
        StyleEnum<Align> alignItems { get; }
        StyleEnum<Align> alignContent { get; }
        StyleEnum<Justify> justifyContent { get; }
        StyleFloat borderLeftWidth { get; }
        StyleFloat borderTopWidth { get; }
        StyleFloat borderRightWidth { get; }
        StyleFloat borderBottomWidth { get; }
        StyleLength borderTopLeftRadius { get; }
        StyleLength borderTopRightRadius { get; }
        StyleLength borderBottomRightRadius { get; }
        StyleLength borderBottomLeftRadius { get; }
        StyleInt unitySliceLeft { get; }
        StyleInt unitySliceTop { get; }
        StyleInt unitySliceRight { get; }
        StyleInt unitySliceBottom { get; }
        StyleFloat opacity { get; }
        StyleCursor cursor { get; }
        StyleEnum<Visibility> visibility { get; }
    }
}
