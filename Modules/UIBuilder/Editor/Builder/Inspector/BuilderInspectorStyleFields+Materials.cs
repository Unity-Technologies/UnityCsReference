// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    partial class BuilderInspectorStyleFields
    {
        static class MaterialConstants
        {
            public static readonly string Material = StylePropertyId.UnityMaterial.UssName();
        }

        public void BindStyleField(BuilderStyleRow styleRow, MaterialDefinitionStyleField materialDefinitionStyleField)
        {
            materialDefinitionStyleField.SetContainingRow(styleRow);

            materialDefinitionStyleField.SetInspectorStylePropertyName(MaterialConstants.Material);
            GetOrCreateFieldListForStyleName(MaterialConstants.Material).Add(materialDefinitionStyleField);

            SetUpContextualMenuOnStyleField(materialDefinitionStyleField);

            materialDefinitionStyleField.RegisterCallback<MaterialSelectedEvent, MaterialDefinitionStyleField>(OnMaterialSelected, materialDefinitionStyleField);
            materialDefinitionStyleField.RegisterCallback<MaterialPropertyAddedEvent, MaterialDefinitionStyleField>(OnMaterialPropertyAdded, materialDefinitionStyleField);
            materialDefinitionStyleField.RegisterCallback<MaterialPropertyChangedEvent, MaterialDefinitionStyleField>(OnMaterialPropertyChanged, materialDefinitionStyleField);
            materialDefinitionStyleField.RegisterCallback<MaterialPropertyRemovedEvent, MaterialDefinitionStyleField>(OnMaterialPropertyRemoved, materialDefinitionStyleField);
        }

        public void RefreshStyleField(MaterialDefinitionStyleField materialDefinitionStyleField)
        {
            // It's important to cancel any running animation so that when we query the computed style
            // we get the new number of material properties. Otherwise, the first frame of any transition will
            // have the old number of material properties before the add/remove of the property.

            if (currentVisualElement.HasRunningAnimation(StylePropertyId.UnityMaterial))
                currentVisualElement.CancelAnimation(StylePropertyId.UnityMaterial);

            materialDefinitionStyleField.SetValueWithoutNotify(currentVisualElement.resolvedStyle.unityMaterial);

            var prop = GetLastStyleProperty(currentRule, MaterialConstants.Material);
            m_Inspector.UpdateFieldStatus(materialDefinitionStyleField, prop);
        }

        void OnMaterialSelected(MaterialSelectedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            var styleProperty = GetOrCreateStylePropertyByStyleName(MaterialConstants.Material);

            var matDef = new MaterialDefinition(evt.material);
            styleProperty.SetMaterialDefinition(styleSheet, matDef);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(MaterialConstants.Material);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void OnMaterialPropertyAdded(MaterialPropertyAddedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(MaterialConstants.Material);
            var manip = styleProperty.GetManipulator(styleSheet);
            manip.AddMaterialPropertyValue(evt.materialPropertyValue);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(MaterialConstants.Material);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void OnMaterialPropertyChanged(MaterialPropertyChangedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(MaterialConstants.Material);
            var manip = styleProperty.GetManipulator(styleSheet);
            manip.SetMaterialPropertyValue(evt.propertyIndex, evt.materialPropertyValue);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(MaterialConstants.Material);
            NotifyStyleChanges(s_StyleChangeList, false);
        }

        void OnMaterialPropertyRemoved(MaterialPropertyRemovedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Remove duplicates, and sort indices in descending order
            var uniqueIndices = new HashSet<int>(evt.indices);
            var sortedIndices = new List<int>(uniqueIndices);
            sortedIndices.Sort((a, b) => b.CompareTo(a)); // Sort in descending order

            var styleProperty = GetOrCreateStylePropertyByStyleName(MaterialConstants.Material);
            var manip = styleProperty.GetManipulator(styleSheet);
            foreach (var index in sortedIndices)
                manip.RemoveValue(index + 1); // +1 to skip the material itself

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(MaterialConstants.Material);
            NotifyStyleChanges(s_StyleChangeList, true);
        }
    }
}
