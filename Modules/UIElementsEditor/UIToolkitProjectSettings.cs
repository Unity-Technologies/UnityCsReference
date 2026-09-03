// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using UnityEditor.UIElements.Experimental.Debugger;
using UnityEditor.UIElements.Experimental.UILayoutDebugger;
using UnityEditor.UIElements.Experimental.USSStats;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.UIElements
{
    [FilePath("ProjectSettings/UIToolkitProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [NoAutoStaticsCleanup] // Editor settings singleton; onEnableGridLayoutChanged subscribers unsubscribe on panel detach.
    internal partial class UIToolkitProjectSettings : ScriptableSingleton<UIToolkitProjectSettings>
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";
        const string k_HideNotificationAboutMissingUITKPackage = "UIBuilder.HideNotificationAboutMissingUITKPackage";
        const string k_DisableMouseWheelZooming = "UIBuilder.DisableMouseWheelZooming";
        const string k_EnableAbsolutePositionPlacement = "UIBuilder.EnableAbsolutePositionPlacement";
        const string k_EnableEventDebugger = "UIToolkit.EnableEventDebugger";
        const string k_EnableLayoutDebugger = "UIToolkit.EnableLayoutDebugger";
        const string k_EnableUSStatsWindow = "UIToolkit.EnableUSSStatsWindow";
        const string k_EnableZIndex = "UIAuthoring.EnableZIndex";
        const string k_EnableFilterShaderGraph = "UIToolkit.EnableFilterShaderGraph";
        const string k_EnableCurvedUI = "UIToolkit.EnableCurvedUI";
        const string k_EnableMultiWindowBuilder = "UIBuilder.EnableMultiWindow";

        // Must match BuilderConstants.BuilderMenuEntry + " (New Window)" in the UI Builder module (which this
        // module cannot reference). The Menu API is only reachable here, so the experimental menu item is
        // registered/removed here and routed to the Builder through openNewBuilderWindowMenuCommand.
        const string k_MultiWindowBuilderMenuPath = "Window/UI Toolkit/UI Builder (New Window)";

        [SerializeField] LazyLoadReference<ThemeStyleSheet> m_DefaultRuntimeTheme;
        [SerializeField] LazyLoadReference<ThemeStyleSheet> m_DefaultEditorTheme;
        [SerializeField] CanvasTheme m_DefaultRuntimeCanvasTheme;
        [SerializeField] CanvasTheme m_DefaultEditorCanvasTheme;
        [SerializeField] bool m_ConsistentAttributeOrderingWhenExporting;

        /// <summary>
        /// Invoked when any theme setting changes (runtime/editor theme or canvas theme).
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [AutoStaticsCleanupOnCodeReload]
        internal static Action onThemeChanged;

        /// The default runtime theme for the project (version controlled).
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static ThemeStyleSheet defaultRuntimeTheme
        {
            get => instance.m_DefaultRuntimeTheme.asset;
            set
            {
                if (instance.m_DefaultRuntimeTheme.asset != value)
                {
                    instance.m_DefaultRuntimeTheme = value;
                    instance.Save();
                    onThemeChanged?.Invoke();
                }
            }
        }

        /// The default editor theme for the project (version controlled).
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static ThemeStyleSheet defaultEditorTheme
        {
            get => instance.m_DefaultEditorTheme.asset;
            set
            {
                if (instance.m_DefaultEditorTheme.asset != value)
                {
                    instance.m_DefaultEditorTheme = value;
                    instance.Save();
                    onThemeChanged?.Invoke();
                }
            }
        }

        /// The default runtime canvas theme type for the project (version controlled).
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static CanvasTheme defaultRuntimeCanvasTheme
        {
            get => instance.m_DefaultRuntimeCanvasTheme;
            set
            {
                if (instance.m_DefaultRuntimeCanvasTheme != value)
                {
                    instance.m_DefaultRuntimeCanvasTheme = value;
                    instance.Save();
                    onThemeChanged?.Invoke();
                }
            }
        }

        /// The default editor canvas theme type for the project.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static CanvasTheme defaultEditorCanvasTheme
        {
            get => instance.m_DefaultEditorCanvasTheme;
            set
            {
                if (instance.m_DefaultEditorCanvasTheme != value)
                {
                    instance.m_DefaultEditorCanvasTheme = value;
                    instance.Save();
                    onThemeChanged?.Invoke();
                }
            }
        }

        [SerializeField]
        private bool m_EnableLowLevelDebugger = false;

        [SerializeField]
        private bool m_EnablePanelRendererAnimation = false;

        [SerializeField]
        private bool m_EnableUIComponents = false;

		
        [SerializeField]
        private bool m_EnableGridLayout = false;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule", "UnityEditor.UIElementsSamplesModule")]
        internal static Action<bool> onEnableGridLayoutChanged;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule", "UnityEditor.UIElementsSamplesModule")]
        internal static bool enableGridLayout
        {
            get => instance.m_EnableGridLayout;
            set
            {
                if (instance.m_EnableGridLayout == value)
                    return;
                instance.m_EnableGridLayout = value;
                instance.Save();
                // Push to the native layout solver so display:grid switches between grid and its
                // flex fallback live, then let grid-related UI react.
                UnityEngine.UIElements.Layout.LayoutNative.SetGridLayoutEnabled(value);
                onEnableGridLayoutChanged?.Invoke(value);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule", "UnityEditor.UIElementsSamplesModule")]
        internal static Action onEnableZIndexChanged;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule", "UnityEditor.UIElementsSamplesModule")]
        internal static bool enableZIndex
        {
            get => GetBool(k_EnableZIndex);
            set
            {
                if (GetBool(k_EnableZIndex) == value)
                    return;
                SetBool(k_EnableZIndex, value);
                onEnableZIndexChanged?.Invoke();
            }
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule")]
        [NoAutoStaticsCleanup]
        internal static bool s_EnablePanelRendererAnimationAtBoot;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule")]
        internal static bool enablePanelRendererAnimation
        {
            get => instance.m_EnablePanelRendererAnimation;
            set
            {
                if (instance.m_EnablePanelRendererAnimation == value)
                    return;
                instance.m_EnablePanelRendererAnimation = value;
                instance.Save();
            }
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule")]
        internal static bool enableUIComponents
        {
            get => instance.m_EnableUIComponents;
            set
            {
                if (instance.m_EnableUIComponents == value)
                    return;
                instance.m_EnableUIComponents = value;
                instance.Save();
            }
        }

        internal static bool isAnimationSettingDirty => enablePanelRendererAnimation != s_EnablePanelRendererAnimationAtBoot;

        internal static void CaptureBootValues()
        {
            s_EnablePanelRendererAnimationAtBoot = enablePanelRendererAnimation;
        }

        // Curved UI (-unity-curvature) authoring gate, off by default: hides the curvature rows in the
        // UI Builder and VisualElement inspectors. The runtime feature is always on (like background
        // gradients); the inspectors re-check this on refresh, so toggling applies live.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule")]
        internal static bool enableCurvedUI
        {
            get => GetBool(k_EnableCurvedUI);
            set => SetBool(k_EnableCurvedUI, value);
        }

        internal static bool EnableLowLevelDebugger
        {
            get => instance.m_EnableLowLevelDebugger;
            set
            {
                if (instance.m_EnableLowLevelDebugger == value)
                    return;
                instance.m_EnableLowLevelDebugger = value;
                onEnableLowLevelDebuggerChanged?.Invoke(value);
                instance.Save();
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static Action<bool> onEnableLowLevelDebuggerChanged;

        public void Save()
        {
            Save(true);
        }

        public static bool enableEditorExtensionModeByDefault
        {
            get => GetBool(k_EditorExtensionModeKey);
            set => SetBool(k_EditorExtensionModeKey, value);
        }

        public static bool disableMouseWheelZooming
        {
            get => GetBool(k_DisableMouseWheelZooming);
            set => SetBool(k_DisableMouseWheelZooming, value);
        }

        public static bool hideNotificationAboutMissingUITKPackage
        {
            get => GetBool(k_HideNotificationAboutMissingUITKPackage);
            set => SetBool(k_HideNotificationAboutMissingUITKPackage, value);
        }

        public static bool enableAbsolutePositionPlacement
        {
            get => Unsupported.IsDeveloperMode() && GetBool(k_EnableAbsolutePositionPlacement);
            set => SetBool(k_EnableAbsolutePositionPlacement, value);
        }

        [AutoStaticsCleanupOnCodeReload]
        public static event Action<bool> consistentAttributeOrderingWhenExportingChanged;

        public static bool consistentAttributeOrderingWhenExporting
        {
            get => instance.m_ConsistentAttributeOrderingWhenExporting;
            set
            {
                if (instance.m_ConsistentAttributeOrderingWhenExporting == value)
                    return;
                instance.m_ConsistentAttributeOrderingWhenExporting = value;
                instance.Save();
                consistentAttributeOrderingWhenExportingChanged?.Invoke(value);
            }
        }

        public static bool enableEventDebugger
        {
            get => GetBool(k_EnableEventDebugger);
            set
            {
                SetBool(k_EnableEventDebugger, value);
                if (value)
                    Menu.AddMenuItem("Window/UI Toolkit/Event Debugger", "", false, 3010,
                        UIElementsEventsDebugger.ShowUIElementsEventDebugger, null);
                else
                    EditorApplication.CallDelayed(RemoveEventDebuggerMenuItem);
            }
        }

        static void RemoveEventDebuggerMenuItem()
        {
            var menuItems = Menu.GetMenuItems("Window/UI Toolkit/Event Debugger", false, false);
            if (menuItems != null)
            {
                Menu.RemoveMenuItem("Window/UI Toolkit/Event Debugger");
                Menu.RebuildAllMenus();
            }
        }

        public static bool enableLayoutDebugger
        {
            get => GetBool(k_EnableLayoutDebugger);
            set
            {
                SetBool(k_EnableLayoutDebugger, value);
                if (value)
                    Menu.AddMenuItem(UILayoutDebuggerWindow.k_WindowPath, "", false, 3010,
                        UILayoutDebuggerWindow.OpenAndInspectWindow, null);
                else
                    EditorApplication.CallDelayed(RemoveLayoutDebuggerMenuItem);
            }

        }

        static void RemoveLayoutDebuggerMenuItem()
        {
            var menuItems = Menu.GetMenuItems(UILayoutDebuggerWindow.k_WindowPath, false, false);
            if (menuItems != null)
            {
                Menu.RemoveMenuItem(UILayoutDebuggerWindow.k_WindowPath);
                Menu.RebuildAllMenus();
            }
        }

        public static bool enableUSSStats
        {
            get => GetBool(k_EnableUSStatsWindow);
            set
            {
                SetBool(k_EnableUSStatsWindow, value);
                if (value)
                    Menu.AddMenuItem(USSStatsWindow.k_WindowPath, "", false, 3010,
                        USSStatsWindow.OpenAndInspectWindow, null);
                else
                    EditorApplication.CallDelayed(RemoveUSSStatMenuItem);
            }

        }

        static void RemoveUSSStatMenuItem()
        {
            var menuItems = Menu.GetMenuItems(USSStatsWindow.k_WindowPath, false, false);
            if (menuItems != null)
            {
                Menu.RemoveMenuItem(USSStatsWindow.k_WindowPath);
                Menu.RebuildAllMenus();
            }
        }

        // Set by the UI Builder module; invoked by the "UI Builder (New Window)" menu item.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static Action openNewBuilderWindowMenuCommand;

        // Experimental: surfaces the menu entry that opens additional UI Builder windows. Off by default.
        // Only the entry's visibility is gated; the multi-instance code itself always runs.
        public static bool enableMultiWindowBuilder
        {
            get => GetBool(k_EnableMultiWindowBuilder);
            set
            {
                if (GetBool(k_EnableMultiWindowBuilder) == value)
                    return;

                SetBool(k_EnableMultiWindowBuilder, value);
                if (value)
                    AddMultiWindowBuilderMenuItem();
                else
                    EditorApplication.CallDelayed(RemoveMultiWindowBuilderMenuItem);
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterMultiWindowBuilderMenuAtStartup()
        {
            // The menu is not built yet at load time; add the item once it is (mirrors the debuggers above).
            Menu.menuChanged += AddMultiWindowBuilderMenuItemOnce;
        }

        static void AddMultiWindowBuilderMenuItemOnce()
        {
            Menu.menuChanged -= AddMultiWindowBuilderMenuItemOnce;
            if (enableMultiWindowBuilder)
                AddMultiWindowBuilderMenuItem();
        }

        static void AddMultiWindowBuilderMenuItem()
        {
            // Priority 1001 sits directly under the "UI Builder" entry (priority 1000), no separator.
            Menu.AddMenuItem(k_MultiWindowBuilderMenuPath, "", false, 1001,
                () => openNewBuilderWindowMenuCommand?.Invoke(), null);
        }

        static void RemoveMultiWindowBuilderMenuItem()
        {
            if (Menu.GetMenuItems(k_MultiWindowBuilderMenuPath, false, false) != null)
            {
                Menu.RemoveMenuItem(k_MultiWindowBuilderMenuPath);
                Menu.RebuildAllMenus();
            }
        }

        // Gates the ShaderGraph "Filter" subtarget via SubTarget.isHidden. Off by default.
        internal static bool enableFilterShaderGraph
        {
            get => GetBool(k_EnableFilterShaderGraph);
            set => SetBool(k_EnableFilterShaderGraph, value);
        }

        static bool GetBool(string name)
        {
            var value = EditorUserSettings.GetConfigValue(name);
            if (string.IsNullOrEmpty(value))
                return false;

            return Convert.ToBoolean(value);
        }

        static void SetBool(string name, bool value)
        {
            EditorUserSettings.SetConfigValue(name, value.ToString());
        }

        internal void Reset()
        {
            defaultRuntimeTheme = null;
            defaultEditorTheme = null;
            defaultRuntimeCanvasTheme = CanvasTheme.ProjectSettings;
            defaultEditorCanvasTheme = CanvasTheme.ProjectSettings;
            m_EnablePanelRendererAnimation = false;
            m_EnableUIComponents = false;
        }


        internal static void Reset2()
        {
            //SD: Renamed because a reset is a special keyword expected to reset the scriptable object to its default state. I don't know where this is used (tests?)
            EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, null);
            EditorUserSettings.SetConfigValue(k_HideNotificationAboutMissingUITKPackage, null);
            EditorUserSettings.SetConfigValue(k_DisableMouseWheelZooming, null);
            EditorUserSettings.SetConfigValue(k_EnableAbsolutePositionPlacement, null);
            EditorUserSettings.SetConfigValue(k_EnableEventDebugger, null);
            EditorUserSettings.SetConfigValue(k_EnableCurvedUI, null);
        }
    }
}

#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
