// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.Pool;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal interface IUIToolkitSettingsProviderExtension
    {
        int order { get; }
        bool HasSearchInterestHandler(string searchContext);
        void OnActivate(string searchContext, VisualElement rootElement);
        void OnDeactivate();
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class UIToolkitSettingsProvider : SettingsProvider
    {
        const string k_EditorExtensionsModeToggleName = "editor-extensions-mode-toggle";
        const string k_DisableMouseWheelZoomingToggleName = "disable-mouse-wheel-zooming";
        const string k_EnableAbsolutePositionPlacementToggleName = "enable-absolute-position-placement";
        const string k_ConsistentAttributeOrdering = "consistent-attribute-ordering-toggle";
        const string k_EnableEventDebugger = "enable-event-debugger";
        const string k_EnableLayoutDebugger = "enable-layout-debugger";
        const string k_EnableTextAdvanced = "enable-text-advanced";
        const string k_EnableDebuggerLowLevelName = "enable-debugger-low-level";
        const string k_DefaultRuntimeTheme = "default-runtime-theme";
        const string k_DefaultEditorTheme = "default-editor-theme";

        private static readonly List<Type> s_ExtensionTypes = new();

        [InitializeOnLoadMethod, UsedImplicitly]
        private static void GetExtensionTypes()
        {
            foreach (var extension in TypeCache.GetTypesDerivedFrom<IUIToolkitSettingsProviderExtension>())
            {
                if (extension.IsInterface)
                    continue;

                if (extension.IsAbstract || extension.IsGenericType)
                    continue;

                if (extension.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                s_ExtensionTypes.Add(extension);
            }
        }

        private readonly List<IUIToolkitSettingsProviderExtension> m_Extensions = new ();

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

            foreach (var extension in m_Extensions)
            {
                if (extension.HasSearchInterestHandler(searchContext))
                    return true;
            }

            return false;
        }

        public UIToolkitSettingsProvider() : base(name, SettingsScope.Project)
        {
            hasSearchInterestHandler = HasSearchInterestHandler;
            foreach (var extension in s_ExtensionTypes)
                m_Extensions.Add((IUIToolkitSettingsProviderExtension)Activator.CreateInstance(extension));
            m_Extensions.Sort((lhs, rhs) => lhs.order.CompareTo(rhs.order));
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

            var consistentAttributeOrderingToggle = rootElement.Q<Toggle>(k_ConsistentAttributeOrdering);
            consistentAttributeOrderingToggle.SetValueWithoutNotify(UIToolkitProjectSettings.consistentAttributeOrderingWhenExporting);
            consistentAttributeOrderingToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.consistentAttributeOrderingWhenExporting = e.newValue;
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

            var enableDebuggerLowLevelToggle = rootElement.Q<Toggle>(k_EnableDebuggerLowLevelName);
            enableDebuggerLowLevelToggle.SetValueWithoutNotify(UIToolkitProjectSettings.EnableLowLevelDebugger);
            enableDebuggerLowLevelToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.EnableLowLevelDebugger = e.newValue;
            });

            var defaultRuntimeThemeMenu = rootElement.Q<ProjectSettingsThemeDropdown>(k_DefaultRuntimeTheme);

            // Callback for runtime theme changes
            Action<CanvasTheme, ThemeStyleSheet> onRuntimeThemeSelected = null;
            onRuntimeThemeSelected = (canvasTheme, themeAsset) =>
            {
                UIToolkitProjectSettings.defaultRuntimeCanvasTheme = canvasTheme;
                UIToolkitProjectSettings.defaultRuntimeTheme = themeAsset;

                // Re-populate the menu to update ordering (selected theme moves to top)
                PopulateThemeMenu(defaultRuntimeThemeMenu, true, onRuntimeThemeSelected);
            };
            PopulateThemeMenu(defaultRuntimeThemeMenu, true, onRuntimeThemeSelected);

            var defaultEditorThemeMenu = rootElement.Q<ProjectSettingsThemeDropdown>(k_DefaultEditorTheme);

            // Callback for editor theme changes
            Action<CanvasTheme, ThemeStyleSheet> onEditorThemeSelected = null;
            onEditorThemeSelected = (canvasTheme, themeAsset) =>
            {
                UIToolkitProjectSettings.defaultEditorCanvasTheme = canvasTheme;
                UIToolkitProjectSettings.defaultEditorTheme = themeAsset;

                // Re-populate the menu to update ordering (selected theme moves to top)
                PopulateThemeMenu(defaultEditorThemeMenu, false, onEditorThemeSelected);
            };
            PopulateThemeMenu(defaultEditorThemeMenu, false, onEditorThemeSelected);

            var localRoot = rootElement.Q<VisualElement>(className: "uitoolkit-settings-container");

            foreach(var extension in m_Extensions)
                extension.OnActivate(searchContext, localRoot);

            base.OnActivate(searchContext, rootElement);
        }

        public override void OnDeactivate()
        {
            foreach(var extension in m_Extensions)
                extension.OnDeactivate();
            base.OnDeactivate();
        }

        /// Populates a Custom Dropdown with theme choices, showing selected theme first with separator.
        private static void PopulateThemeMenu(ProjectSettingsThemeDropdown menu, bool isRuntimeTheme, Action<CanvasTheme, ThemeStyleSheet> onThemeSelected)
        {
            // Get theme files list
            var themeFiles = new SortedSet<string>();
            ThemeUtility.GetRuntimeThemeFiles(themeFiles);

            var (selectedCanvasTheme, selectedTheme) = ThemeUtility.GetProjectDefaultTheme(!isRuntimeTheme, themeFiles);

            // Build choices list and dictionary
            var choices = new List<string>();
            var themeData = new Dictionary<string, (CanvasTheme, ThemeStyleSheet)>();

            // Add selected theme first
            string selectedDisplayName = null;
            if (selectedTheme != null)
            {
                selectedDisplayName = AddSelectedThemeToChoices(choices, themeData, selectedTheme, themeFiles, isRuntimeTheme);
            }
            else if (!isRuntimeTheme)
            {
                // For built-in editor themes without a custom asset
                selectedDisplayName = AddSelectedEditorCanvasThemeToChoices(choices, themeData, selectedCanvasTheme);
            }

            // Add separator after selected theme
            choices.Add(ProjectSettingsThemeDropdown.k_Separator);

            // Add remaining themes
            if (!isRuntimeTheme)
            {
                AddEditorThemesToChoices(choices, themeData, selectedCanvasTheme);
            }
            AddRuntimeThemesToChoices(choices, themeData, themeFiles, selectedTheme, isRuntimeTheme);

            // Set choices and initial value
            menu.choices = choices;
            menu.SetValueWithoutNotify(selectedDisplayName);

            // Register single callback for all selections
            menu.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == ProjectSettingsThemeDropdown.k_Separator)
                    return; // Ignore separator

                if (themeData.TryGetValue(evt.newValue, out var themeInfo))
                {
                    var (canvasTheme, themeSheet) = themeInfo;
                    onThemeSelected?.Invoke(canvasTheme, themeSheet);
                }
            });
        }

        /// <summary>
        /// Adds the currently selected theme to the choices list and returns its display name.
        /// </summary>
        private static string AddSelectedThemeToChoices(List<string> choices, Dictionary<string, (CanvasTheme, ThemeStyleSheet)> themeData,
            ThemeStyleSheet selectedTheme, SortedSet<string> themeFiles, bool isRuntimeTheme)
        {
            string selectedDisplayName = ThemeUtility.NicifyThemeName(selectedTheme);

            // Add "(Default)" suffix for runtime themes (runtime mode)
            if (isRuntimeTheme)
            {
                var runtimeDefault = ThemeUtility.FindProjectDefaultRuntimeThemeAssetOrDefault(themeFiles);
                if (selectedTheme == runtimeDefault)
                    selectedDisplayName += ThemeUtility.DefaultThemeSuffix;
            }

            choices.Add(selectedDisplayName);
            themeData[selectedDisplayName] = (CanvasTheme.Custom, selectedTheme);

            return selectedDisplayName;
        }

        /// <summary>
        /// Adds the selected editor canvas theme (when no custom asset is selected) and returns its display name.
        /// </summary>
        private static string AddSelectedEditorCanvasThemeToChoices(List<string> choices, Dictionary<string, (CanvasTheme, ThemeStyleSheet)> themeData,
            CanvasTheme canvasTheme)
        {
            // Fallback to Default
            if (canvasTheme == CanvasTheme.ProjectSettings)
                canvasTheme = CanvasTheme.Default;

            var themeText = ThemeUtility.GetEditorThemeText(canvasTheme);
            var displayName = themeText;

            if (canvasTheme == CanvasTheme.Default)
                displayName += ThemeUtility.DefaultThemeSuffix;

            choices.Add(displayName);
            themeData[displayName] = (canvasTheme, null);

            return displayName;
        }

        /// <summary>
        /// Adds editor theme options (Default, Dark, Light) to the choices list.
        /// </summary>
        private static void AddEditorThemesToChoices(List<string> choices, Dictionary<string, (CanvasTheme, ThemeStyleSheet)> themeData,
            CanvasTheme currentTheme)
        {
            CanvasTheme[] editorThemes = [CanvasTheme.Default, CanvasTheme.Dark, CanvasTheme.Light];

            foreach (var editorTheme in editorThemes)
            {
                // Skip the currently selected theme (already added)
                if (editorTheme == currentTheme)
                    continue;

                var displayName = ThemeUtility.GetEditorThemeText(editorTheme);
                if (editorTheme == CanvasTheme.Default)
                {
                    displayName += ThemeUtility.DefaultThemeSuffix;
                }

                choices.Add(displayName);
                themeData[displayName] = (editorTheme, null);
            }

            choices.Add(ProjectSettingsThemeDropdown.k_Separator);
        }

        /// <summary>
        /// Adds runtime theme options to the choices list.
        /// </summary>
        private static void AddRuntimeThemesToChoices(List<string> choices, Dictionary<string, (CanvasTheme, ThemeStyleSheet)> themeData,
            SortedSet<string> themeFiles, ThemeStyleSheet selectedTheme, bool isRuntimeTheme)
        {
            var runtimeDefault = ThemeUtility.FindProjectDefaultRuntimeThemeAssetOrDefault(themeFiles);

            foreach (var themeFile in themeFiles)
            {
                var themeAsset = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFile);
                if (themeFile == ThemeRegistry.k_DefaultStyleSheetPath)
                    themeAsset = ThemeUtility.builtInDefaultRuntimeTheme;

                if (selectedTheme == themeAsset)
                    continue;

                string displayName = ThemeUtility.NicifyThemeName(themeAsset);
                if (isRuntimeTheme && themeAsset == runtimeDefault)
                    displayName += ThemeUtility.DefaultThemeSuffix;

                choices.Add(displayName);
                themeData[displayName] = (CanvasTheme.Custom, themeAsset);
            }
        }
    }
}
