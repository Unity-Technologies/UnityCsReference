// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    [UxmlElement]
    partial class FontDefinitionStyleField : MultiTypeField
    {
        const string k_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/FontDefinitionStyleField.uss";

        const string k_FieldInputName = "unity-visual-input";
        const string k_FontDefinitionStyleFieldContainerClassName = "unity-font-definition-style-field__container";

        public FontDefinitionStyleField() : this(null) {}

        public FontDefinitionStyleField(string label) : base(label, new VisualElement())
        {
            AddType(typeof(FontAsset), "Font Asset");
            AddType(typeof(Font), "Font");

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));

            var fieldInput = this.Q(k_FieldInputName);
            visualInput.Add(fieldInput);

            Add(visualInput);
            visualInput.name = StyleField<int>.VisualInputName;
            visualInput.AddToClassList(k_FontDefinitionStyleFieldContainerClassName);

            objectField.objectFieldDisplay.RegisterDefaultDragAndDrop(new List<Type>() {typeof(FontAsset), typeof(Font)});
        }
    }
}
