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
            materialDefinitionStyleField.RegisterCallback<MaterialDefinitionChangedEvent, MaterialDefinitionStyleField>(OnMaterialDefinitionChanged, materialDefinitionStyleField);
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

        void ApplyMaterialDefinitionChange(MaterialDefinition newMaterialDefinition, bool refreshField, VisualElement elementTarget)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(MaterialConstants.Material);
            styleProperty.SetMaterialDefinition(styleSheet, newMaterialDefinition);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(MaterialConstants.Material);
            NotifyStyleChanges(s_StyleChangeList, refreshField);

            if (!refreshField)
            {
                // Required to update the override status in the inspector.
                m_Inspector.UpdateFieldStatus(elementTarget, styleProperty);
            }
        }

        void OnMaterialSelected(MaterialSelectedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            ApplyMaterialDefinitionChange(new MaterialDefinition(evt.material), true, evt.elementTarget);
        }

        void OnMaterialDefinitionChanged(MaterialDefinitionChangedEvent evt, MaterialDefinitionStyleField materialStyleField)
        {
            ApplyMaterialDefinitionChange(evt.newMaterialDefinition, evt.refreshField, evt.elementTarget);
        }
    }
}
