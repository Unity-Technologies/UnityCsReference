// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                m_RootElementAsset?.SetAttribute(k_EditorExtensionModeAttributeName, m_EditorExtensionMode.ToString());
                var builderWindow = Builder.ActiveWindow;
                if (builderWindow != null)
                    builderWindow.toolbar?.InitCanvasTheme();
            }
        }

        public BuilderUXMLFileSettings(VisualTreeAsset visualTreeAsset)
        {
            m_RootElementAsset = visualTreeAsset.GetRootUxmlElement();

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
