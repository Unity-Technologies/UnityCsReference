// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Profiling
{
    [UIFramework(UIFrameworkUsage.UITK)]
    internal class ProfilerModulesDropdownWindow : EditorWindow
    {
        const string k_UxmlResourceName = "ProfilerModulesDropdownWindow.uxml";
        const string k_UssSelectorModuleEditorWindowDark = "profiler-modules-dropdown-window__dark";
        const string k_UssSelectorModuleEditorWindowLight = "profiler-modules-dropdown-window__light";
        const string k_UssSelector_ListView = "modules__list-view";
        const string k_UssSelector_ConfigureButton = "modules__toolbar__configure-button";
        const string k_UssSelector_ConfigureIcon = "modules__toolbar__configure-icon";
        const string k_UssSelector_RestoreDefaultsButton = "modules__toolbar__restore-defaults-button";
        const int k_WindowWidth = 250;
        const int k_ListViewItemHeight = 26;
        const int k_ToolbarHeight = 30;
        const int k_TotalBorderHeight = 2;

        static long s_LastClosedTime;

        // Data
        bool m_IsInitialized;
        List<ProfilerModule> m_Modules;

        // UI
        ListView m_ModulesListView;

        public IResponder responder { get; set; }

        public static bool TryPresentIfNoOpenInstances(Rect buttonRect, List<ProfilerModule> modules, out ProfilerModulesDropdownWindow window)
        {
            /* Due to differences in the timing of Editor window destruction across platforms, we cannot easily detect if the window was just destroyed due to being unfocussed with EditorWindow.HasOpenInstances alone. We need to detect this situation in the case of clicking the dropdown control whilst the window is open - this should close the window. Following the advice of the Editor team, this timer pattern is copied from LayerSettingsWindow as this is how they work around the issue. realtimeSinceStartUp is not used since it is set to 0 when entering/exiting playmode, we assume an increasing time when comparing time.
             */
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (HasOpenInstances<ProfilerModulesDropdownWindow>() || justClosed)
            {
                window = null;
                return false;
            }

            window = GetWindowDontShow<ProfilerModulesDropdownWindow>();
            window.Initialize(modules);

            var windowHeight = (k_ListViewItemHeight * modules.Count) + k_ToolbarHeight + k_TotalBorderHeight;
            var windowSize = new Vector2(k_WindowWidth, windowHeight);
            window.ShowAsDropDown(buttonRect, windowSize);
            window.Focus();

            return true;
        }

        void Initialize(List<ProfilerModule> modules)
        {
            if (m_IsInitialized)
            {
                return;
            }

            m_Modules = modules;
            m_IsInitialized = true;

            BuildWindow();
        }

        void BuildWindow()
        {
            var template = EditorGUIUtility.Load(k_UxmlResourceName) as VisualTreeAsset;
            template.CloneTree(rootVisualElement);

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssSelectorModuleEditorWindowDark : k_UssSelectorModuleEditorWindowLight;
            rootVisualElement.AddToClassList(themeUssClass);

            m_ModulesListView = rootVisualElement.Q<ListView>(k_UssSelector_ListView);
            m_ModulesListView.fixedItemHeight = k_ListViewItemHeight;
            m_ModulesListView.makeItem = MakeListViewItem;
            m_ModulesListView.bindItem = BindListViewItem;
            m_ModulesListView.selectionType = SelectionType.Single;
            m_ModulesListView.selectionChanged += OnModuleSelected;
            m_ModulesListView.itemsSource = m_Modules;

            var configureButton = rootVisualElement.Q<Button>(k_UssSelector_ConfigureButton);
            configureButton.clicked += ConfigureModules;

            var configureIcon = rootVisualElement.Q<VisualElement>(k_UssSelector_ConfigureIcon);
            configureIcon.style.backgroundImage = Styles.configureIcon;

            var restoreDefaultsButton = rootVisualElement.Q<Button>(k_UssSelector_RestoreDefaultsButton);
            restoreDefaultsButton.text = LocalizationDatabase.GetLocalizedString("Restore Defaults");
            restoreDefaultsButton.clicked += RestoreDefaults;
        }

        void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        }

        void OnModuleSelected(IEnumerable<object> selectedItems)
        {
            var selectedIndex = m_ModulesListView.selectedIndex;
            if (selectedIndex < 0)
            {
                return;
            }

            var selectedModule = m_Modules[selectedIndex];
            selectedModule.ToggleActive();
            m_ModulesListView.Rebuild();
            m_ModulesListView.ClearSelection();

            responder?.OnModuleActiveStateChanged();
        }

        void ConfigureModules()
        {
            responder?.OnConfigureModules();
            Close();
        }

        void RestoreDefaults()
        {
            var title = LocalizationDatabase.GetLocalizedString("Restore Defaults");
            var message = LocalizationDatabase.GetLocalizedString("Do you want to restore the default Profiler Window modules? All custom modules will be deleted and the order of modules will be reset.");
            var restoreDefaults = LocalizationDatabase.GetLocalizedString("Restore Defaults");
            var cancel = LocalizationDatabase.GetLocalizedString("Cancel");
            bool proceed = EditorUtility.DisplayDialog(title, message, restoreDefaults, cancel);
            if (proceed)
            {
                responder?.OnRestoreDefaultModules();
            }
            Close();
        }

        VisualElement MakeListViewItem()
        {
            return new ModuleListViewItem();
        }

        void BindListViewItem(VisualElement element, int index)
        {
            var module = m_Modules[index];
            var moduleListViewItem = element as ModuleListViewItem;
            moduleListViewItem.ConfigureWithModule(module);
        }

        static class Styles
        {
            public static readonly Texture2D configureIcon = EditorGUIUtility.LoadIcon("SettingsIcon");
        }

        class ModuleListViewItem : VisualElement
        {
            const string k_UssClass = "module-list-view-item";
            const string k_UssClass_Icon = "module-list-view-item__icon";
            const string k_UssClass_Label = "module-list-view-item__label";
            const string k_UssClass_WarningIcon = "module-list-view-item__warning-icon";

            Image m_Icon;
            Label m_Label;
            Image m_WarningIcon;

            public ModuleListViewItem()
            {
                AddToClassList(k_UssClass);

                m_Icon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit
                };
                m_Icon.AddToClassList(k_UssClass_Icon);
                Add(m_Icon);

                m_Label = new Label();
                m_Label.AddToClassList(k_UssClass_Label);
                Add(m_Label);

                m_WarningIcon = new Image
                {
                    scaleMode = ScaleMode.ScaleToFit,
                    image = EditorGUIUtility.LoadIcon("console.warnicon.sml")
                };
                m_WarningIcon.AddToClassList(k_UssClass_WarningIcon);
                Add(m_WarningIcon);
            }

            public void ConfigureWithModule(ProfilerModule module)
            {
                bool isActive = module.active;
                SetActive(isActive);

                m_Label.text = module.DisplayName;
                if (!string.IsNullOrEmpty(module.WarningMsg))
                {
                    m_WarningIcon.style.visibility = Visibility.Visible;
                    m_WarningIcon.tooltip = module.WarningMsg;
                }
                else
                    m_WarningIcon.style.visibility = Visibility.Hidden;
            }

            public void SetActive(bool active)
            {
                var icon = (active) ? Styles.activeIcon : Styles.inactiveIcon;
                m_Icon.image = icon;
            }

            static class Styles
            {
                public static readonly Texture2D inactiveIcon = EditorGUIUtility.LoadIcon("toggle_bg");
                public static readonly Texture2D activeIcon = EditorGUIUtility.LoadIcon("toggle_on");
            }
        }

        public interface IResponder
        {
            void OnModuleActiveStateChanged();
            void OnConfigureModules();
            void OnRestoreDefaultModules();
        }
    }
}
