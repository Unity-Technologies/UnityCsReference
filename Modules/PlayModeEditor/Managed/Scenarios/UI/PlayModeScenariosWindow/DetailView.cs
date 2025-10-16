// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Shows one configuration that the user can change
    /// </summary>
    class DetailView : ScrollView
    {
        const string k_Stylesheet = "PlayMode/UI/Framework.uss";
        internal const string k_DetailedViewName = "playmodeconfig-detail-view";

        PlayModeScenario m_Config;
        VisualElement m_ConfigEditor;
        readonly VisualElement m_NoConfigSelectedView;

        public DetailView()
        {
            name = k_DetailedViewName;
            m_NoConfigSelectedView = NoConfigSelectedView();
            Add(m_NoConfigSelectedView);
            styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        }

        internal void SetConfig(PlayModeScenario config)
        {
            m_Config = config;
            if (m_Config != null)
            {
                m_NoConfigSelectedView.style.display = DisplayStyle.None;
                UpdateView();
                return;
            }

            if (m_ConfigEditor != null)
                m_ConfigEditor.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            m_NoConfigSelectedView.style.display = DisplayStyle.Flex;
        }

        internal void UpdateView()
        {
            // If nothing is selected, no need to update.
            if (m_Config == null)
                return;

            var so = new SerializedObject(m_Config);

            if (m_ConfigEditor == null)
            {
                m_ConfigEditor = new InspectorElement(so);
                Add(m_ConfigEditor);
            }
            m_ConfigEditor.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            m_ConfigEditor.Unbind();
            m_ConfigEditor.Bind(so);
        }

        VisualElement NoConfigSelectedView()
        {
            var container = new VisualElement();
            container.name = "no-config-selected-view";
            container.AddToClassList("unity-scenarios-detailed-view__no-config-selected-view");
            container.Add(new Label("No configuration selected"));
            return container;
        }
    }
}
