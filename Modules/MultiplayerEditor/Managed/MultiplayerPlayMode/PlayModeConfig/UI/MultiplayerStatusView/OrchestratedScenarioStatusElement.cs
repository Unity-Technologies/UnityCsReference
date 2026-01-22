// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class OrchestratedScenarioStatusElement : VisualElement
    {
        const string k_Stylesheet = "Multiplayer/UI/PlaymodeStatusWindow.uss";
        const string k_StylesheetDark = "Multiplayer/UI/PlaymodeStatusWindowDark.uss";
        const string k_StylesheetLight = "Multiplayer/UI/PlaymodeStatusWindowLight.uss";
        const string k_InstanceListContainer = "status-list";
        internal const string k_HelpBoxName = "help-box";

        private OrchestratedScenario m_Scenario;

        public OrchestratedScenarioStatusElement(OrchestratedScenario scenario)
        {
            m_Scenario = scenario;
            CreateGUI();
        }

        private void AttachHelpBox(VisualElement container, string prompt, HelpBoxMessageType promptType)
        {
            var helpBox = new HelpBox(prompt,promptType) { name = k_HelpBoxName };
            helpBox.AddToClassList("help-box");
            helpBox.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            container.Add(helpBox);
            Add(container);
        }

        private void CreateGUI()
        {
            var container = new VisualElement() { name = k_InstanceListContainer };

            container.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            var stylesheet = EditorGUIUtility.isProSkin ? k_StylesheetDark : k_StylesheetLight;
            container.styleSheets.Add(EditorGUIUtility.LoadRequired(stylesheet) as StyleSheet);

            // Display warnings if the scenario configuration is invalid
            var isValid = m_Scenario.IsValid(out _);
            if (!isValid)
            {
                var prompt = "This Scenario is not setup properly. Use the Edit Configuration button to fix the issues.";
                AttachHelpBox(container, prompt, HelpBoxMessageType.Warning);
                Add(container);
                return;
            }

            var mainEditor = new VisualElement();
            var virtualEditors = new VisualElement();
            var otherInstances = new VisualElement();

            container.Add(mainEditor);
            container.Add(virtualEditors);
            container.Add(otherInstances);

            var instances = m_Scenario.Scenario.GetAllInstances();
            foreach (var instance in instances)
            {
                var instanceElement = new InstanceStatusElement(instance);

                // We separate them just to make sure they show up in that order
                switch (instance.Controller)
                {
                    case MainEditorController:
                        mainEditor.Add(instanceElement);
                        break;
                    case CloneEditorController:
                        virtualEditors.Add(instanceElement);
                        break;
                    default:
                        otherInstances.Add(instanceElement);
                        break;
                }
            }

            Add(container);
        }
    }
}
