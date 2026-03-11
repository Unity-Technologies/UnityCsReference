// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;
using Unity.Properties;
using Unity.Properties.Internal;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    static class UIElementsInitialization
    {
        static readonly ProfilerMarker s_InitializeUIElementsManagedMarker =
            new ProfilerMarker("InitializeUIElementsManaged");

        [RequiredByNativeCode(optional: true)]
        [RequiredMember]
        public static void InitializeUIElementsManaged()
        {
            s_InitializeUIElementsManagedMarker.Begin();
            RegisterBuiltInPropertyBags();
            RegisterConverters();
            s_InitializeUIElementsManagedMarker.End();
        }

        internal static void RegisterBuiltInPropertyBags()
        {
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(InlineStyleAccess), () => new InlineStyleAccessPropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(ResolvedStyleAccess), () => new ResolvedStyleAccessPropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Align>), () => new StyleValuePropertyBag<StyleEnum<Align>, Align>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<DisplayStyle>), () => new StyleValuePropertyBag<StyleEnum<DisplayStyle>, DisplayStyle>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<EasingMode>), () => new StyleValuePropertyBag<StyleEnum<EasingMode>, EasingMode>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<FlexDirection>), () => new StyleValuePropertyBag<StyleEnum<FlexDirection>, FlexDirection>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<FontStyle>), () => new StyleValuePropertyBag<StyleEnum<FontStyle>, FontStyle>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Justify>), () => new StyleValuePropertyBag<StyleEnum<Justify>, Justify>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Overflow>), () => new StyleValuePropertyBag<StyleEnum<Overflow>, Overflow>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<OverflowClipBox>), () => new StyleValuePropertyBag<StyleEnum<OverflowClipBox>, OverflowClipBox>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<OverflowInternal>), () => new StyleValuePropertyBag<StyleEnum<OverflowInternal>, OverflowInternal>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Position>), () => new StyleValuePropertyBag<StyleEnum<Position>, Position>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<ScaleMode>), () => new StyleValuePropertyBag<StyleEnum<ScaleMode>, ScaleMode>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<SliceType>), () => new StyleValuePropertyBag<StyleEnum<SliceType>, SliceType>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<TextAnchor>), () => new StyleValuePropertyBag<StyleEnum<TextAnchor>, TextAnchor>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<TextGeneratorType>), () => new StyleValuePropertyBag<StyleEnum<TextGeneratorType>, TextGeneratorType>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<TextOverflow>), () => new StyleValuePropertyBag<StyleEnum<TextOverflow>, TextOverflow>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<EditorTextRenderingMode>), () => new StyleValuePropertyBag<StyleEnum<EditorTextRenderingMode>, EditorTextRenderingMode>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<TextOverflowPosition>), () => new StyleValuePropertyBag<StyleEnum<TextOverflowPosition>, TextOverflowPosition>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<TransformOriginOffset>), () => new StyleValuePropertyBag<StyleEnum<TransformOriginOffset>, TransformOriginOffset>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Visibility>), () => new StyleValuePropertyBag<StyleEnum<Visibility>, Visibility>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<WhiteSpace>), () => new StyleValuePropertyBag<StyleEnum<WhiteSpace>, WhiteSpace>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleEnum<Wrap>), () => new StyleValuePropertyBag<StyleEnum<Wrap>, Wrap>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleBackground), () => new StyleValuePropertyBag<StyleBackground, Background>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Length), () => new Length.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleBackgroundPosition), () => new StyleValuePropertyBag<StyleBackgroundPosition, BackgroundPosition>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(BackgroundPosition), () => new BackgroundPosition.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleBackgroundRepeat), () => new StyleValuePropertyBag<StyleBackgroundRepeat, BackgroundRepeat>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(BackgroundRepeat), () => new BackgroundRepeat.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleBackgroundSize), () => new StyleValuePropertyBag<StyleBackgroundSize, BackgroundSize>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(BackgroundSize), () => new BackgroundSize.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleColor), () => new StyleValuePropertyBag<StyleColor, Color>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleCursor), () => new StyleValuePropertyBag<StyleCursor, Cursor>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Cursor), () => new Cursor.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleFloat), () => new StyleValuePropertyBag<StyleFloat, float>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleFont), () => new StyleValuePropertyBag<StyleFont, Font>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleFontDefinition), () => new StyleValuePropertyBag<StyleFontDefinition, FontDefinition>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(FontDefinition), () => new FontDefinition.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleInt), () => new StyleValuePropertyBag<StyleInt, int>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleLength), () => new StyleValuePropertyBag<StyleLength, Length>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Background), () => new Background.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleList<EasingFunction>), () => new StyleValuePropertyBag<StyleList<EasingFunction>, List<EasingFunction>>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(EasingFunction), () => new EasingFunction.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(List<EasingFunction>), () => new ListPropertyBag<EasingFunction>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleList<StylePropertyName>), () => new StyleValuePropertyBag<StyleList<StylePropertyName>, List<StylePropertyName>>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StylePropertyName), () => new StylePropertyName.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(List<StylePropertyName>), () => new ListPropertyBag<StylePropertyName>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleList<TimeValue>), () => new StyleValuePropertyBag<StyleList<TimeValue>, List<TimeValue>>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(TimeValue), () => new TimeValue.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(List<TimeValue>), () => new ListPropertyBag<TimeValue>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleRotate), () => new StyleValuePropertyBag<StyleRotate, Rotate>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Rotate), () => new Rotate.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleRatio), () => new StyleValuePropertyBag<StyleRatio, Ratio>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Ratio), () => new Ratio.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Angle), () => new Angle.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleScale), () => new StyleValuePropertyBag<StyleScale, Scale>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Scale), () => new Scale.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleTransformOrigin), () => new StyleValuePropertyBag<StyleTransformOrigin, TransformOrigin>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(TransformOrigin), () => new TransformOrigin.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleTranslate), () => new StyleValuePropertyBag<StyleTranslate, Translate>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(Translate), () => new Translate.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleTextShadow), () => new StyleValuePropertyBag<StyleTextShadow, TextShadow>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(TextShadow), () => new TextShadow.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleTextAutoSize), () => new StyleValuePropertyBag<StyleTextAutoSize, TextAutoSize>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(TextAutoSize), () => new TextAutoSize.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleList<FilterFunction>), () => new StyleValuePropertyBag<StyleList<FilterFunction>, List<FilterFunction>>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(List<FilterFunction>), () => new ListPropertyBag<FilterFunction>());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(MaterialDefinition), () => new MaterialDefinition.PropertyBag());
            PropertyBagLazyInitialization.AddLazyRegistration(typeof(StyleMaterialDefinition), () => new StyleValuePropertyBag<StyleMaterialDefinition, MaterialDefinition>());
        }

        static void RegisterConverters()
        {
            UIToolkitConverters.Register();
        }
    }
}
