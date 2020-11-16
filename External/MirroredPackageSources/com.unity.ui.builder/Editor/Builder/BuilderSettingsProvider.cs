using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderSettingsProvider : SettingsProvider
    {
        const string k_EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";
        const string k_DisableMouseWheelZoomingToggleName = "disable-mouse-wheel-zooming";

        [SettingsProvider]
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new BuilderSettingsProvider();
        }

        public static string name => $"Project/{BuilderConstants.BuilderWindowTitle}";
        bool HasSearchInterestHandler(string searchContext) => true;

        public BuilderSettingsProvider() : base(name, SettingsScope.Project)
        {
            hasSearchInterestHandler = HasSearchInterestHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.SettingsUIPath + "/BuilderSettingsView.uxml");
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

            base.OnActivate(searchContext, rootElement);
        }
    }
}
