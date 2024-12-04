// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderEditorInitialization
    {
        [RegisterUxmlCache]
        public static void Register()
        {
            BackgroundPositionStyleField.UxmlSerializedData.Register();
            BackgroundRepeatStyleField.UxmlSerializedData.Register();
            BackgroundSizeStyleField.UxmlSerializedData.Register();
            BuilderPane.UxmlSerializedData.Register();
            CategoryDropdownField.UxmlSerializedData.Register();
            DimensionStyleField.UxmlSerializedData.Register();
            FieldStatusIndicator.UxmlSerializedData.Register();
            FoldoutField.UxmlSerializedData.Register();
            FontStyleStrip.UxmlSerializedData.Register();
            HelpBox.UxmlSerializedData.Register();
            IntegerStyleField.UxmlSerializedData.Register();
            ModalPopup.UxmlSerializedData.Register();
            MultiTypeField.UxmlSerializedData.Register();
            NumericStyleField.UxmlSerializedData.Register();
            PercentSlider.UxmlSerializedData.Register();
            PersistedFoldout.UxmlSerializedData.Register();
            RotateStyleField.UxmlSerializedData.Register();
            ScaleStyleField.UxmlSerializedData.Register();
            TextAlignStrip.UxmlSerializedData.Register();
            TextShadowStyleField.UxmlSerializedData.Register();
            TranslateStyleField.UxmlSerializedData.Register();
        }
    }
}
