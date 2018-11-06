// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public interface IStyle
    {
        StyleLength width { get; set; }
        StyleLength height { get; set; }
        StyleLength maxWidth { get; set; }
        StyleLength maxHeight { get; set; }
        StyleLength minWidth { get; set; }
        StyleLength minHeight { get; set; }
        StyleLength flexBasis { get; set; }
        StyleFloat flexGrow { get; set; }
        StyleFloat flexShrink { get; set; }
        StyleEnum<FlexDirection> flexDirection { get; set; }
        StyleEnum<Wrap> flexWrap { get; set; }
        StyleEnum<Overflow> overflow { get; set; }
        StyleLength left { get; set; }
        StyleLength top { get; set; }
        StyleLength right { get; set; }
        StyleLength bottom { get; set; }
        StyleLength marginLeft { get; set; }
        StyleLength marginTop { get; set; }
        StyleLength marginRight { get; set; }
        StyleLength marginBottom { get; set; }
        StyleLength paddingLeft { get; set; }
        StyleLength paddingTop { get; set; }
        StyleLength paddingRight { get; set; }
        StyleLength paddingBottom { get; set; }
        StyleEnum<Position> position { get; set; }
        StyleEnum<Align> alignSelf { get; set; }
        StyleEnum<TextAnchor> unityTextAlign { get; set; }
        StyleEnum<FontStyle> unityFontStyleAndWeight { get; set; }
        StyleFont unityFont { get; set; }
        StyleLength fontSize { get; set; }
        StyleEnum<WhiteSpace> whiteSpace { get; set; }
        StyleColor color { get; set; }
        StyleColor backgroundColor { get; set; }
        StyleColor borderColor { get; set; }
        StyleBackground backgroundImage { get; set; }
        StyleEnum<ScaleMode> unityBackgroundScaleMode { get; set; }
        StyleEnum<Align> alignItems { get; set; }
        StyleEnum<Align> alignContent { get; set; }
        StyleEnum<Justify> justifyContent { get; set; }
        StyleFloat borderLeftWidth { get; set; }
        StyleFloat borderTopWidth { get; set; }
        StyleFloat borderRightWidth { get; set; }
        StyleFloat borderBottomWidth { get; set; }
        StyleLength borderTopLeftRadius { get; set; }
        StyleLength borderTopRightRadius { get; set; }
        StyleLength borderBottomRightRadius { get; set; }
        StyleLength borderBottomLeftRadius { get; set; }
        StyleInt unitySliceLeft { get; set; }
        StyleInt unitySliceTop { get; set; }
        StyleInt unitySliceRight { get; set; }
        StyleInt unitySliceBottom { get; set; }
        StyleFloat opacity { get; set; }
        StyleEnum<Visibility> visibility { get; set; }
        StyleCursor cursor { get; set; }
    }
}
