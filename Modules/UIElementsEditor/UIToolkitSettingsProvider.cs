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
        const string k_EnableUSSStatsWindow = "enable-uss-stats-window";
        const string k_EnablePanelRendererAnimation = "enable-panel-renderer-animation";
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

        private static EventCallback<ChangeEvent<string>> s_RuntimeThemeCallback;
        private static EventCallback<ChangeEvent<string>> s_EditorThemeCallback;

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

            var ussStatsToggle = rootElement.Q<Toggle>(k_EnableUSSStatsWindow);
            ussStatsToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enableUSSStats);
            ussStatsToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enableUSSStats = e.newValue;
            });

            var panelRendererAnimationToggle = rootElement.Q<Toggle>(k_EnablePanelRendererAnimation);
            panelRendererAnimationToggle.SetValueWithoutNotify(UIToolkitProjectSettings.enablePanelRendererAnimation);
            panelRendererAnimationToggle.RegisterValueChangedCallback(e =>
            {
                UIToolkitProjectSettings.enablePanelRendererAnimation = e.newValue;
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
                PopulateThemeMenu(defaultRuntimeThemeMenu, false, onRuntimeThemeSelected);
            };
            PopulateThemeMenu(defaultRuntimeThemeMenu, false, onRuntimeThemeSelected);

            var defaultEditorThemeMenu = rootElement.Q<ProjectSettingsThemeDropdown>(k_DefaultEditorTheme);

            // Callback for editor theme changes
            Action<CanvasTheme, ThemeStyleSheet> onEditorThemeSelected = null;
            onEditorThemeSelected = (canvasTheme, themeAsset) =>
            {
                UIToolkitProjectSettings.defaultEditorCanvasTheme = canvasTheme;
                UIToolkitProjectSettings.defaultEditorTheme = themeAsset;

                // Re-populate the menu to update ordering (selected theme moves to top)
                PopulateThemeMenu(defaultEditorThemeMenu, true, onEditorThemeSelected);
            };
            PopulateThemeMenu(defaultEditorThemeMenu, true, onEditorThemeSelected);

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

        private static void PopulateThemeMenu(ProjectSettingsThemeDropdown menu, bool isEditorTheme, Action<CanvasTheme, ThemeStyleSheet> onThemeSelected)
        {
            var choices = new List<string>();
            var themeData = new Dictionary<string, (CanvasTheme, ThemeStyleSheet)>();
            var (selectedCanvasTheme, selectedTheme) = ThemeUtility.GetProjectDefaultTheme(isEditorTheme);

            // Add all editor theme choices
            if (isEditorTheme)
            {
                var editorThemeNames = ThemeUtility.GetEditorThemesToDisplayName();
                foreach (var themeKvp in editorThemeNames)
                {
                    var displayName =  themeKvp.Value;
                    var canvasTheme = themeKvp.Key;

                    if (canvasTheme == CanvasTheme.Default)
                        displayName += ThemeUtility.DefaultThemeSuffix;

                    // Add the currently selected theme as the first option
                    if (canvasTheme == selectedCanvasTheme)
                        choices.InsertRange(0, [displayName, ProjectSettingsThemeDropdown.k_Separator]);
                    else
                        choices.Add(displayName);

                    themeData[displayName] = (canvasTheme, null);
                }

                choices.Add(ProjectSettingsThemeDropdown.k_Separator);
            }

            // Add remaining theme choices
            var runtimeDefault = ThemeUtility.FindProjectDefaultRuntimeThemeAssetOrDefault();
            var runtimeThemeNames = ThemeUtility.GetRuntimeThemesToDisplayName();
            foreach (var themeKvp in runtimeThemeNames)
            {
                var displayName = themeKvp.Value;
                var themeAsset = themeKvp.Key;

                if (!isEditorTheme && themeAsset == runtimeDefault)
                    displayName += ThemeUtility.DefaultThemeSuffix;

                // Add currently selected theme to the front
                if (selectedTheme == themeAsset)
                    choices.InsertRange(0, [displayName, ProjectSettingsThemeDropdown.k_Separator]);
                else
                    choices.Add(displayName);
                themeData[displayName] = (CanvasTheme.Custom, themeAsset);
            }

            // Set choices and initial value
            menu.choices = choices;
            menu.SetValueWithoutNotify(choices[0]);

            // Register new callback and store reference
            EventCallback<ChangeEvent<string>> valueChangedCallback = evt =>
            {
                if (themeData.TryGetValue(evt.newValue, out var themeInfo))
                {
                    var (canvasTheme, themeSheet) = themeInfo;
                    onThemeSelected?.Invoke(canvasTheme, themeSheet);
                }
            };

            // Unregister old callbacks
            if (isEditorTheme)
            {
                if (s_EditorThemeCallback != null)
                    menu.UnregisterValueChangedCallback(s_EditorThemeCallback);

                menu.RegisterValueChangedCallback(valueChangedCallback);
                s_EditorThemeCallback = valueChangedCallback;
            }
            else
            {
                if (s_RuntimeThemeCallback != null)
                    menu.UnregisterValueChangedCallback(s_RuntimeThemeCallback);

                menu.RegisterValueChangedCallback(valueChangedCallback);
                s_RuntimeThemeCallback = valueChangedCallback;
            }
        }
    }
}
