// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class UIToolkitSettingsProvider : SettingsProvider
    {
        const string k_EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";
        const string k_DisableMouseWheelZoomingToggleName = "disable-mouse-wheel-zooming";
        const string k_EnableAbsolutePositionPlacementToggleName = "enable-absolute-position-placement";
        const string k_EnableEventDebugger = "enable-event-debugger";
        const string k_EnableLayoutDebugger = "enable-layout-debugger";
        const string k_EnableTextAdvanced = "enable-text-advanced";
        const string k_enableDebuggerLowLevelName = "enable-debugger-low-level";

        private VisualElement m_HelpVisualTree;
        private VisualTreeAsset m_UIToolkitTemplate;
        private VisualTreeAsset uiToolkitTemplate
        {
            get
            {
                if (m_UIToolkitTemplate == null)
                    m_UIToolkitTemplate = EditorGUIUtility.Load("UIPackageResources/Settings/UIToolkitSettingsView.uxml") as VisualTreeAsset;
                return m_UIToolkitTemplate;
            }
        }

        [SettingsProvider]
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new UIToolkitSettingsProvider();
        }

        public static string name => "Project/UI Toolkit";

        private bool HasSearchInterestHandler(string searchContext)
        {
            if (m_HelpVisualTree == null)
                m_HelpVisualTree = uiToolkitTemplate.CloneTree();
            foreach (var e in m_HelpVisualTree.Query<TextElement>().Build())
            {
                if (e.text.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }

            return false;
        }

        public UIToolkitSettingsProvider() : base(name, SettingsScope.Project)
        {
            hasSearchInterestHandler = HasSearchInterestHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            uiToolkitTemplate.CloneTree(rootElement);

            var styleSheet = EditorGUIUtility.Load("UIPackageResources/Settings/UIToolkitSettingsView.uss") as StyleSheet;
            rootElement.styleSheets.Add(styleSheet);

            var editorExtensionsModeToggle = rootElement.Q<Toggle>(k_EditorExtensionsModeToggleName);
            editorExtensionsModeToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableEditorExtensionModeByDefault);
            editorExtensionsModeToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableEditorExtensionModeByDefault = e.newValue;
            });

            var zoomToggle = rootElement.Q<Toggle>(k_DisableMouseWheelZoomingToggleName);
            zoomToggle.SetValueWithoutNotify(UIToolkitProjectSettings.disableMouseWheelZooming);
            zoomToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.disableMouseWheelZooming = e.newValue;
            });

            var absolutePlacementToggle = rootElement.Q<Toggle>(k_EnableAbsolutePositionPlacementToggleName);
            absolutePlacementToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableAbsolutePositionPlacement);
            absolutePlacementToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableAbsolutePositionPlacement = e.newValue;
            });
            if (!Unsupported.IsDeveloperMode())
            {
                VisualElement container = rootElement.Q("developer-settings-container");
                if (container != null)
                    container.style.display = DisplayStyle.None;
            }

            var eventDebuggerToggle = rootElement.Q<Toggle>(k_EnableEventDebugger);
            eventDebuggerToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableEventDebugger);
            eventDebuggerToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableEventDebugger = e.newValue;
            });

            var layoutDebuggerToggle = rootElement.Q<Toggle>(k_EnableLayoutDebugger);
            layoutDebuggerToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableLayoutDebugger);
            layoutDebuggerToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableLayoutDebugger = e.newValue;
            });

            var enableTextAdvancedToggle = rootElement.Q<Toggle>(k_EnableTextAdvanced);
            enableTextAdvancedToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableAdvancedText);
            enableTextAdvancedToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableAdvancedText = e.newValue;
            });

            var enableDebuggerLowLevelToggle = rootElement.Q<Toggle>(k_enableDebuggerLowLevelName);
            enableDebuggerLowLevelToggle.SetValueWithoutNotify(UIToolkitProjectSettings.EnableLowLevelDebugger);
            enableDebuggerLowLevelToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.EnableLowLevelDebugger = e.newValue;
            });

            base.OnActivate(searchContext, rootElement);
        }
    }
}
