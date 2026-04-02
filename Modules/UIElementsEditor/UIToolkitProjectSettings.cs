// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements.Experimental.Debugger;
using UnityEditor.UIElements.Experimental.UILayoutDebugger;
using UnityEditor.UIElements.Experimental.USSStats;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [FilePath("ProjectSettings/UIToolkitProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class UIToolkitProjectSettings : ScriptableSingleton<UIToolkitProjectSettings>
    {
        const string k_EditorExtensionModeKey = "UIBuilder.EditorExtensionModeKey";
        const string k_HideNotificationAboutMissingUITKPackage = "UIBuilder.HideNotificationAboutMissingUITKPackage";
        const string k_DisableMouseWheelZooming = "UIBuilder.DisableMouseWheelZooming";
        const string k_EnableAbsolutePositionPlacement = "UIBuilder.EnableAbsolutePositionPlacement";
        const string k_EnableEventDebugger = "UIToolkit.EnableEventDebugger";
        const string k_EnableLayoutDebugger = "UIToolkit.EnableLayoutDebugger";
        const string k_EnableUSStatsWindow = "UIToolkit.EnableUSSStatsWindow";

        [SerializeField] LazyLoadReference<ThemeStyleSheet> m_DefaultRuntimeTheme;
        [SerializeField] LazyLoadReference<ThemeStyleSheet> m_DefaultEditorTheme;
        [SerializeField] CanvasTheme m_DefaultRuntimeCanvasTheme;
        [SerializeField] CanvasTheme m_DefaultEditorCanvasTheme;
        [SerializeField] bool m_ConsistentAttributeOrderingWhenExporting;

        /// <summary>
        /// Invoked when any theme setting changes (runtime/editor theme or canvas theme).
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        }


        internal static void Reset2()
        {
            //SD: Renamed because a reset is a special keyword expected to reset the scriptable object to its default state. I don't know where this is used (tests?)
            EditorUserSettings.SetConfigValue(k_EditorExtensionModeKey, null);
            EditorUserSettings.SetConfigValue(k_HideNotificationAboutMissingUITKPackage, null);
            EditorUserSettings.SetConfigValue(k_DisableMouseWheelZooming, null);
            EditorUserSettings.SetConfigValue(k_EnableAbsolutePositionPlacement, null);
            EditorUserSettings.SetConfigValue(k_EnableEventDebugger, null);
        }
    }
}

