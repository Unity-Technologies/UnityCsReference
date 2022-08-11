// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;
using Unity.Properties;
using Unity.Properties.Internal;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        static readonly ProfilerMarker s_PropertyBagsRegistrationMarker = new ProfilerMarker("UI Toolkit PropertyBags Registration");

        static VisualElement()
        {
            using (s_PropertyBagsRegistrationMarker.Auto())
            {
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Align>, Align>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<DisplayStyle>, DisplayStyle>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<EasingMode>, EasingMode>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<FlexDirection>, FlexDirection>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<FontStyle>, FontStyle>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Justify>, Justify>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Overflow>, Overflow>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<OverflowClipBox>, OverflowClipBox>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<OverflowInternal>, OverflowInternal>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Position>, Position>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<ScaleMode>, ScaleMode>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<TextAnchor>, TextAnchor>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<TextOverflow>, TextOverflow>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<TextOverflowPosition>, TextOverflowPosition>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<TransformOriginOffset>, TransformOriginOffset>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Visibility>, Visibility>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<WhiteSpace>, WhiteSpace>());
                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleEnum<Wrap>, Wrap>());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleBackground, Background>());
                PropertyBagStore.AddPropertyBag(new Length.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleBackgroundPosition, BackgroundPosition>());
                PropertyBagStore.AddPropertyBag(new BackgroundPosition.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleBackgroundRepeat, BackgroundRepeat>());
                PropertyBagStore.AddPropertyBag(new BackgroundRepeat.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleBackgroundSize, BackgroundSize>());
                PropertyBagStore.AddPropertyBag(new BackgroundSize.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleColor, Color>());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleCursor, Cursor>());
                PropertyBagStore.AddPropertyBag(new Cursor.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleFloat, float>());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleFont, Font>());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleFontDefinition, FontDefinition>());
                PropertyBagStore.AddPropertyBag(new FontDefinition.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleInt, int>());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleLength, Length>());
                PropertyBagStore.AddPropertyBag(new Background.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleList<EasingFunction>, List<EasingFunction>>());
                PropertyBagStore.AddPropertyBag(new EasingFunction.PropertyBag());
                PropertyBag.RegisterList<StyleList<EasingFunction>, EasingFunction>();

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleList<StylePropertyName>, List<StylePropertyName>>());
                PropertyBagStore.AddPropertyBag(new StylePropertyName.PropertyBag());
                PropertyBag.RegisterList<StyleList<StylePropertyName>, StylePropertyName>();

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleList<TimeValue>, List<TimeValue>>());
                PropertyBagStore.AddPropertyBag(new TimeValue.PropertyBag());
                PropertyBag.RegisterList<StyleList<TimeValue>, TimeValue>();

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleRotate, Rotate>());
                PropertyBagStore.AddPropertyBag(new Rotate.PropertyBag());
                PropertyBagStore.AddPropertyBag(new Angle.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleScale, Scale>());
                PropertyBagStore.AddPropertyBag(new Scale.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleTransformOrigin, TransformOrigin>());
                PropertyBagStore.AddPropertyBag(new TransformOrigin.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleTranslate, Translate>());
                PropertyBagStore.AddPropertyBag(new Translate.PropertyBag());

                PropertyBagStore.AddPropertyBag(new StyleValuePropertyBag<StyleTextShadow, TextShadow>());
                PropertyBagStore.AddPropertyBag(new TextShadow.PropertyBag());
            }
        }
    }
}
