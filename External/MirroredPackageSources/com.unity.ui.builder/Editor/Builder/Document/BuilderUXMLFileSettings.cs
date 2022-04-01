using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUXMLFileSettings
    {
        const string k_EditorExtensionModeAttributeName = "editor-extension-mode";

        bool m_EditorExtensionMode;
        readonly VisualElementAsset m_RootElementAsset;

        public bool editorExtensionMode
        {
            get => m_EditorExtensionMode;
            set
            {
                m_EditorExtensionMode = value;
                m_RootElementAsset?.SetAttributeValue(k_EditorExtensionModeAttributeName, m_EditorExtensionMode.ToString());
                var builderWindow = Builder.ActiveWindow;
                if (builderWindow != null)
                    builderWindow.toolbar?.InitCanvasTheme();
            }
        }

        public BuilderUXMLFileSettings(VisualTreeAsset visualTreeAsset)
        {
            m_RootElementAsset = visualTreeAsset.GetRootUXMLElement();

            RetrieveEditorExtensionModeSetting();
        }

        void RetrieveEditorExtensionModeSetting()
        {
            if (m_RootElementAsset != null && m_RootElementAsset.HasAttribute(k_EditorExtensionModeAttributeName))
                m_EditorExtensionMode = Convert.ToBoolean(m_RootElementAsset.GetAttributeValue(k_EditorExtensionModeAttributeName));
            else
                editorExtensionMode = BuilderProjectSettings.enableEditorExtensionModeByDefault;
        }
    }
}
