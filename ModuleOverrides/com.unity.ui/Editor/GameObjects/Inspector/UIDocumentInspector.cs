// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(UIDocument))]
    internal class UIDocumentInspector : Editor
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/Inspector/UIDocumentInspector.uss";
        const string k_InspectorVisualTreeAssetPath = "UIPackageResources/UXML/Inspector/UIDocumentInspector.uxml";
        private const string k_StyleClassWithParentHidden = "unity-ui-document-inspector--with-parent--hidden";

        private static StyleSheet k_DefaultStyleSheet = null;

        private VisualElement m_RootVisualElement;

        private VisualTreeAsset m_InspectorUxml;

        private ObjectField m_PanelSettingsField;
        private ObjectField m_ParentField;
        private ObjectField m_SourceAssetField;

        private HelpBox m_DrivenByParentWarning;

        private FloatField m_SortOrderField;

        private void ConfigureFields()
        {
            // Using MandatoryQ instead of just Q to make sure modifications of the UXML file don't make the
            // necessary elements disappear unintentionally.
            m_DrivenByParentWarning = m_RootVisualElement.MandatoryQ<HelpBox>("drivenByParentWarning");

            m_PanelSettingsField = m_RootVisualElement.MandatoryQ<ObjectField>("panelSettingsField");
            m_PanelSettingsField.objectType = typeof(PanelSettings);

            m_ParentField = m_RootVisualElement.MandatoryQ<ObjectField>("parentField");
            m_ParentField.objectType = typeof(UIDocument);
            m_ParentField.SetEnabled(false);

            m_SourceAssetField = m_RootVisualElement.MandatoryQ<ObjectField>("sourceAssetField");
            m_SourceAssetField.objectType = typeof(VisualTreeAsset);

            m_SortOrderField = m_RootVisualElement.MandatoryQ<FloatField>("sortOrderField");
            m_SortOrderField.isDelayed = true;
        }

        private void BindFields()
        {
            m_ParentField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_PanelSettingsField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_SortOrderField.RegisterCallback<ChangeEvent<float>>(evt => SetSortOrder(evt));
        }

        private void UpdateValues()
        {
            UIDocument uiDocument = (UIDocument)target;
            bool isNotDrivenByParent = uiDocument.parentUI == null;

            m_DrivenByParentWarning.EnableInClassList(k_StyleClassWithParentHidden, isNotDrivenByParent);
            m_ParentField.EnableInClassList(k_StyleClassWithParentHidden, isNotDrivenByParent);

            m_PanelSettingsField.SetEnabled(isNotDrivenByParent);
        }

        private void SetSortOrder(ChangeEvent<float> evt)
        {
            // The field is bound, but we need to do extra operations when the value changes.
            UIDocument uiDocument = (UIDocument)target;

            if (uiDocument.sortingOrder != evt.newValue)
            {
                m_SortOrderField.schedule.Execute(() => uiDocument.ApplySortingOrder());
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (m_RootVisualElement == null)
            {
                m_RootVisualElement = new VisualElement();
            }
            else
            {
                m_RootVisualElement.Clear();
            }

            if (m_InspectorUxml == null)
            {
                m_InspectorUxml = EditorGUIUtility.Load(k_InspectorVisualTreeAssetPath) as VisualTreeAsset;
            }

            if (k_DefaultStyleSheet == null)
            {
                k_DefaultStyleSheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            }
            m_RootVisualElement.styleSheets.Add(k_DefaultStyleSheet);

            m_InspectorUxml.CloneTree(m_RootVisualElement);
            ConfigureFields();
            BindFields();
            UpdateValues();

            return m_RootVisualElement;
        }
    }
}
