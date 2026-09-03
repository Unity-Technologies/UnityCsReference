// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Adds the experimental accessibility toggle to the Project Settings &gt; UI Toolkit page.
    /// </summary>
    [UsedImplicitly]
    internal class UIToolkitAccessibilitySettingsProviderExtension : IUIToolkitSettingsProviderExtension
    {
        const string k_SectionHeaderText = "Accessibility";
        const string k_ExperimentalTagText = "Experimental";
        const string k_ToggleText = "Generate Accessibility Hierarchies for Runtime Panels";
        const string k_ToggleTooltip =
            "Automatically generate and keep in sync the accessibility hierarchies that screen readers use to read " +
            "and operate runtime UI Toolkit content. Applies in Play mode and ships with player builds.";
        const string k_HelpText =
            "When enabled, UI Toolkit automatically generates and maintains accessibility hierarchies visible to " +
            "screen readers for the runtime panels of the application's main window. Individual UI Document and " +
            "Panel Renderer components can be opted out in their Inspector. The generated hierarchy can be inspected " +
            "in the Accessibility Hierarchy Viewer during Play mode.\n\nThis feature is supported on Android, iOS, " +
            "macOS, and Windows. It is experimental and not ready for production use.";
        const string k_HelpButtonText = "Open Accessibility Hierarchy Viewer";
        const string k_HierarchyViewerMenuPath = "Window/Accessibility/Hierarchy Viewer";

        public int order => 200;

        public bool HasSearchInterestHandler(string searchContext)
        {
            return k_SectionHeaderText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1 ||
                k_ToggleText.IndexOf(searchContext, System.StringComparison.OrdinalIgnoreCase) != -1;
        }

        public void OnActivate(string searchContext, VisualElement rootElement)
        {
            var header = new Label(k_SectionHeaderText);
            header.AddToClassList("uitoolkit-settings-header");
            header.style.paddingTop = 20;
            rootElement.Add(header);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var toggle = new Toggle { text = k_ToggleText, tooltip = k_ToggleTooltip };
            toggle.SetValueWithoutNotify(UIToolkitAccessibilitySettingsEditor.generateHierarchies);
            toggle.RegisterValueChangedCallback(evt =>
            {
                UIToolkitAccessibilitySettingsEditor.generateHierarchies = evt.newValue;
            });
            row.Add(toggle);

            var experimentalTag = new Label(k_ExperimentalTagText);
            experimentalTag.AddToClassList("unity-experimental-tag");
            row.Add(experimentalTag);

            rootElement.Add(row);

            // This section sits at the bottom of the page; the other sections get their breathing
            // room from the next header's top padding, which nothing provides here. The button
            // opens the viewer through its menu path: the window lives in the Accessibility editor
            // module, which this module does not reference.
            var helpBox = new HelpBox(k_HelpText, HelpBoxMessageType.Info) { buttonText = k_HelpButtonText };
            helpBox.onButtonClicked += () => EditorApplication.ExecuteMenuItem(k_HierarchyViewerMenuPath);
            helpBox.style.marginBottom = 20;
            rootElement.Add(helpBox);
        }

        public void OnDeactivate()
        {
        }
    }
}
