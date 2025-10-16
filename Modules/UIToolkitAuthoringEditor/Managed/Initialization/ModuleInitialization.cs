// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal static class ModuleInitialization
    {
        [RegisterUxmlCache]
        public static void Register()
        {
            AlignStyleEnumField.UxmlSerializedData.Register();
            BackgroundPositionStyleField.UxmlSerializedData.Register();
            BackgroundRepeatStyleField.UxmlSerializedData.Register();
            BackgroundSizeStyleField.UxmlSerializedData.Register();
            BorderBoxModelField.UxmlSerializedData.Register();
            BorderColorFoldout.UxmlSerializedData.Register();
            BorderRadiusFoldout.UxmlSerializedData.Register();
            BorderWidthFoldout.UxmlSerializedData.Register();
            DisplayStyleEnumField.UxmlSerializedData.Register();
            EditorTextRenderingModeStyleEnumField.UxmlSerializedData.Register();
            FlexDirectionStyleEnumField.UxmlSerializedData.Register();
            FontStyleStyleEnumField.UxmlSerializedData.Register();
            FontStyleToggleField.UxmlSerializedData.Register();
            JustifyStyleEnumField.UxmlSerializedData.Register();
            LengthFoldoutField.UxmlSerializedData.Register();
            OverflowStyleEnumField.UxmlSerializedData.Register();
            OverrideFoldout.UxmlSerializedData.Register();
            OverrideRow.UxmlSerializedData.Register();
            PercentSlider.UxmlSerializedData.Register();
            PositionStyleEnumField.UxmlSerializedData.Register();
            SpacingBoxModelField.UxmlSerializedData.Register();
            StyleBackgroundField.UxmlSerializedData.Register();
            StyleBackgroundPositionField.UxmlSerializedData.Register();
            StyleBackgroundRepeatField.UxmlSerializedData.Register();
            StyleBackgroundSizeField.UxmlSerializedData.Register();
            StyleColorField.UxmlSerializedData.Register();
            StyleCursorField.UxmlSerializedData.Register();
            StyleFloatField.UxmlSerializedData.Register();
            StyleFontDefinitionField.UxmlSerializedData.Register();
            StyleFontField.UxmlSerializedData.Register();
            StyleIntField.UxmlSerializedData.Register();
            StyleLengthField.UxmlSerializedData.Register();
            StylePercentSliderField.UxmlSerializedData.Register();
            StyleRotateField.UxmlSerializedData.Register();
            StyleScaleField.UxmlSerializedData.Register();
            StyleTextShadowField.UxmlSerializedData.Register();
            StyleTransformOriginField.UxmlSerializedData.Register();
            StyleTransitionListView.UxmlSerializedData.Register();
            StyleTranslateField.UxmlSerializedData.Register();
            TextAlignStyleEnumField.UxmlSerializedData.Register();
            TextAlignToggleField.UxmlSerializedData.Register();
            TextGeneratorTypeStyleEnumField.UxmlSerializedData.Register();
            TextOverflowPositionStyleEnumField.UxmlSerializedData.Register();
            TextOverflowStyleEnumField.UxmlSerializedData.Register();
            UnityTextAlignStyleEnumField.UxmlSerializedData.Register();
            VisibilityStyleEnumField.UxmlSerializedData.Register();
            WhiteSpaceStyleEnumField.UxmlSerializedData.Register();
            WrapStyleEnumField.UxmlSerializedData.Register();
        }
    }
}
