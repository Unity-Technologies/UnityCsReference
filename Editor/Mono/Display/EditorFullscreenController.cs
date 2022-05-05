// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [InitializeOnLoad]
    [FilePathAttribute("Library/EditorDisplaySettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class EditorFullscreenController : ScriptableSingleton<EditorFullscreenController>
    {
        private const float kDisplayCheckIntervalSec = 1.0f;

        [SerializeField] private List<EditorDisplaySettingsProfile> m_profiles = null;
        [SerializeField] private bool m_isSortDisplayOrder = true;
        [SerializeField] private bool m_showNotificationOnFullscreen = true;
        [SerializeField] private bool m_showToolbarOnFullscreen = false;
        [SerializeField] private int m_selectedProfileIndex;
        [SerializeField] private EditorDisplayFullscreenSetting m_mainDisplaySetting;

        private PlayModeStateChange m_state;
        private EditorDisplayFullscreenSetting m_defaultSetting;

        internal bool isPlaying => m_state == PlayModeStateChange.ExitingEditMode;

        private float m_tLastTimeChecked;
        private int m_numberOfConnectedDisplays;

        private string[] m_displayNames;
        private int[] m_displayIds;
        private int m_mainDisplayId;

        private EnumData m_buildTargetData;
        private bool m_buildTargetDataInitialized;

        private Dictionary<Type, string> m_AvailableWindowTypes;
        private Dictionary<int, EditorDisplayFullscreenSetting> m_DisplaySettings;
        private List<ContainerWindow> m_FullscreenContainerWindows;

        // This is a stub for selecting the fullscreen option via the game view toolbar GUI.
        // Instead of having configurable profiles, we'll use the main display setting that is
        // created by default and modify which display we want to see it on via the toolbar GUI
        // dropdown. In the future we may want to hook it up to the currently active display profile.

        // This is a stub. in the future references to this should be replaced with the currently active profile.

        internal static void SetSettingsForCurrentDisplay(int display)
        {
            var c = instance;

            if (c.m_DisplaySettings == null)
            {
                c.m_DisplaySettings = new Dictionary<int, EditorDisplayFullscreenSetting>();
            }

            if (c.m_DisplaySettings.ContainsKey(display))
            {
                c.m_mainDisplaySetting = c.m_DisplaySettings[display];
                return;
            }

            var newDisplay = new EditorDisplayFullscreenSetting(c.m_DisplaySettings.Count + 1, "Main Display");
            newDisplay.enabled = true;
            newDisplay.playModeViewSettings = new GameViewFullscreenSettings();
            newDisplay.viewWindowTitle = GetWindowTitle(typeof(GameView));

            c.m_DisplaySettings.Add(display, newDisplay);
            c.m_mainDisplaySetting = c.m_DisplaySettings[display];
        }

        internal static bool isFullscreenOnPlay
        {
            get => instance.m_mainDisplaySetting.mode == EditorDisplayFullscreenSetting.Mode.FullscreenOnPlaymode;
            set => instance.SetFullscreenMainDisplay(value);
        }

        // This is a stub. Future references to this should be replaced with the currently active profile.
        internal static bool isToolbarEnabledOnFullscreen
        {
            get
            {
                return (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                    ? settings.ShowToolbar
                    : false;
            }
            set
            {
                instance.SetShowToolbarOnMainDisplay(value);
            }
        }

        // This is a stub. Future references to this should be replaced with the currently active profile.
        internal static int fullscreenDisplayId
        {
            get => instance.m_mainDisplaySetting.displayId;
            set => instance.SetFullscreenDisplayId(value);
        }

        internal static int targetDisplayID
        {
            get
            {
                return (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                        ? settings.DisplayNumber
                        : 0;
            }
            set
            {
                if (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                {
                    settings.DisplayNumber = value;
                }
            }
        }

        internal static bool enableVSync
        {
            get
            {
                return (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                    ? settings.VsyncEnabled
                    : false;
            }
            set
            {
                if (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                {
                    settings.VsyncEnabled = value;
                }
            }
        }

        internal static int selectedSizeIndex
        {
            get
            {
                return (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                        ? settings.SelectedSizeIndex
                        : 0;
            }
            set
            {
                if (instance.m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings settings)
                {
                    settings.SelectedSizeIndex = value;
                }
            }
        }

        // This is a stub. Future references to this should be replaced with the currently active profile.
        internal static Vector2 fullscreenDisplayRenderSize
        {
            get => instance.GetDisplayRenderSizeFromId(fullscreenDisplayId);
        }

        internal static DisplayAPIControlMode DisplayAPIMode
        {
            get
            {
                var c = instance;
                if (c.m_profiles == null || c.m_profiles.Count <= 0)
                {
                    c.InitializeProfile();
                }

                if (c.m_profiles == null || c.m_profiles.Count <= c.m_selectedProfileIndex)
                    return DisplayAPIControlMode.FromEditor;

                return c.m_profiles[c.m_selectedProfileIndex].DisplayAPIMode;
            }
        }

        internal static void OnEnterPlaymode()
        {
            var c = instance;
            c.m_state = PlayModeStateChange.ExitingEditMode;
            c.RefreshDisplayStateWithCurrentProfile();
        }

        internal static void OnExitPlaymode()
        {
            var c = instance;
            c.m_state = PlayModeStateChange.ExitingPlayMode;
            c.RefreshDisplayStateWithCurrentProfile();
        }

        internal static void SetMainDisplayPlayModeViewType(Type playModeViewType)
        {
            SetDisplayPlayModeViewType(instance.m_mainDisplaySetting, playModeViewType);
        }

        internal static int[] GetConnectedDisplayIds()
        {
            return instance.m_displayIds;
        }

        internal static string[] GetConnectedDisplayNames()
        {
            return instance.m_displayNames;
        }

        internal static string[] GetConnectedDisplayIdsAndNames()
        {
            int displayCount = instance.m_displayNames.Length;
            string[] displayList = new string[displayCount];

            for (int i = 0; i < displayCount; i++)
            {
                displayList[i] = i + ": " + instance.m_displayNames[i];
            }
            return displayList;
        }

        private void InitializeProfile()
        {
            if (m_profiles != null)
            {
                return;
            }

            m_mainDisplaySetting = new EditorDisplayFullscreenSetting(0, "Main Display");
            m_mainDisplaySetting.enabled = true;
            m_mainDisplaySetting.playModeViewSettings = new GameViewFullscreenSettings();
            m_mainDisplaySetting.viewWindowTitle = GetWindowTitle(typeof(GameView));

            m_profiles = new List<EditorDisplaySettingsProfile>();
            var defaultProfile = new EditorDisplaySettingsProfile("Default");
            defaultProfile.AddEditorDisplayFullscreenSetting(m_defaultSetting);

            var displayAPIProfile = new EditorDisplaySettingsProfile("Simulate Standalone")
            {
                DisplayAPIMode = DisplayAPIControlMode.FromRuntime
            };

            for (var i = 0; i < m_displayNames.Length; ++i)
            {
                displayAPIProfile.AddEditorDisplayFullscreenSetting(CreateDefaultEditorDisplayFullscreenSetting(i));
            }

            m_profiles.Add(defaultProfile);
            m_profiles.Add(displayAPIProfile);
            m_selectedProfileIndex = 0;
        }

        private EditorDisplayFullscreenSetting CreateDefaultEditorDisplayFullscreenSetting(int displayNumber)
        {
            var setting = new EditorDisplayFullscreenSetting(m_displayIds[displayNumber], m_displayNames[displayNumber]) {enabled = true};
            var gvSetting = new GameViewFullscreenSettings {DisplayNumber = displayNumber};
            setting.playModeViewSettings = gvSetting;
            setting.viewWindowTitle = GetWindowTitle(typeof(GameView));

            return setting;
        }

        private void OnEnable()
        {
            EditorApplication.globalEventHandler += HandleToggleFullscreenKeyShortcut;

            UpdateDisplayNamesAndIds();
            m_buildTargetDataInitialized = false;

            if (m_profiles == null)
            {
                InitializeProfile();
            }

            m_FullscreenContainerWindows = new List<ContainerWindow>();
            m_defaultSetting = new EditorDisplayFullscreenSetting(0, string.Empty);
            m_defaultSetting.enabled = true;
            m_defaultSetting.playModeViewSettings = new GameViewFullscreenSettings();
            m_defaultSetting.viewWindowTitle = GetWindowTitle(typeof(GameView));

            EditorDisplayUtility.SetSortDisplayOrder(m_isSortDisplayOrder);
            RefreshDisplayStateWithCurrentProfile();

            EditorApplication.update += CheckDisplayNumberChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= CheckDisplayNumberChanged;
            EditorApplication.globalEventHandler -= HandleToggleFullscreenKeyShortcut;
        }

        public void CheckDisplayNumberChanged()
        {
            var tNow = Time.realtimeSinceStartup;
            if (tNow - m_tLastTimeChecked < kDisplayCheckIntervalSec)
                return;

            var ncDisplays = EditorDisplayUtility.GetNumberOfConnectedDisplays();
            if (ncDisplays != m_numberOfConnectedDisplays)
            {
                UpdateDisplayNamesAndIds();
                HandleDisplayProfileChange();
            }

            m_tLastTimeChecked = tNow;
        }

        private static void SetDisplayPlayModeViewType(EditorDisplayFullscreenSetting setting, Type playModeViewType)
        {
            setting.playModeViewSettings = CreatePlayModeViewSettingsForType(playModeViewType);
            setting.viewWindowTitle = GetWindowTitle(playModeViewType);
        }

        private void HandleDisplayProfileChange()
        {
            // When hardware display configuration changes,
            // always go back to default profile.
            SetCurrentProfile(0);
            RefreshDisplayStateWithCurrentProfile();
        }

        private void UpdateDisplayNamesAndIds()
        {
            m_numberOfConnectedDisplays = EditorDisplayUtility.GetNumberOfConnectedDisplays();
            m_displayNames = new string[m_numberOfConnectedDisplays];
            m_displayIds = new int[m_numberOfConnectedDisplays];
            m_mainDisplayId = EditorDisplayUtility.GetMainDisplayId();

            for (var i = 0; i < m_numberOfConnectedDisplays; ++i)
            {
                m_displayIds[i] = EditorDisplayUtility.GetDisplayId(i);
                m_displayNames[i] = EditorDisplayUtility.GetDisplayName(i);
            }

            if (m_profiles != null && m_profiles.Count > 1)
            {
                var displayAPIProfile = m_profiles[1];

                for (var i = 0; i < m_numberOfConnectedDisplays; ++i)
                {
                    if (displayAPIProfile.Settings.Count <= i)
                    {
                        displayAPIProfile.AddEditorDisplayFullscreenSetting(CreateDefaultEditorDisplayFullscreenSetting(i));
                    }
                    else
                    {
                        displayAPIProfile.Settings[i].displayId = m_displayIds[i];
                    }
                }
            }
        }

        private void GetInstalledBuildTargetData()
        {
            if (m_buildTargetDataInitialized)
                return;

            var buildTargetData = EnumDataUtility.GetCachedEnumData(typeof(BuildTarget));
            var installedBuildTargetCount =
                (from BuildTarget target in buildTargetData.values
                    let @group = BuildPipeline.GetBuildTargetGroup(target)
                    where BuildPipeline.IsBuildTargetSupported(@group, target) select target).Count();

            m_buildTargetData = new EnumData
            {
                values = new Enum[installedBuildTargetCount],
                displayNames = new string[installedBuildTargetCount],
                tooltip = new string[installedBuildTargetCount],
                flagValues = new int[installedBuildTargetCount],
                flags = buildTargetData.flags,
                serializable = buildTargetData.serializable,
                underlyingType = buildTargetData.underlyingType,
                unsigned = buildTargetData.unsigned
            };

            for (int i = 0, j = 0; i < buildTargetData.values.Length; ++i)
            {
                var target = (BuildTarget) buildTargetData.values[i];
                var group = BuildPipeline.GetBuildTargetGroup(target);
                if (BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    m_buildTargetData.values[j] = buildTargetData.values[i];
                    m_buildTargetData.displayNames[j] = buildTargetData.displayNames[i];
                    m_buildTargetData.tooltip[j] = buildTargetData.tooltip[i];
                    m_buildTargetData.flagValues[j] = buildTargetData.flagValues[i];
                    ++j;
                }
            }

            m_buildTargetDataInitialized = true;
        }

        private EditorDisplayFullscreenSetting GetSettingForDisplay(int displayIndex)
        {
            if (m_profiles == null)
            {
                InitializeProfile();
            }

            var displayId = EditorDisplayUtility.GetDisplayId(displayIndex);
            if (DisplayAPIMode == DisplayAPIControlMode.FromEditor &&
                displayId == EditorDisplayUtility.GetMainDisplayId())
            {
                return m_mainDisplaySetting;
            }

            var s = m_profiles[m_selectedProfileIndex].GetEditorDisplayFullscreenSetting(displayId);
            if (s == null || !s.enabled)
            {
                return m_defaultSetting;
            }

            return s;
        }

        private EditorDisplayFullscreenSetting GetSettingForDisplayById(int displayId)
        {
            return GetSettingForDisplay(GetDisplayIndexFromId(displayId));
        }

        private int GetDisplayIndexFromId(int displayId)
        {
            var nDisplays = EditorDisplayUtility.GetNumberOfConnectedDisplays();
            for (var i = 0; i < nDisplays; ++i)
            {
                if (displayId == EditorDisplayUtility.GetDisplayId(i))
                {
                    return i;
                }
            }
            return 0;
        }

        private Vector2 GetDisplayRenderSizeFromId(int displayId)
        {
            int idx = GetDisplayIndexFromId(displayId);
            int width = EditorDisplayUtility.GetDisplayWidth(idx);
            int height = EditorDisplayUtility.GetDisplayHeight(idx);
            Vector2 size = new Vector2(width, height);
            return size;
        }

        private void RefreshDisplayStateWithCurrentProfile()
        {
            ApplySettings(m_mainDisplaySetting.displayId, m_mainDisplaySetting);
        }

        private static ContainerWindow FindContainerWindow(int displayIndex)
        {
            var windows = ContainerWindow.windows;

            return windows.FirstOrDefault(w =>
                w.m_IsFullscreenContainer &&
                w.m_DisplayIndex == displayIndex);
        }

        internal static void ClearAllFullscreenWindows()
        {
            var windows = ContainerWindow.windows;
            foreach (var w in windows)
            {
                if (w.m_IsFullscreenContainer)
                {
                    w.Close();
                }
            }
        }

        internal static void BeginFullscreen(int displayIndex, int targetWidth, int targetHeight)
        {
            var containerWindow = FindContainerWindow(displayIndex);
            if (containerWindow == null)
            {
                instance.BeginFullScreen(displayIndex, instance.GetSettingForDisplay(displayIndex), targetWidth, targetHeight);
            }
        }

        [RequiredByNativeCode]
        internal static void EndFullscreen(int displayIndex, bool closeWindow)
        {
            ContainerWindow containerWindow = null;
            if (closeWindow)
            {
                containerWindow = FindContainerWindow(displayIndex);
            }
            instance.EndFullScreen(displayIndex, containerWindow);

            if (displayIndex == 0)
            {
                instance.SetFullscreenMainDisplay(false, false);
            }
        }

        private void ApplySettings(int displayIndex, EditorDisplayFullscreenSetting setting)
        {
            var containerWindow = FindContainerWindow(displayIndex);

            if (containerWindow == null &&
                (setting.mode == EditorDisplayFullscreenSetting.Mode.AlwaysFullscreen ||
                (setting.mode == EditorDisplayFullscreenSetting.Mode.FullscreenOnPlaymode && instance.isPlaying)))
            {
                BeginFullScreen(displayIndex, setting);
                return;
            }

            if (setting.mode == EditorDisplayFullscreenSetting.Mode.DoNothing ||
                (setting.mode == EditorDisplayFullscreenSetting.Mode.FullscreenOnPlaymode && !instance.isPlaying))
            {
                EndFullScreen(displayIndex, containerWindow);
                return;
            }

            if (containerWindow != null)
            {
                var hostView = (HostView)containerWindow.rootView;
                var playModeView = (PlayModeView)hostView.actualView;

                if (playModeView != null)
                {
                    playModeView.ApplyEditorDisplayFullscreenSetting(setting.playModeViewSettings);
                }
            }
        }

        private void BeginFullScreen(int displayIndex, EditorDisplayFullscreenSetting setting, int targetWidth = 0, int targetHeight = 0)
        {
            var viewType = typeof(GameView);
            if (setting.playModeViewSettings == null)
            {
                SetDisplayPlayModeViewType(setting, typeof(GameView));
            }

            var attributes = setting.playModeViewSettings.GetType().GetCustomAttributes(typeof(FullscreenSettingsForAttribute), false);
            if (attributes.Length > 0)
            {
                var proposedType = ((FullscreenSettingsForAttribute) attributes[0]).AssignedType;
                if (typeof(PlayModeView).IsAssignableFrom(proposedType))
                {
                    viewType = proposedType;
                }
                else
                {
                    Debug.LogError($"Type assigned for FullscreenSettingsFor is not a subclass of PlayModeView. PlayModeViewSettings={setting.playModeViewSettings.GetType()}, AssignedType{proposedType}");
                }
            }

            var playModeView = ScriptableObject.CreateInstance(viewType) as PlayModeView;

            // Now create a new hostView and container window (popup style -> no borders) and maximize it
            var hostView = ScriptableObject.CreateInstance<HostView>();
            hostView.name = $"HostView {setting.displayName}";
            hostView.actualView = playModeView;
            playModeView.m_Parent = hostView;
            playModeView.isFullscreen = true;

            var containerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
            containerWindow.name = $"Unity Fullscreen Window {setting.displayName}";
            containerWindow.m_DontSaveToLayout = true;
            containerWindow.m_IsFullscreenContainer = true;
            containerWindow.m_DisplayIndex = displayIndex;

            // this ensures the fullscreen game view is shown on the same screen has the normal GameView is currently on
            containerWindow.rootView = hostView;

            playModeView.wantsMouseMove = true;
            playModeView.MakeParentsSettingsMatchMe();

            playModeView.ApplyEditorDisplayFullscreenSetting(setting.playModeViewSettings);
            if (targetWidth != 0 && targetHeight != 0)
            {
                playModeView.targetSize = new Vector2(targetWidth, targetHeight);
            }
            containerWindow.Show(ShowMode.Fullscreen, false, true, true, displayIndex);
            containerWindow.ToggleFullscreen(displayIndex);
            hostView.Focus();

            playModeView.m_Parent.SetAsStartView();
            playModeView.m_Parent.SetAsLastPlayModeView();
            SuppressViewsFromRendering(containerWindow.GetDisplayId(), true);

            if (instance.m_showNotificationOnFullscreen && setting == instance.m_mainDisplaySetting)
            {
                ShowFullscreenNotification(playModeView);
            }
            if (instance.m_showToolbarOnFullscreen && setting == instance.m_mainDisplaySetting)
            {
                SetShowToolbarOnMainDisplay(true);
            }
            m_FullscreenContainerWindows.Add(containerWindow);
        }

        private void EndFullScreen(int displayIndex, ContainerWindow w)
        {
            SuppressViewsFromRendering(EditorDisplayUtility.GetDisplayId(displayIndex), false);

            if (w != null)
            {
                w.Close();
                m_FullscreenContainerWindows.Remove(w);
            }

            // Refocus main window after ending fullscreen.
            var mainWindow = WindowLayout.FindMainWindow();
            if (mainWindow != null && mainWindow.rootView != null) {
                var hostView = mainWindow.rootView as HostView;
                if (hostView != null)
                    hostView.Focus();
            }
        }

        internal static List<ContainerWindow> GetFullscreenContainersForDisplayIndex(int displayIndex)
        {
            List<ContainerWindow> containers = new List<ContainerWindow>();
            foreach (var cw in instance.m_FullscreenContainerWindows)
            {
                if (cw.m_DisplayIndex == displayIndex)
                {
                    containers.Add(cw);
                }
            }

            return containers;
        }

        private bool IsGoingFullscreenOnPlaymode(int displayId)
        {
            var setting = GetSettingForDisplayById(displayId);
            return setting.mode != EditorDisplayFullscreenSetting.Mode.DoNothing;
        }

        private static void ShowFullscreenNotification(PlayModeView playView)
        {
            var binding = ShortcutManager.instance.GetShortcutBinding(kFullscreenToggle);

            playView.ShowNotification(EditorGUIUtility.TextContentWithIcon(string.Format(Styles.disableFullscreenMainDisplayFormatContent.text, binding), "FullscreenNotification"));
            playView.Repaint();
        }

        private void SuppressViewsFromRendering(int displayId, bool suppress)
        {
            var playModeViews = Resources.FindObjectsOfTypeAll(typeof(PlayModeView));
            foreach (PlayModeView playModeView in playModeViews)
            {
                if (playModeView.m_Parent == null ||
                    playModeView.m_Parent.window == null ||
                    playModeView.m_Parent.window.m_IsFullscreenContainer)
                {
                    // The fullscreen window should never suppress rendering.
                    playModeView.suppressRenderingForFullscreen = false;
                    playModeView.SetPlayModeView(true);
                    continue;
                }

                var windowDisplayId = playModeView.m_Parent.window.GetDisplayId();
                if (windowDisplayId == displayId)
                {
                    // This play mode view is rendering on the same display we're going to fullscreen on. Always suppress.
                    playModeView.suppressRenderingForFullscreen = suppress;
                    playModeView.SetPlayModeView(!suppress);
                    continue;
                }

                if (playModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayFullscreen)
                {
                    // This play mode view is going to spawn another fullscreen view. We should suppress rendering
                    // from this game view so as to not duplicate/double our number of rendered views.
                    playModeView.suppressRenderingForFullscreen = suppress;
                    playModeView.SetPlayModeView(!suppress);
                    continue;
                }
            }
        }

        private static void Save()
        {
            instance.Save(true);
        }

        private const string kBaseMenuPath = "Window/Displays";
        private const string kFullscreenOnPlayMenu = kBaseMenuPath + "/Toggle Fullscreen";
        private const string kShowToolbarOnFullscreenMenu = kBaseMenuPath + "/Show Toolbar on Fullscreen";
        private const string kProfilePath = kBaseMenuPath + "/Profiles/";
        public const string kFullscreenToggle = "Window/Fullscreen Game View";
        private static float sLastToggleShortcutTriggerTime = 0f;
        private static float kToggleShortcutTriggerTimeoutInSeconds = 1f;

        void ToggleFullscreen()
        {
            var playModeViews = Resources.FindObjectsOfTypeAll(typeof(PlayModeView));
            foreach (PlayModeView playModeView in playModeViews)
            {
                if (playModeView.m_Parent == null || playModeView.m_Parent.window == null)
                    continue;

                if (playModeView.enterPlayModeBehavior != PlayModeView.EnterPlayModeBehavior.PlayFullscreen)
                    continue; // Do nothing with non-fullscreen game views.

                int displayIdx = playModeView.fullscreenMonitorIdx;
                if (m_DisplaySettings.ContainsKey(displayIdx))
                {
                    var displaySetting = m_DisplaySettings[displayIdx];

                    if (displaySetting.mode == EditorDisplayFullscreenSetting.Mode.AlwaysFullscreen ||
                        displaySetting.mode == EditorDisplayFullscreenSetting.Mode.FullscreenOnPlaymode)
                    {
                        displaySetting.mode = EditorDisplayFullscreenSetting.Mode.DoNothing;
                    }
                    else
                    {
                        displaySetting.mode = EditorDisplayFullscreenSetting.Mode.AlwaysFullscreen;
                    }
                    instance.ApplySettings(displayIdx, displaySetting);
                }
            }
        }

        void HandleToggleFullscreenKeyShortcut()
        {
            if (!Application.isPlaying || m_DisplaySettings == null)
                return;

            var evt = Event.current;
            var binding = ShortcutManager.instance.GetShortcutBinding("Window/Fullscreen Game View");
            var keys = binding.keyCombinationSequence;

            if (evt.type != EventType.KeyUp)
                return;

            if (keys.Where(x => x.keyCode == evt.keyCode).Count() <= 0)
                return;

            // Detect if the right key combinaison is active or not.
            var shiftNecessary = keys.Where(x => (x.modifiers & ShortcutModifiers.Shift) == ShortcutModifiers.Shift).Count() > 0;
            var containShift = (evt.modifiers & EventModifiers.Shift) == EventModifiers.Shift;

            if (shiftNecessary && !containShift)
                return;

            var altNecessary = keys.Where(x => (x.modifiers & ShortcutModifiers.Alt) == ShortcutModifiers.Alt).Count() > 0;
            var containAlt = (evt.modifiers & EventModifiers.Alt) == EventModifiers.Alt;

            if (altNecessary && !containAlt)
                return;

            var ctrlNecessary = keys.Where(x => (x.modifiers & ShortcutModifiers.Control) == ShortcutModifiers.Control).Count() > 0;
            var containCtrl = (evt.modifiers & EventModifiers.Control) == EventModifiers.Control;

            if (ctrlNecessary && !containCtrl)
                return;

            // OSX will animate windows moving to fullscreen and toggling fullscreen again during this animation will
            // cause a slew of bugs. Rather than lock this shortcut until the op is completed, instead provide a reasonable
            // timeout for triggering this shortcut again.
            if (sLastToggleShortcutTriggerTime + kToggleShortcutTriggerTimeoutInSeconds < Time.realtimeSinceStartup)
            {
                sLastToggleShortcutTriggerTime = Time.realtimeSinceStartup;
                ToggleFullscreen();
                evt.Use();
            }
        }

        [ClutchShortcutAttribute(kFullscreenToggle, KeyCode.F7, ShortcutModifiers.Shift | ShortcutModifiers.Control)]
        internal static void FullscreenKeyHandler(ShortcutArguments args)
        {
            // The CTRL + SHIFT + F7 event doesn't work when a Game View is focused.
            // It's a current limitation by the Shortcut Manager. Instead the kFullscreenToggle
            // shortcut is handled by HandleToggleFullscreenKeyShortcut function which is a global
            // event handler.
        }

        static EditorFullscreenController()
        {
            EditorApplication.update -= DelayReloadWindowDisplayMenu;
            EditorApplication.update += DelayReloadWindowDisplayMenu;
        }

        private static void DelayReloadWindowDisplayMenu()
        {
            EditorApplication.update -= DelayReloadWindowDisplayMenu;
            instance.ReloadWindowDisplayMenu();
            EditorUtility.Internal_UpdateAllMenus();
        }

        internal void ReloadWindowDisplayMenu()
        {
            Menu.RemoveMenuItem(kBaseMenuPath);

            var displayMenuItemPriority = 200;

            Menu.AddMenuItem(kFullscreenOnPlayMenu, "", isFullscreenOnPlay, displayMenuItemPriority++, ToggleFullscreenMainDisplay, null);
            Menu.AddMenuItem(kShowToolbarOnFullscreenMenu, "", isToolbarEnabledOnFullscreen, displayMenuItemPriority++, ToggleShowToolbarOnMainDisplay, null);

            Menu.AddSeparator(kBaseMenuPath, displayMenuItemPriority++);

            displayMenuItemPriority += 500;

            for (var i = 0; i < m_profiles.Count; ++i)
            {
                var index = i;
                var profile = m_profiles[i];

                if (i == 0 || profile.Target == EditorUserBuildSettings.activeBuildTarget)
                {
                    Menu.AddMenuItem(kProfilePath + profile.Name, "", m_selectedProfileIndex == i, displayMenuItemPriority++,
                        () => { SetCurrentProfile(index); }, () => !EditorApplication.isPlaying);
                }
            }
        }

        private static void SetCurrentProfile(int index)
        {
            if (instance.m_profiles.Count <= index)
            {
                return;
            }

            if (instance.m_selectedProfileIndex != index)
            {
                var previousMenuPath = kProfilePath + instance.m_profiles[instance.m_selectedProfileIndex].Name;
                var currentMenuPath = kProfilePath + instance.m_profiles[index].Name;
                Menu.SetChecked(previousMenuPath, false);
                Menu.SetChecked(currentMenuPath, true);

                instance.m_selectedProfileIndex = index;
            }

            instance.RefreshDisplayStateWithCurrentProfile();
        }

        private void ToggleFullscreenMainDisplay()
        {
            SetFullscreenMainDisplay(!isFullscreenOnPlay);
        }

        private void SetFullscreenMainDisplay(bool enabled, bool refresh = true)
        {
            if (isFullscreenOnPlay == enabled)
                return;

            m_mainDisplaySetting.mode = enabled
                ? EditorDisplayFullscreenSetting.Mode.FullscreenOnPlaymode
                : EditorDisplayFullscreenSetting.Mode.DoNothing;

            Menu.SetChecked(kFullscreenOnPlayMenu, enabled);
            if (refresh)
            {
                //RefreshDisplayStateWithCurrentProfile();
            }
            Save();
        }

        private void SetFullscreenDisplayId(int displayId)
        {
            m_mainDisplaySetting.displayId = displayId;
        }

        private void ToggleShowToolbarOnMainDisplay()
        {
            SetShowToolbarOnMainDisplay(!isToolbarEnabledOnFullscreen);
        }

        private void SetShowToolbarOnMainDisplay(bool enabled)
        {
            if (!(m_mainDisplaySetting.playModeViewSettings is GameViewFullscreenSettings gameViewSetting) ||
                gameViewSetting.ShowToolbar == enabled)
            {
                return;
            }

            gameViewSetting.ShowToolbar = enabled;
            Menu.SetChecked(kShowToolbarOnFullscreenMenu, enabled);
            RefreshDisplayStateWithCurrentProfile();
            Save();
        }

        private static string GetWindowTitle(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(EditorWindowTitleAttribute), true);
            return attributes.Length > 0 ? ((EditorWindowTitleAttribute)attributes[0]).title : type.Name;
        }

        private Dictionary<Type, string> GetAvailableWindowTypes()
        {
            return m_AvailableWindowTypes ?? (m_AvailableWindowTypes = TypeCache.GetTypesDerivedFrom(typeof(PlayModeView)).OrderBy(GetWindowTitle).ToDictionary(t => t, GetWindowTitle));
        }

        private class Styles
        {
            public static readonly GUIContent sortDisplayOrderContent = EditorGUIUtility.TrTextContent("Sort Display Order (*Windows Only)");
            public static readonly GUIContent showNotificationContent = EditorGUIUtility.TrTextContent("Show notification when entering fullscreen");
            public static readonly GUIContent showToolbarOnFullscreenContent = EditorGUIUtility.TrTextContent("Show game view toolbar on fullscreen");

            public static readonly GUIContent[] modePopupDisplayTexts =
            {
                EditorGUIUtility.TrTextContent("Do nothing"),
                EditorGUIUtility.TrTextContent("Fullscreen on Playmode"),
                EditorGUIUtility.TrTextContent("Always Fullscreen"),
            };
            public static readonly GUIContent addProfileContent = EditorGUIUtility.TrTextContent("Add Profile");
            public static readonly GUIContent nameContent = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent platformContent = EditorGUIUtility.TrTextContent("Platform");
            public static readonly GUIContent disconnectedDisplaysContent = EditorGUIUtility.TrTextContent("Disconnected Displays");


            public static readonly GUIContent mainDisplaySettingHelpTextContent = EditorGUIUtility.TrTextContent("Settings for main display is done from GameView.");
            public static readonly GUIContent viewTypeContent = EditorGUIUtility.TrTextContent("View Type");
            public static readonly GUIContent vsyncContent = EditorGUIUtility.TrTextContent("VSync");
            public static readonly GUIContent gizmosContent = EditorGUIUtility.TrTextContent("Gizmos");
            public static readonly GUIContent toolbarContent = EditorGUIUtility.TrTextContent("Toolbar");
            public static readonly GUIContent statsContent = EditorGUIUtility.TrTextContent("Stats");
            public static readonly GUIContent displaySettingsLabelContent = EditorGUIUtility.TrTextContent("Display Settings");

            public static readonly GUIContent mainDisplayFormatContent = EditorGUIUtility.TrTextContent("{0} (Main Display)");
            public static readonly GUIContent disconnectedDisplayFormatContent = EditorGUIUtility.TrTextContent("{0} (Disconnected)");
            public static readonly GUIContent disableFullscreenMainDisplayFormatContent = EditorGUIUtility.TrTextContent("Press {0} to exit fullscreen.");

            public static readonly GUIContent displayAPIMappingContent = EditorGUIUtility.TrTextContent("Standalone simulation monitor mapping");
        }

        private static void DrawPreferenceGUI(string searchContext)
        {
            if (instance.m_profiles == null)
            {
                instance.InitializeProfile();
            }
            GUILayout.Space(12);

            EditorGUI.BeginChangeCheck();
            instance.m_isSortDisplayOrder = EditorGUILayout.ToggleLeft(Styles.sortDisplayOrderContent, instance.m_isSortDisplayOrder);
            if (EditorGUI.EndChangeCheck())
            {
                EditorDisplayUtility.SetSortDisplayOrder(instance.m_isSortDisplayOrder);
                Save();
            }

            EditorGUI.BeginChangeCheck();
            instance.m_showNotificationOnFullscreen = EditorGUILayout.ToggleLeft(Styles.showNotificationContent, instance.m_showNotificationOnFullscreen);
            instance.m_showToolbarOnFullscreen = EditorGUILayout.ToggleLeft(Styles.showToolbarOnFullscreenContent, instance.m_showToolbarOnFullscreen);
            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }

            GUILayout.Space(12);


            DrawDisplayAPIProfile();
            //DrawProfilePreferencesGUI(); //TODO Enables GUI for display profiles.
        }

        private static void DrawProfilePreferencesGUI()
        {
            EditorDisplaySettingsProfile removingProfile = null;
            EditorGUI.BeginChangeCheck();

            // m_profiles[0] is always Default Profile.
            // m_profiles[1] is always DisplayAPI Profile.
            for(var i = 2; i < instance.m_profiles.Count; ++i)
            {
                if (!DrawPreferenceProfileGUI(instance.m_profiles[i]))
                {
                    removingProfile = instance.m_profiles[i];
                    break;
                }
                GUILayout.Space(12);
            }

            if (removingProfile != null)
            {
                instance.m_profiles.Remove(removingProfile);
            }

            GUILayout.Space(12);

            if (GUILayout.Button(Styles.addProfileContent, GUILayout.Width(150)))
            {
                var newProfile = new EditorDisplaySettingsProfile(instance.GetAppropriateNewProfileName());
                instance.m_profiles.Add(newProfile);
            }

            GUILayout.Space(12);

            if (EditorGUI.EndChangeCheck())
            {
                Save();
            }
        }

        private string GetAppropriateNewProfileName()
        {
            const string kDefaultNewProfileName = "New Profile";
            if (m_profiles.Find(p => p.Name == kDefaultNewProfileName) == null)
            {
                return kDefaultNewProfileName;
            }

            for (var i = 1;; ++i)
            {
                var name = $"New Profile ({i})";
                if (m_profiles.Find(p => p.Name == name) == null)
                {
                    return name;
                }
            }
        }

        private static void DrawDisplayAPIProfile()
        {
            if (!ModuleManager.ShouldShowMultiDisplayOption())
            {
                return;
            }

            using (new GUILayout.VerticalScope(EditorStyles.frameBox))
            {
                GUILayout.Label(Styles.displayAPIMappingContent, EditorStyles.boldLabel);
                GUILayout.Space(12);

                var displayNames = GetDisplayNamesForBuildTarget(EditorUserBuildSettings.activeBuildTarget);

                for (var i = 0; i < instance.m_numberOfConnectedDisplays; ++i)
                {
                    EditorGUILayout.LabelField(displayNames[i], GUIContent.Temp(instance.m_displayNames[i]));
                }
            }
            GUILayout.Space(12);
        }

        private static bool DrawPreferenceProfileGUI(EditorDisplaySettingsProfile profile)
        {
            using (new GUILayout.VerticalScope(EditorStyles.frameBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(profile.Name, EditorStyles.boldLabel);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        return false;
                    }
                }

                GUILayout.Space(8);

                var newName = EditorGUILayout.TextField(Styles.nameContent, profile.Name, GUILayout.Width(400));
                if (newName != profile.Name)
                {
                    profile.Name = newName;
                }

                instance.GetInstalledBuildTargetData();

                var selectedIndex = Array.IndexOf(instance.m_buildTargetData.values, profile.Target);
                var newSelectedIndex = EditorGUILayout.Popup(Styles.platformContent, selectedIndex, instance.m_buildTargetData.displayNames, GUILayout.Width(400));
                if (selectedIndex != newSelectedIndex)
                {
                    profile.Target = (BuildTarget) instance.m_buildTargetData.values[newSelectedIndex];
                }

                // if selecting unsupported build target, then do not let settings to edit
                if (selectedIndex < 0)
                {
                    return true;
                }

                GUILayout.Space(8);

                for (var i = 0; i < instance.m_displayIds.Length; ++i)
                {
                    var id = instance.m_displayIds[i];
                    var isMainScreen = id == instance.m_mainDisplayId;

                    var setting = profile.GetEditorDisplayFullscreenSetting(id);
                    if (setting == null)
                    {
                        setting = new EditorDisplayFullscreenSetting(id, instance.m_displayNames[i]);
                        profile.AddEditorDisplayFullscreenSetting(setting);
                    }

                    DrawPlayModeViewSettingsGUI(profile.Target, setting, isMainScreen, false);
                    GUILayout.Space(8);
                }

                bool drawDisconnectedSettingsHeader = false;
                // For Display Settings which is currently disconnected
                EditorDisplayFullscreenSetting removingSetting = null;

                foreach (var setting in profile.Settings)
                {
                    if (instance.m_displayIds.Contains(setting.displayId))
                    {
                        continue;
                    }

                    if (!drawDisconnectedSettingsHeader)
                    {
                        GUILayout.Space(12);
                        GUILayout.Label(Styles.disconnectedDisplaysContent);
                        drawDisconnectedSettingsHeader = true;
                    }

                    if (!DrawPlayModeViewSettingsGUI(profile.Target, setting, false, true))
                    {
                        removingSetting = setting;
                    }
                    GUILayout.Space(8);
                }

                if (removingSetting != null)
                {
                    profile.RemoveEditorDisplayFullscreenSetting(removingSetting);
                }

                GUILayout.Space(12);
            }

            return true;
        }

        private static bool DrawPlayModeViewSettingsGUI(BuildTarget target, EditorDisplayFullscreenSetting setting, bool isMainScreen, bool isDisconnected)
        {
            if (isMainScreen)
            {
                GUILayout.Label(string.Format(Styles.mainDisplayFormatContent.text, setting.displayName));
                EditorGUILayout.HelpBox(Styles.mainDisplaySettingHelpTextContent);
                return true;
            }

            var label = isDisconnected ? string.Format(Styles.disconnectedDisplayFormatContent.text, setting.displayName) :  setting.displayName;

            using (new GUILayout.HorizontalScope())
            {
                setting.enabled = EditorGUILayout.ToggleLeft(label, setting.enabled);
                if (isDisconnected)
                {
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        return false;
                    }
                }
            }

            if (!setting.enabled)
            {
                return true;
            }

            using (new GUILayout.VerticalScope(EditorStyles.frameBox))
            {
                var availableTypes = instance.GetAvailableWindowTypes();
                if (availableTypes.Count > 1)
                {
                    var viewTitleNames = availableTypes.Values.ToList();
                    var viewIndex = viewTitleNames.IndexOf(setting.viewWindowTitle);
                    var newViewIndex = EditorGUILayout.Popup(
                        Styles.viewTypeContent, viewTitleNames.IndexOf(setting.viewWindowTitle),
                        viewTitleNames.ToArray(), GUILayout.Width(400));
                    if (newViewIndex != viewIndex)
                    {
                        setting.viewWindowTitle = viewTitleNames[newViewIndex];
                        setting.playModeViewSettings =
                            CreatePlayModeViewSettingsForType(availableTypes.Keys.ToList()[newViewIndex]);
                    }
                }
                else {
                    if (string.IsNullOrEmpty(setting.viewWindowTitle))
                    {
                        var typeNames = availableTypes.Values.ToList();
                        setting.viewWindowTitle = typeNames[0];
                        setting.playModeViewSettings =
                            CreatePlayModeViewSettingsForType(availableTypes.Keys.ToList()[0]);
                    }
                    EditorGUILayout.LabelField(Styles.viewTypeContent, new GUIContent(setting.viewWindowTitle), GUILayout.Width(300));
                }
                EditorGUILayout.Space();

                setting.mode = (EditorDisplayFullscreenSetting.Mode)EditorGUILayout.Popup((int)setting.mode, Styles.modePopupDisplayTexts, GUILayout.Width(200));
                if (setting.playModeViewSettings != null)
                {
                    GUILayout.Space(4);
                    setting.playModeViewSettings.OnPreferenceGUI(target);
                }
            }

            return true;
        }

        private static IPlayModeViewFullscreenSettings CreatePlayModeViewSettingsForType(Type playModeViewType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var settingsTypes = assembly.GetTypes()
                    .Where(t => !t.IsInterface)
                    .Where(t => typeof(IPlayModeViewFullscreenSettings).IsAssignableFrom(t));

                foreach (var settingsType in settingsTypes)
                {
                    var attributes = settingsType.GetCustomAttributes(typeof(FullscreenSettingsForAttribute), false);
                    if (attributes.Length > 0 && ((FullscreenSettingsForAttribute)attributes[0]).AssignedType == playModeViewType)
                    {
                        return (IPlayModeViewFullscreenSettings) settingsType.Assembly.CreateInstance(settingsType.FullName);
                    }
                }
            }
            return null;
        }

        internal static GUIContent[] GetDisplayNamesForBuildTarget(BuildTarget buildTarget)
        {
            var platformDisplayNames = Modules.ModuleManager.GetDisplayNames(buildTarget.ToString());
            return platformDisplayNames ?? DisplayUtility.GetGenericDisplayNames();
        }


        [SettingsProvider]
        internal static SettingsProvider CreateEditorDisplaySettingUserPreference()
        {
            var provider = new SettingsProvider("Preferences/Display Settings", SettingsScope.User)
            {
                label = Styles.displaySettingsLabelContent.text,
                guiHandler = DrawPreferenceGUI,
                activateHandler = (s, element) =>
                {
                    instance.UpdateDisplayNamesAndIds();
                },
                deactivateHandler = () =>
                {
                    instance.RefreshDisplayStateWithCurrentProfile();
                    instance.ReloadWindowDisplayMenu();
                },
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>()
            };
            return provider;
        }
    }
} // namespace
