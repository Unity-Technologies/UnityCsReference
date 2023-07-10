// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    static class UIElementsInitialization
    {
        [RequiredByNativeCode(optional:false)]
        public static void InitializeUIElementsManaged()
        {
            RegisterBuiltInPropertyBags();
        }

        internal static void RegisterBuiltInPropertyBags()
        {
            PropertyBag.Register(new InlineStyleAccessPropertyBag());
            PropertyBag.Register(new ResolvedStyleAccessPropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Align>, Align>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<DisplayStyle>, DisplayStyle>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<EasingMode>, EasingMode>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<FlexDirection>, FlexDirection>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<FontStyle>, FontStyle>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Justify>, Justify>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Overflow>, Overflow>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<OverflowClipBox>, OverflowClipBox>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<OverflowInternal>, OverflowInternal>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Position>, Position>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<ScaleMode>, ScaleMode>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<TextAnchor>, TextAnchor>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<TextOverflow>, TextOverflow>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<TextOverflowPosition>, TextOverflowPosition>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<TransformOriginOffset>, TransformOriginOffset>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Visibility>, Visibility>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<WhiteSpace>, WhiteSpace>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleEnum<Wrap>, Wrap>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleBackground, Background>());
            PropertyBag.Register(new Length.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleBackgroundPosition, BackgroundPosition>());
            PropertyBag.Register(new BackgroundPosition.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleBackgroundRepeat, BackgroundRepeat>());
            PropertyBag.Register(new BackgroundRepeat.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleBackgroundSize, BackgroundSize>());
            PropertyBag.Register(new BackgroundSize.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleColor, Color>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleCursor, Cursor>());
            PropertyBag.Register(new Cursor.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleFloat, float>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleFont, Font>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleFontDefinition, FontDefinition>());
            PropertyBag.Register(new FontDefinition.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleInt, int>());
            PropertyBag.Register(new StyleValuePropertyBag<StyleLength, Length>());
            PropertyBag.Register(new Background.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleList<EasingFunction>, List<EasingFunction>>());
            PropertyBag.Register(new EasingFunction.PropertyBag());
            PropertyBag.RegisterList<StyleList<EasingFunction>, EasingFunction>();
            PropertyBag.Register(new StyleValuePropertyBag<StyleList<StylePropertyName>, List<StylePropertyName>>());
            PropertyBag.Register(new StylePropertyName.PropertyBag());
            PropertyBag.RegisterList<StyleList<StylePropertyName>, StylePropertyName>();
            PropertyBag.Register(new StyleValuePropertyBag<StyleList<TimeValue>, List<TimeValue>>());
            PropertyBag.Register(new TimeValue.PropertyBag());
            PropertyBag.RegisterList<StyleList<TimeValue>, TimeValue>();
            PropertyBag.Register(new StyleValuePropertyBag<StyleRotate, Rotate>());
            PropertyBag.Register(new Rotate.PropertyBag());
            PropertyBag.Register(new Angle.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleScale, Scale>());
            PropertyBag.Register(new Scale.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleTransformOrigin, TransformOrigin>());
            PropertyBag.Register(new TransformOrigin.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleTranslate, Translate>());
            PropertyBag.Register(new Translate.PropertyBag());
            PropertyBag.Register(new StyleValuePropertyBag<StyleTextShadow, TextShadow>());
            PropertyBag.Register(new TextShadow.PropertyBag());
        }
    }
}
