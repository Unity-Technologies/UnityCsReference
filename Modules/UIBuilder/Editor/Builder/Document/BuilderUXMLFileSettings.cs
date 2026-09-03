// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUXMLFileSettings
    {
        const string k_EditorExtensionModeAttributeName = "editor-extension-mode";

        bool m_EditorExtensionMode;
        VisualElementAsset m_RootElementAsset;
        BuilderDocument m_Document;

        public bool editorExtensionMode
        {
            get => m_EditorExtensionMode;
            set
            {
                m_EditorExtensionMode = value;
                m_RootElementAsset?.SetAttribute(k_EditorExtensionModeAttributeName, m_EditorExtensionMode.ToString());
                var builderWindow = m_Document?.primaryViewportWindow as Builder;
                if (builderWindow != null)
                    builderWindow.toolbar?.InitCanvasTheme();
            }
        }

        public BuilderUXMLFileSettings(VisualTreeAsset visualTreeAsset, BuilderDocument document)
        {
            m_Document = document;
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            SetRootElementAsset(visualTreeAsset);
            #pragma warning restore UAL0015
        }

        void RetrieveEditorExtensionModeSetting()
        {
            if (m_RootElementAsset != null && m_RootElementAsset.HasAttribute(k_EditorExtensionModeAttributeName))
                m_EditorExtensionMode = Convert.ToBoolean(m_RootElementAsset.GetAttributeValue(k_EditorExtensionModeAttributeName));
            else
                editorExtensionMode = BuilderProjectSettings.enableEditorExtensionModeByDefault;
        }

        internal void SetRootElementAsset(VisualTreeAsset visualTreeAsset)
        {
            m_RootElementAsset = visualTreeAsset.visualTree;

            RetrieveEditorExtensionModeSetting();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
