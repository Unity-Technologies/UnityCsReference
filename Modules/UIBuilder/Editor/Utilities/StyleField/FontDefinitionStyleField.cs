// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class FontDefinitionStyleField : MultiTypeField
    {
        [Serializable]
        public new class UxmlSerializedData : MultiTypeField.UxmlSerializedData
        {
            public override object CreateInstance() => new FontDefinitionStyleField();
        }

        const string k_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/FontDefinitionStyleField.uss";

        const string k_FieldInputName = "unity-visual-input";
        const string k_FontDefinitionStyleFieldContainerName = "unity-font-definition-style-field-container";
        const string k_FontDefinitionStyleFieldContainerClassName = "unity-font-definition-style-field__container";

        public FontDefinitionStyleField() : this(null) {}

        public FontDefinitionStyleField(string label) : base(label)
        {
            AddType(typeof(FontAsset), "Font Asset");
            AddType(typeof(Font), "Font");

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));
            var fieldContainer = new VisualElement {name = k_FontDefinitionStyleFieldContainerName};
            fieldContainer.AddToClassList(k_FontDefinitionStyleFieldContainerClassName);

            var fieldInput = this.Q(k_FieldInputName);
            // Move visual input over to field container
            fieldContainer.Add(fieldInput);

            Add(fieldContainer);
        }
    }
}
