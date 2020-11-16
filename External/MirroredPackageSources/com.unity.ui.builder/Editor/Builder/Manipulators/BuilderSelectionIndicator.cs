using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderSelectionIndicator : BuilderTracker
    {
        static readonly string s_UssClassName = "unity-builder-selection-indicator";
        private VisualElement m_Header;
        private Label m_HeaderLabel;

        public new class UxmlFactory : UxmlFactory<BuilderSelectionIndicator, UxmlTraits> { }

        public BuilderSelectionIndicator()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Manipulators/BuilderSelectionIndicator.uxml");
            builderTemplate.CloneTree(this);

            AddToClassList(s_UssClassName);
            m_Header = this.Q("header");
            m_HeaderLabel = this.Q<Label>("header-label");
        }

        public override void Activate(VisualElement element)
        {
            base.Activate(element);
            UpdateLabel();
        }

        public void OnHierarchyChanged(VisualElement element)
        {
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (m_Target == null)
                return;

            if (string.IsNullOrEmpty(m_Target.name))
            {
                m_HeaderLabel.text = m_Target.typeName;
            }
            else
            {
                m_HeaderLabel.text = BuilderConstants.UssSelectorNameSymbol + m_Target.name;
            }
        }
    }
}
