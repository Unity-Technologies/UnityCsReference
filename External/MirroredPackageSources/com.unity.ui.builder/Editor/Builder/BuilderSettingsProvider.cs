using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderSettingsProvider : SettingsProvider
    {
        const string k_EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";
        const string k_DisableMouseWheelZoomingToggleName = "disable-mouse-wheel-zooming";
        const string k_EnableAbsolutePositionPlacementToggleName = "enable-absolute-position-placement";

        private VisualElement m_HelpVisualTree;
        private VisualTreeAsset m_BuilderTemplate;
        private VisualTreeAsset builderTemplate
        {
            get
            {
                if (m_BuilderTemplate == null)
                    m_BuilderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.SettingsUIPath + "/BuilderSettingsView.uxml");
                return m_BuilderTemplate;
            }
        }

        [SettingsProvider]
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new BuilderSettingsProvider();
        }

        public static string name => $"Project/{BuilderConstants.BuilderWindowTitle}";
        
        private bool HasSearchInterestHandler(string searchContext)
        {
            if (m_HelpVisualTree == null)
                m_HelpVisualTree = builderTemplate.CloneTree();
            foreach (var e in m_HelpVisualTree.Query<TextElement>().ToList())
            {
                if (e.text.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1)
                    return true;
            }

            return false;
        }

        public BuilderSettingsProvider() : base(name, SettingsScope.Project)
        {
            hasSearchInterestHandler = HasSearchInterestHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            builderTemplate.CloneTree(rootElement);

            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.SettingsUIPath + "/BuilderSettingsView.uss");
            rootElement.styleSheets.Add(styleSheet);

            var editorExtensionsModeToggle = rootElement.Q<Toggle>(k_EditorExtensionsModeToggleName);
            editorExtensionsModeToggle.SetValueWithoutNotify(BuilderProjectSettings.enableEditorExtensionModeByDefault);
            editorExtensionsModeToggle.RegisterValueChangedCallback(e =>
            {
                BuilderProjectSettings.enableEditorExtensionModeByDefault = e.newValue;
            });

            var zoomToggle = rootElement.Q<Toggle>(k_DisableMouseWheelZoomingToggleName);
            zoomToggle.SetValueWithoutNotify(BuilderProjectSettings.disableMouseWheelZooming);
            zoomToggle.RegisterValueChangedCallback(e =>
            {
                BuilderProjectSettings.disableMouseWheelZooming = e.newValue;
            });

            var absolutePlacementToggle = rootElement.Q<Toggle>(k_EnableAbsolutePositionPlacementToggleName);
            absolutePlacementToggle.SetValueWithoutNotify(BuilderProjectSettings.enableAbsolutePositionPlacement);
            absolutePlacementToggle.RegisterValueChangedCallback(e =>
            {
                BuilderProjectSettings.enableAbsolutePositionPlacement = e.newValue;
            });
            if (!Unsupported.IsDeveloperMode())
                absolutePlacementToggle.style.display = DisplayStyle.None;

            base.OnActivate(searchContext, rootElement);
        }
    }
}
