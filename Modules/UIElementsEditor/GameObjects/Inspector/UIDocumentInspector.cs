// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;
using System;
using UnityEditor.UIElements.GameObjects;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Inspector
{
    [CustomEditor(typeof(UIDocument))]
    internal class UIDocumentInspector : Editor
    {
        const string k_DefaultStyleSheetPath = "UIPackageResources/StyleSheets/Inspector/UIDocumentInspector.uss";
        const string k_InspectorVisualTreeAssetPath = "UIPackageResources/UXML/Inspector/UIDocumentInspector.uxml";
        private const string k_StyleClassWithParentHidden = "unity-ui-document-inspector--with-parent--hidden";
        private const string k_StyleClassPanelMissing = "unity-ui-document-inspector--panel-missing--hidden";

        private static StyleSheet s_DefaultStyleSheet;
        private static VisualTreeAsset s_InspectorUxml;

        private VisualElement m_RootVisualElement;

        private ObjectField m_PanelSettingsField;
        private ObjectField m_ParentField;
        private ObjectField m_SourceAssetField;

        private Foldout m_WorldSpaceDimensionsFoldout;
        private EnumField m_WorldSpaceSizeField;
        private VisualElement m_WorldSpaceWidthField;
        private VisualElement m_WorldSpaceHeightField;

        private HelpBox m_DrivenByParentWarning;
        private HelpBox m_MissingPanelSettings;
        private VisualElement m_InputConfiguration;

        private void ConfigureFields()
        {
            // Using MandatoryQ instead of just Q to make sure modifications of the UXML file don't make the
            // necessary elements disappear unintentionally.
            m_DrivenByParentWarning = m_RootVisualElement.MandatoryQ<HelpBox>("driven-by-parent-warning");
            m_MissingPanelSettings = m_RootVisualElement.MandatoryQ<HelpBox>("missing-panel-warning");

            m_PanelSettingsField = m_RootVisualElement.MandatoryQ<ObjectField>("panel-settings-field");
            m_PanelSettingsField.objectType = typeof(PanelSettings);

            m_ParentField = m_RootVisualElement.MandatoryQ<ObjectField>("parent-field");
            m_ParentField.objectType = typeof(UIDocument);
            m_ParentField.SetEnabled(false);

            m_SourceAssetField = m_RootVisualElement.MandatoryQ<ObjectField>("source-asset-field");
            m_SourceAssetField.objectType = typeof(VisualTreeAsset);

            m_WorldSpaceDimensionsFoldout = m_RootVisualElement.MandatoryQ<Foldout>("world-space-dimensions");
            m_WorldSpaceSizeField = m_RootVisualElement.MandatoryQ<EnumField>("size-mode");
            m_WorldSpaceWidthField = m_RootVisualElement.MandatoryQ<VisualElement>("width-field");
            m_WorldSpaceHeightField = m_RootVisualElement.MandatoryQ<VisualElement>("height-field");
            m_WorldSpaceDimensionsFoldout.style.display = DisplayStyle.None;

            m_InputConfiguration = m_RootVisualElement.MandatoryQ<VisualElement>("input-configuration");
            m_InputConfiguration.style.display = DisplayStyle.None;
            m_InputConfiguration.MandatoryQ<Button>("input-configuration-create").clicked += () =>
            {
                if (PanelInputConfiguration.current == null)
                    PlayModeMenuItems.AddPanelInputConfiguration();
            };
        }

        private void BindFields()
        {
            m_ParentField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_PanelSettingsField.RegisterCallback<ChangeEvent<Object>>(evt => UpdateValues());
            m_WorldSpaceSizeField.RegisterCallback<ChangeEvent<Enum>>(evt => UpdateValues());
        }

        private void UpdateValues()
        {
            UIDocument uiDocument = (UIDocument)target;
            bool isNotDrivenByParent = uiDocument.parentUI == null;

            m_DrivenByParentWarning.EnableInClassList(k_StyleClassWithParentHidden, isNotDrivenByParent);
            m_ParentField.EnableInClassList(k_StyleClassWithParentHidden, isNotDrivenByParent);

            bool displayPanelMissing = !(isNotDrivenByParent && uiDocument.panelSettings == null);
            m_MissingPanelSettings.EnableInClassList(k_StyleClassPanelMissing, displayPanelMissing);

            m_PanelSettingsField.SetEnabled(isNotDrivenByParent);

            m_WorldSpaceDimensionsFoldout.style.display = (uiDocument.panelSettings?.renderMode == PanelRenderMode.WorldSpace) ? DisplayStyle.Flex : DisplayStyle.None;

            bool isFixedSize = (uiDocument.worldSpaceSizeMode == UIDocument.WorldSpaceSizeMode.Fixed);
            var display = isFixedSize ? DisplayStyle.Flex : DisplayStyle.None;
            m_WorldSpaceWidthField.style.display = display;
            m_WorldSpaceHeightField.style.display = display;
        }

        private void UpdateInputConfigurationOptions()
        {
            bool hasNoConfig = PanelInputConfiguration.s_ActiveInstances == 0;
            bool isWorldSpace = ((UIDocument)target).panelSettings?.renderMode == PanelRenderMode.WorldSpace;
            m_InputConfiguration.style.display = hasNoConfig && isWorldSpace ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_RootVisualElement = new VisualElement();

            if (s_InspectorUxml == null)
            {
                s_InspectorUxml = EditorGUIUtility.Load(k_InspectorVisualTreeAssetPath) as VisualTreeAsset;
            }

            if (s_DefaultStyleSheet == null)
            {
                s_DefaultStyleSheet = EditorGUIUtility.Load(k_DefaultStyleSheetPath) as StyleSheet;
            }
            m_RootVisualElement.styleSheets.Add(s_DefaultStyleSheet);

            s_InspectorUxml.CloneTree(m_RootVisualElement);
            ConfigureFields();
            BindFields();
            UpdateValues();

            UpdateInputConfigurationOptions();
            m_InputConfiguration.schedule.Execute(UpdateInputConfigurationOptions).Every(200);

            return m_RootVisualElement;
        }
    }
}
