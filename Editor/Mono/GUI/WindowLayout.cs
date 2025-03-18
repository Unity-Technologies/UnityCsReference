// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.ShortcutManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;
using Directory = System.IO.Directory;
using UnityObject = UnityEngine.Object;
using JSONObject = System.Collections.IDictionary;

namespace UnityEditor
{
    class LayoutException : Exception
    {
        public LayoutException()
        {
        }

        public LayoutException(string message)
            : base(message)
        {
        }
    }

    [InitializeOnLoad]
    internal static class WindowLayout
    {
        struct LayoutViewInfo
        {
            public LayoutViewInfo(object key, float defaultSize, bool usedByDefault)
            {
                this.key = key;
                used = usedByDefault;
                this.defaultSize = defaultSize;

                className = string.Empty;
                type = null;
                isContainer = false;
                extendedData = null;
            }

            public object key;
            public string className;
            public Type type;
            public bool used;
            public float defaultSize;
            public bool isContainer;
            public JSONObject extendedData;

            public float size
            {
                get
                {
                    if (!used)
                        return 0;
                    return defaultSize;
                }
            }
        }

        const string tabsLayoutKey = "tabs";
        const string verticalLayoutKey = "vertical";
        const string horizontalLayoutKey = "horizontal";

        const string k_TopViewClassName = "top_view";
        const string k_CenterViewClassName = "center_view";
        const string k_BottomViewClassName = "bottom_view";

        // used by tests
        internal const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";
        private const string kDefaultLayoutName = "Default.wlt";
        internal static string layoutResourcesPath => Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts");
        internal static string layoutsPreferencesPath => FileUtil.CombinePaths(InternalEditorUtility.unityPreferencesFolder, "Layouts");
        internal static string layoutsModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, ModeService.currentId);
        internal static string layoutsDefaultModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, "default");
        internal static string layoutsCurrentModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, "current");
        internal static string layoutsProjectPath => FileUtil.CombinePaths("UserSettings", "Layouts");
        internal static string ProjectLayoutPath => GetProjectLayoutPerMode(ModeService.currentId);
        internal static string currentLayoutName => GetLayoutFileName(ModeService.currentId, Application.unityVersionVer);

        [UsedImplicitly, RequiredByNativeCode]
        public static void LoadDefaultWindowPreferences()
        {
            LoadCurrentModeLayout(keepMainWindow: FindMainWindow());
            ModeService.InitializeCurrentMode();
        }

        public static void LoadCurrentModeLayout(bool keepMainWindow)
        {
            InitializeLayoutPreferencesFolder();
            var dynamicLayout = ModeService.GetDynamicLayout();
            if (dynamicLayout == null)
                LoadLastUsedLayoutForCurrentMode(keepMainWindow);
            else
            {
                var projectLayoutExists = File.Exists(ProjectLayoutPath);
                if ((projectLayoutExists && Convert.ToBoolean(dynamicLayout["restore_saved_layout"]))
                    || !LoadModeDynamicLayout(keepMainWindow, dynamicLayout))
                    LoadLastUsedLayoutForCurrentMode(keepMainWindow);
            }
        }

        private static bool LoadModeDynamicLayout(bool keepMainWindow, JSONObject layoutData)
        {
            LayoutViewInfo topViewInfo = new LayoutViewInfo(k_TopViewClassName, MainView.kToolbarHeight, true);
            LayoutViewInfo bottomViewInfo = new LayoutViewInfo(k_BottomViewClassName, MainView.kStatusbarHeight, true);
            LayoutViewInfo centerViewInfo = new LayoutViewInfo(k_CenterViewClassName, 0, true);

            var availableEditorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray();

            if (!GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref centerViewInfo))
                return false;

            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref topViewInfo);
            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref bottomViewInfo);

            var mainWindow = FindMainWindow();
            if (keepMainWindow && mainWindow == null)
            {
                Debug.LogWarning($"No main window to restore layout from while loading dynamic layout for mode {ModeService.currentId}");
                return false;
            }

            var mainViewID = $"MainView_{ModeService.currentId}";
            InitContainerWindow(ref mainWindow, mainViewID, layoutData);
            GenerateLayout(mainWindow, ShowMode.MainWindow, availableEditorWindowTypes, centerViewInfo, topViewInfo, bottomViewInfo, layoutData);
            mainWindow.m_DontSaveToLayout = !Convert.ToBoolean(layoutData["restore_saved_layout"]);
            return true;
        }

        private static View LoadLayoutView<T>(Type[] availableEditorWindowTypes, LayoutViewInfo viewInfo, float width, float height) where T : View
        {
            if (!viewInfo.used)
                return null;

            View view = null;
            if (viewInfo.isContainer)
            {
                bool useTabs = viewInfo.extendedData.Contains(tabsLayoutKey) && Convert.ToBoolean(viewInfo.extendedData[tabsLayoutKey]);
                bool useSplitter = viewInfo.extendedData.Contains(verticalLayoutKey) || viewInfo.extendedData.Contains(horizontalLayoutKey);
                bool isVertical = viewInfo.extendedData.Contains(verticalLayoutKey) && Convert.ToBoolean(viewInfo.extendedData[verticalLayoutKey]);

                if (useTabs && useSplitter)
                    Debug.LogWarning($"{ModeService.currentId} defines both tabs and splitter (horizontal or vertical) layouts.\n You can only define one to true (i.e. tabs = true) in the editor mode file.");

                if (useSplitter)
                {
                    var splitView = ScriptableObject.CreateInstance<SplitView>();
                    splitView.vertical = isVertical;
                    view = splitView;
                }
                else if (useTabs)
                {
                    var dockAreaView = ScriptableObject.CreateInstance<DockArea>();
                    view = dockAreaView;
                }

                view.position = new Rect(0, 0, width, height);

                var childrenData = viewInfo.extendedData["children"] as IList;
                if (childrenData == null)
                    throw new LayoutException("Invalid split view data");

                int childIndex = 0;
                foreach (var childData in childrenData)
                {
                    var lvi = new LayoutViewInfo(childIndex, useTabs ? 1f : 1f / childrenData.Count, true);
                    if (!ParseViewData(availableEditorWindowTypes, childData, ref lvi))
                        continue;

                    var cw = useTabs ? width : (isVertical ? width : width * lvi.size);
                    var ch = useTabs ? height : (isVertical ? height * lvi.size : height);
                    if (useTabs && view is DockArea da)
                    {
                        da.AddTab((EditorWindow)ScriptableObject.CreateInstance(lvi.type));
                    }
                    else
                    {
                        view.AddChild(LoadLayoutView<HostView>(availableEditorWindowTypes, lvi, cw, ch));
                        view.children[childIndex].position = new Rect(0, 0, cw, ch);
                    }

                    childIndex++;
                }
            }
            else
            {
                if (viewInfo.type != null)
                {
                    var hostView = ScriptableObject.CreateInstance<HostView>();
                    hostView.SetActualViewInternal(ScriptableObject.CreateInstance(viewInfo.type) as EditorWindow, true);
                    view = hostView;
                }
                else
                    view = ScriptableObject.CreateInstance<T>();
            }

            return view;
        }

        internal static void InitContainerWindow(ref ContainerWindow window, string windowId, JSONObject layoutData)
        {
            if (window == null)
            {

                window = ScriptableObject.CreateInstance<ContainerWindow>();

                var windowMinSize = new Vector2(120, 80);
                var windowMaxSize = new Vector2(8192, 8192);
                if (layoutData.Contains("min_width"))
                {
                    windowMinSize.x = Convert.ToSingle(layoutData["min_width"]);
                }
                if (layoutData.Contains("min_height"))
                {
                    windowMinSize.y = Convert.ToSingle(layoutData["min_height"]);
                }
                if (layoutData.Contains("max_width"))
                {
                    windowMaxSize.x = Convert.ToSingle(layoutData["max_width"]);
                }
                if (layoutData.Contains("max_height"))
                {
                    windowMaxSize.y = Convert.ToSingle(layoutData["max_height"]);
                }
                window.SetMinMaxSizes(windowMinSize, windowMaxSize);
            }

            var hasMainViewGeometrySettings = EditorPrefs.HasKey($"{windowId}h");
            window.windowID = windowId;
            var loadInitialWindowGeometry = Convert.ToBoolean(layoutData["restore_layout_dimension"]);
            if (loadInitialWindowGeometry && hasMainViewGeometrySettings)
                window.LoadGeometry(true);
        }

        internal static ContainerWindow FindMainWindow()
        {
            return Resources.FindObjectsOfTypeAll<ContainerWindow>().FirstOrDefault(w => w.showMode == ShowMode.MainWindow);
        }

        internal static ContainerWindow ShowWindowWithDynamicLayout(string windowId, string layoutDataPath)
        {
            try
            {
                ContainerWindow.SetFreezeDisplay(true);
                if (!File.Exists(layoutDataPath))
                {
                    Debug.LogError($"Failed to find layout data file at path {layoutDataPath}");
                    return null;
                }

                var layoutDataJson = File.ReadAllText(layoutDataPath);
                var layoutData = SJSON.LoadString(layoutDataJson);

                var availableEditorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray();

                LayoutViewInfo topViewInfo = new LayoutViewInfo(k_TopViewClassName, MainView.kToolbarHeight, false);
                LayoutViewInfo bottomViewInfo = new LayoutViewInfo(k_BottomViewClassName, MainView.kStatusbarHeight, false);

                // Supports both view and center_view.
                var centerViewKey = "view";
                if (!layoutData.Contains(centerViewKey))
                {
                    centerViewKey = "center_view";
                }
                LayoutViewInfo centerViewInfo = new LayoutViewInfo(centerViewKey, 0, true);
                if (!GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref centerViewInfo))
                {
                    Debug.LogError("Failed to load window layout; no view defined");
                    return null;
                }

                GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref topViewInfo);
                GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref bottomViewInfo);

                var window = Resources.FindObjectsOfTypeAll<ContainerWindow>().FirstOrDefault(w => w.windowID == windowId);
                InitContainerWindow(ref window, windowId, layoutData);
                GenerateLayout(window, ShowMode.Utility, availableEditorWindowTypes, centerViewInfo, topViewInfo, bottomViewInfo, layoutData);
                window.m_DontSaveToLayout = !Convert.ToBoolean(layoutData["restore_saved_layout"]);
                return window;
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }
        }

        private static void GenerateLayout(ContainerWindow window, ShowMode showMode, Type[] availableEditorWindowTypes,
            LayoutViewInfo center, LayoutViewInfo top, LayoutViewInfo bottom, JSONObject layoutData)
        {
            try
            {
                ContainerWindow.SetFreezeDisplay(true);
                var width = window.position.width;
                var height = window.position.height;

                // Create center view
                View centerView = LoadLayoutView<DockArea>(availableEditorWindowTypes, center, width, height);
                var topView = LoadLayoutView<Toolbar>(availableEditorWindowTypes, top, width, height);
                var bottomView = LoadLayoutView<AppStatusBar>(availableEditorWindowTypes, bottom, width, height);

                var main = ScriptableObject.CreateInstance<MainView>();
                main.useTopView = top.used;
                main.useBottomView = bottom.used;
                main.topViewHeight = top.size;
                main.bottomViewHeight = bottom.size;

                // Top View
                if (topView)
                {
                    topView.position = new Rect(0, 0, width, top.size);
                    main.AddChild(topView);
                }

                // Center View
                var centerViewHeight = height - bottom.size - top.size;
                centerView.position = new Rect(0, top.size, width, centerViewHeight);
                main.AddChild(centerView);

                // Bottom View
                if (bottomView)
                {
                    bottomView.position = new Rect(0, height - bottom.size, width, bottom.size);
                    main.AddChild(bottomView);
                }

                if (window.rootView)
                    ScriptableObject.DestroyImmediate(window.rootView, true);

                window.rootView = main;
                window.rootView.position = new Rect(0, 0, width, height);
                window.Show(showMode, true, true, true);
                window.DisplayAllViews();
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }
        }

        private static bool GetLayoutViewInfo(JSONObject layoutData, Type[] availableEditorWindowTypes, ref LayoutViewInfo viewInfo)
        {
            if (!layoutData.Contains(viewInfo.key))
                return false;

            var viewData = layoutData[viewInfo.key];
            return ParseViewData(availableEditorWindowTypes, viewData, ref viewInfo);
        }

        private static bool ParseViewData(Type[] availableEditorWindowTypes, object viewData, ref LayoutViewInfo viewInfo)
        {
            if (viewData is string)
            {
                viewInfo.className = Convert.ToString(viewData);
                viewInfo.used = !string.IsNullOrEmpty(viewInfo.className);
                if (!viewInfo.used)
                    return true;
            }
            else if (viewData is JSONObject viewExpandedData)
            {
                if (viewExpandedData.Contains("children")
                    || viewExpandedData.Contains(verticalLayoutKey)
                    || viewExpandedData.Contains(horizontalLayoutKey)
                    || viewExpandedData.Contains(tabsLayoutKey))
                {
                    viewInfo.isContainer = true;
                    viewInfo.className = string.Empty;
                }
                else
                {
                    viewInfo.isContainer = false;
                    viewInfo.className = Convert.ToString(viewExpandedData["class_name"]);
                }

                if (viewExpandedData.Contains("size"))
                    viewInfo.defaultSize = Convert.ToSingle(viewExpandedData["size"]);
                viewInfo.extendedData = viewExpandedData;
                viewInfo.used = true;
            }
            else
            {
                viewInfo.className = string.Empty;
                viewInfo.type = null;
                viewInfo.used = false;
                return true;
            }

            if (string.IsNullOrEmpty(viewInfo.className))
                return true;

            foreach (var t in availableEditorWindowTypes)
            {
                if (t.Name != viewInfo.className)
                    continue;
                viewInfo.type = t;
                break;
            }

            if (viewInfo.type == null)
            {
                Debug.LogWarning($"Invalid layout view {viewInfo.key} with type {viewInfo.className} for mode {ModeService.currentId}");
                return false;
            }

            return true;
        }

        // Used by tests
        internal static string GetLayoutFileName(string mode, int version) => $"{mode}-{version}.dwlt";

        static IEnumerable<string> GetCurrentModeLayouts()
        {
            var layouts = ModeService.GetModeDataSection(ModeService.currentIndex, ModeDescriptor.LayoutsKey);

            if (layouts is IList<object> modeLayoutPaths)
            {
                foreach (var layoutPath in modeLayoutPaths.Cast<string>())
                {
                    if (!File.Exists(layoutPath))
                        continue;
                    yield return layoutPath;
                }
            }
        }

        // Iterate through potential layouts in descending order of precedence.
        // 1. Last loaded layout in project for matching Unity version
        // 2. Last loaded layout in project for any Unity version, in descending alphabetical order
        // 3. Last loaded layout in global preferences for matching Unity version
        // 4. Last loaded layout in global preferences for any Unity version, in descending alphabetical order
        // 5. Any available layouts specified by the EditorMode, if EditorMode supplies layouts
        // 6. The factory default layout
        private static void LoadLastUsedLayoutForCurrentMode(bool keepMainWindow)
        {
            // steps 1-4
            foreach (var layout in GetLastLayout())
                if (LoadWindowLayout_Internal(layout, layout != ProjectLayoutPath, false, keepMainWindow, false))
                    return;

            // step 5
            foreach (var layout in GetCurrentModeLayouts())
                if (LoadWindowLayout_Internal(layout, layout != ProjectLayoutPath, false, keepMainWindow, false))
                    return;

            // It is not mandatory that modes define a layout. In that case, skip right to the default layout.
            if (!string.IsNullOrEmpty(ModeService.GetDefaultModeLayout())
                && LoadWindowLayout_Internal(ModeService.GetDefaultModeLayout(), true, false, keepMainWindow, false))
                return;

            // If all else fails, load the default layout that ships with the editor. If that fails, prompt the user to
            // restore the default layouts.
            if (!LoadWindowLayout_Internal(GetDefaultLayoutPath(), true, false, keepMainWindow, false))
            {
                int option = 0;

                if (!Application.isTestRun && Application.isHumanControllingUs)
                {
                    option = EditorUtility.DisplayDialogComplex("Missing Default Layout", "No valid user created or " +
                        "default window layout found. Please revert factory settings to restore the default layouts.",
                        "Quit", "Revert Factory Settings", "");
                }
                else
                {
                    ResetUserLayouts();
                }

                switch (option)
                {
                    case 0:
                        EditorApplication.Exit(0);
                        break;
                    case 1:
                        ResetFactorySettings();
                        break;
                }
            }
        }

        [UsedImplicitly, RequiredByNativeCode]
        public static void SaveDefaultWindowPreferences()
        {
            // Do not save layout to default if running tests
            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            SaveCurrentLayoutPerMode(ModeService.currentId);
        }

        internal static void SaveCurrentLayoutPerMode(string modeId)
        {
            // Save the layout in two places. Once in the Project/UserSettings directory, then again the global
            // preferences. The latter is used when opening a new project (or any case where UserSettings/Layouts/ does
            // not exist).
            SaveWindowLayout(FileUtil.CombinePaths(Directory.GetCurrentDirectory(), GetProjectLayoutPerMode(modeId)));
            SaveWindowLayout(Path.Combine(layoutsCurrentModePreferencesPath, GetLayoutFileName(modeId, Application.unityVersionVer)));
        }

        // Iterate through potential layout files, prioritizing exact match followed by descending unity version.
        // IMPORTANT: This function is "dumb" in that it does not do any kind of sophisticated version comparison. If the
        // naming scheme for current layouts is changed, or this function is called on to sort user saved layouts, you will
        // need to add more sophisticated filtering.
        public static IEnumerable<string> GetLastLayout(string directory, string mode, int version)
        {
            var currentModeAndVersionLayout = GetLayoutFileName(mode, version);
            string layoutSearchPattern = $"{mode}-*.*wlt";

            // first try the exact match
            var preferred = Path.Combine(directory, currentModeAndVersionLayout);

            if(File.Exists(preferred))
                yield return preferred;

            // if that fails, fall back to layouts for this mode from other unity versions in descending order
            if (Directory.Exists(directory))
            {
                var paths = Directory.GetFiles(directory, layoutSearchPattern)
                                     .Where(p => string.Compare(p, preferred, StringComparison.OrdinalIgnoreCase) != 0)
                                     .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var path in paths)
                    yield return path;
            }
        }

        // used by Tests/EditModeAndPlayModeTests/EditorModes
        internal static IEnumerable<string> GetLastLayout()
        {
            var mode = ModeService.currentId;
            var version = Application.unityVersionVer;
            foreach (var layout in GetLastLayout(layoutsProjectPath, mode, version))
                yield return layout;
            foreach (var layout in GetLastLayout(layoutsCurrentModePreferencesPath, mode, version))
                yield return layout;
        }

        internal static string GetCurrentLayoutPath()
        {
            var currentLayoutPath = ProjectLayoutPath;

            // Make sure we have a current layout file created
            if (!File.Exists(ProjectLayoutPath))
                currentLayoutPath = GetDefaultLayoutPath();

            return currentLayoutPath;
        }

        internal static string GetDefaultLayoutPath()
        {
            return Path.Combine(layoutsModePreferencesPath, kDefaultLayoutName);
        }

        internal static string GetProjectLayoutPerMode(string modeId)
        {
            return FileUtil.CombinePaths(layoutsProjectPath, GetLayoutFileName(modeId, Application.unityVersionVer));
        }

        private static void InitializeLayoutPreferencesFolder()
        {
            string defaultLayoutPath = GetDefaultLayoutPath();

            if (!Directory.Exists(layoutsPreferencesPath))
                Directory.CreateDirectory(layoutsPreferencesPath);

            if (!Directory.Exists(layoutsModePreferencesPath))
            {
                Console.WriteLine($"[LAYOUT] {layoutsModePreferencesPath} does not exist. Copying base layouts.");
                // Make sure we have a valid default mode folder initialized with the proper default layouts.
                if (layoutsDefaultModePreferencesPath == layoutsModePreferencesPath)
                {
                    // Backward compatibility: if the default layout folder doesn't exists but some layouts have been
                    // saved be sure to copy them to the "default layout per mode folder".
                    FileUtil.CopyFileOrDirectory(layoutResourcesPath, layoutsDefaultModePreferencesPath);
                    var defaultModeUserLayouts = Directory.GetFiles(layoutsPreferencesPath, "*.wlt");
                    foreach (var layoutPath in defaultModeUserLayouts)
                    {
                        var fileName = Path.GetFileName(layoutPath);
                        var dst = Path.Combine(layoutsDefaultModePreferencesPath, fileName);
                        if (!File.Exists(dst))
                            FileUtil.CopyFileIfExists(layoutPath, dst, false);
                    }
                }
                else
                {
                    Directory.CreateDirectory(layoutsModePreferencesPath);
                }
            }

            // Make sure we have the default layout file in the preferences folder
            if (!File.Exists(defaultLayoutPath))
            {
                var defaultModeLayoutPath = ModeService.GetDefaultModeLayout();
                if (!File.Exists(defaultModeLayoutPath))
                {
                    // No mode default layout, use the editor_resources Default:
                    defaultModeLayoutPath = Path.Combine(layoutResourcesPath, kDefaultLayoutName);
                }
                Console.WriteLine($"[LAYOUT] Copying {defaultModeLayoutPath} to {defaultLayoutPath}");
                // If not copy our default file to the preferences folder
                FileUtil.CopyFileOrDirectory(defaultModeLayoutPath, defaultLayoutPath);
            }
            Debug.Assert(File.Exists(defaultLayoutPath));
        }

        static WindowLayout()
        {
            EditorApplication.CallDelayed(UpdateWindowLayoutMenu);
        }

        internal static void UpdateWindowLayoutMenu()
        {
            if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
                return;
            ReloadWindowLayoutMenu();
        }

        internal static void ReloadWindowLayoutMenu()
        {
            Menu.RemoveMenuItem("Window/Layouts");

            if (!ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true))
                return;

            int layoutMenuItemPriority = -20;

            // Get user saved layouts
            string[] layoutPaths = new string[0];
            if (Directory.Exists(layoutsModePreferencesPath))
            {
                layoutPaths = Directory.GetFiles(layoutsModePreferencesPath).Where(path => path.EndsWith(".wlt")).ToArray();
                foreach (var layoutPath in layoutPaths)
                {
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", false, layoutMenuItemPriority++, () => TryLoadWindowLayout(layoutPath, false, true, true, true), null);
                }

                layoutMenuItemPriority += 500;
            }

            // Get all version current layouts
            AddLegacyLayoutMenuItems(ref layoutMenuItemPriority);

            // Get mode layouts
            var modeLayoutPaths = ModeService.GetModeDataSection(ModeService.currentIndex, ModeDescriptor.LayoutsKey) as IList<object>;
            if (modeLayoutPaths != null)
            {
                foreach (var layoutPath in modeLayoutPaths.Cast<string>())
                {
                    if (!File.Exists(layoutPath))
                        continue;
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", Toolbar.lastLoadedLayoutName == name, layoutMenuItemPriority++, () => TryLoadWindowLayout(layoutPath, false), null);
                }
            }

            layoutMenuItemPriority += 500;

            Menu.AddMenuItem("Window/Layouts/Save Layout...", "", false, layoutMenuItemPriority++, SaveGUI, null);
            Menu.AddMenuItem("Window/Layouts/Save Layout to File...", "", false, layoutMenuItemPriority++, SaveToFile, null);
            Menu.AddMenuItem("Window/Layouts/Load Layout from File...", "", false, layoutMenuItemPriority++, LoadFromFile, null);
            Menu.AddMenuItem("Window/Layouts/Delete Layout/", "", false, layoutMenuItemPriority++, null, null);
            foreach (var layoutPath in layoutPaths)
            {
                var name = Path.GetFileNameWithoutExtension(layoutPath);
                Menu.AddMenuItem("Window/Layouts/Delete Layout/" + name, "", false, layoutMenuItemPriority++, () => DeleteWindowLayout(layoutPath), null);
            }
            Menu.AddMenuItem("Window/Layouts/Reset All Layouts", "", false, layoutMenuItemPriority++, () => ResetAllLayouts(false), null);
        }

        private static void AddLegacyLayoutMenuItems(ref int layoutMenuItemPriority)
        {
            const string legacyRootMenu = "Window/Layouts/Other Versions";
            const string legacyCurrentLayoutPath = "Library/CurrentLayout-default.dwlt";
            if (File.Exists(legacyCurrentLayoutPath))
                Menu.AddMenuItem($"{legacyRootMenu}/Default (2020)", "", false, layoutMenuItemPriority++, () => TryLoadWindowLayout(legacyCurrentLayoutPath, false, true, false, true), null);

            if (!Directory.Exists(layoutsProjectPath))
                return;

            var currentMaximizeLayoutPath = FileUtil.CombinePaths(layoutsProjectPath, kMaximizeRestoreFile);
            using var pooled = HashSetPool<string>.Get(out var forbiddenPaths);
            forbiddenPaths.Add(GetCurrentLayoutPath());
            forbiddenPaths.Add(currentMaximizeLayoutPath);
            foreach (var layoutPath in Directory.GetFiles(layoutsProjectPath, "*.dwlt"))
            {
                if (forbiddenPaths.Contains(layoutPath))
                    continue;
                var name = Path.GetFileNameWithoutExtension(layoutPath);
                var names = Path.GetFileName(name).Split('-');
                var menuName = $"{legacyRootMenu}/{name}";
                if (names.Length == 2)
                {
                    name = ObjectNames.NicifyVariableName(names[0]);
                    menuName = $"{legacyRootMenu}/{name} ({names[1]})";
                }
                Menu.AddMenuItem(menuName, "", false, layoutMenuItemPriority++, () => TryLoadWindowLayout(layoutPath, false, true, false, true), null);
            }
        }

        internal static EditorWindow FindEditorWindowOfType(Type type)
        {
            UnityObject[] obj = Resources.FindObjectsOfTypeAll(type);
            if (obj.Length > 0)
                return obj[0] as EditorWindow;
            return null;
        }

        internal static void CheckWindowConsistency()
        {
            UnityObject[] wins = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));

            foreach (EditorWindow win in wins)
            {
                if (win.m_Parent == null)
                {
                    Debug.LogErrorFormat(
                        "Invalid editor window of type: {0}, title: {1}",
                        win.GetType(), win.titleContent.text);
                }
            }
        }

        internal static EditorWindow TryGetLastFocusedWindowInSameDock()
        {
            // Get type of window that was docked together with game view and was focused before play mode
            Type type = null;
            string windowTypeName = WindowFocusState.instance.m_LastWindowTypeInSameDock;
            if (windowTypeName != "")
                type = Type.GetType(windowTypeName);

            // Also get the PlayModeView Window
            var playModeView = PlayModeView.GetMainPlayModeView();
            if (type != null && playModeView && playModeView != null && playModeView.m_Parent is DockArea)
            {
                // Get all windows of that type
                object[] potentials = Resources.FindObjectsOfTypeAll(type);

                DockArea dock = playModeView.m_Parent as DockArea;

                // Find the one that is actually docked together with the GameView
                for (int i = 0; i < potentials.Length; i++)
                {
                    EditorWindow window = potentials[i] as EditorWindow;
                    if (window && window.m_Parent == dock)
                        return window;
                }
            }

            return null;
        }

        internal static void SaveCurrentFocusedWindowInSameDock(EditorWindow windowToBeFocused)
        {
            if (windowToBeFocused.m_Parent != null && windowToBeFocused.m_Parent is DockArea)
            {
                DockArea dock = windowToBeFocused.m_Parent as DockArea;

                // Get currently focused window/tab in that dock
                EditorWindow actualView = dock.actualView;
                if (actualView)
                    WindowFocusState.instance.m_LastWindowTypeInSameDock = actualView.GetType().ToString();
            }
        }

        internal static void FindFirstGameViewAndSetToMaximizeOnPlay()
        {
            GameView gameView = (GameView)FindEditorWindowOfType(typeof(GameView));
            if (gameView)
                gameView.enterPlayModeBehavior = PlayModeView.EnterPlayModeBehavior.PlayMaximized;
        }

        internal static void FindFirstGameViewAndSetToPlayFocused()
        {
            GameView gameView = (GameView)FindEditorWindowOfType(typeof(GameView));
            if (gameView)
                gameView.enterPlayModeBehavior = PlayModeView.EnterPlayModeBehavior.PlayFocused;
        }

        internal static EditorWindow TryFocusAppropriateWindow(bool enteringPlaymode)
        {
            // If PlayModeView behavior is set to 'Do Nothing' ignore focusing windows when entering/exiting PlayMode
            var playModeView = PlayModeView.GetCorrectPlayModeViewToFocus();
            bool shouldFocusView = playModeView && playModeView.enterPlayModeBehavior != PlayModeView.EnterPlayModeBehavior.PlayUnfocused;

            if (enteringPlaymode)
            {
                if (shouldFocusView)
                {
                    SaveCurrentFocusedWindowInSameDock(playModeView);
                    playModeView.Focus();
                }

                return playModeView;
            }
            else
            {
                // If we can retrieve what window type was active when we went into play mode,
                // go back to focus a window of that type if needed.
                if (shouldFocusView)
                {
                    EditorWindow window = TryGetLastFocusedWindowInSameDock();
                    if (window)
                        window.ShowTab();
                    return window;
                }

                return EditorWindow.focusedWindow;
            }
        }

        internal static EditorWindow GetMaximizedWindow()
        {
            UnityObject[] maximized = Resources.FindObjectsOfTypeAll(typeof(MaximizedHostView));
            if (maximized.Length != 0)
            {
                MaximizedHostView maximizedView = maximized[0] as MaximizedHostView;
                if (maximizedView.actualView)
                    return maximizedView.actualView;
            }

            return null;
        }

        internal static EditorWindow ShowAppropriateViewOnEnterExitPlaymode(bool entering)
        {
            // Prevent trying to go into the same state as we're already in, as it will break things
            if (WindowFocusState.instance.m_CurrentlyInPlayMode == entering)
                return null;

            WindowFocusState.instance.m_CurrentlyInPlayMode = entering;

            EditorWindow window = null;
            EditorWindow maximized = GetMaximizedWindow();

            if (entering)
            {
                if (!GameView.openWindowOnEnteringPlayMode && !(PlayModeView.GetCorrectPlayModeViewToFocus() is PlayModeView))
                    return null;

                WindowFocusState.instance.m_WasMaximizedBeforePlay = (maximized != null);

                // If a view is already maximized before entering play mode,
                // just keep that maximized view, no matter if it's the game view or some other.
                // Trust that user has a good reason (desire by Ethan etc.)
                if (maximized != null)
                    return maximized;
            }
            else
            {
                // If a view was already maximized before entering play mode,
                // then it was kept when switching to play mode, and can simply still be kept when exiting
                if (WindowFocusState.instance.m_WasMaximizedBeforePlay)
                    return maximized;
            }

            // Unmaximize if maximized
            if (maximized)
                Unmaximize(maximized);

            // When the playmode behaviour is set to Play Unfocused
            if (entering)
            {
                var playmodeView = PlayModeView.GetCorrectPlayModeViewToFocus();
                if (playmodeView != null && playmodeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayUnfocused)
                {
                    playmodeView.m_Parent.OnLostFocus();
                    return playmodeView;
                }
            }

            // Try finding and focusing appropriate window/tab
            window = TryFocusAppropriateWindow(entering);
            if (window)
                return window;

            // If we are entering Play more and no Game View was found, create one
            if (entering && PlayModeView.openWindowOnEnteringPlayMode)
            {
                // Try to create and focus a Game View tab docked together with the Scene View tab
                EditorWindow sceneView = FindEditorWindowOfType(typeof(SceneView));
                GameView gameView;
                if (sceneView && sceneView.m_Parent is DockArea)
                {
                    DockArea dock = sceneView.m_Parent as DockArea;
                    if (dock)
                    {
                        WindowFocusState.instance.m_LastWindowTypeInSameDock = sceneView.GetType().ToString();
                        gameView = ScriptableObject.CreateInstance<GameView>();
                        dock.AddTab(gameView);
                        return gameView;
                    }
                }

                // If no Scene View was found at all, just create a floating Game View
                gameView = ScriptableObject.CreateInstance<GameView>();
                gameView.Show(true);
                gameView.Focus();

                return gameView;
            }

            return window;
        }

        internal static bool IsMaximized(EditorWindow window)
        {
            return window.m_Parent is MaximizedHostView;
        }

        public static void Unmaximize(EditorWindow win)
        {
            HostView maximizedHostView = win.m_Parent;
            if (maximizedHostView == null)
            {
                Debug.LogError("Host view was not found");
                ResetAllLayouts();
                return;
            }

            var restoreLayout = Path.Combine(layoutsProjectPath, kMaximizeRestoreFile);
            UnityObject[] newWindows = InternalEditorUtility.LoadSerializedFileAndForget(restoreLayout);

            if (newWindows.Length < 2)
            {
                Debug.LogError("Maximized serialized file backup not found.");
                ResetAllLayouts();
                return;
            }

            SplitView oldRoot = newWindows[0] as SplitView;
            EditorWindow oldWindow = newWindows[1] as EditorWindow;

            if (oldRoot == null)
            {
                Debug.LogError("Maximization failed because the root split view was not found.");
                ResetAllLayouts();
                return;
            }

            ContainerWindow parentWindow = win.m_Parent.window;
            if (parentWindow == null)
            {
                Debug.LogError("Maximization failed because the root split view has no container window.");
                ResetAllLayouts();
                return;
            }

            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                // Put the loaded SplitView where the MaximizedHostView was
                if (maximizedHostView.parent)
                {
                    int i = maximizedHostView.parent.IndexOfChild(maximizedHostView);
                    Rect r = maximizedHostView.position;
                    View parent = maximizedHostView.parent;
                    parent.RemoveChild(i);
                    parent.AddChild(oldRoot, i);
                    oldRoot.position = r;

                    // Move the Editor Window to the right spot in the
                    DockArea newDockArea = oldWindow.m_Parent as DockArea;

                    int oldDockAreaIndex = newDockArea.m_Panes.IndexOf(oldWindow);

                    maximizedHostView.actualView = null;
                    win.m_Parent = null;
                    newDockArea.AddTab(oldDockAreaIndex, win);
                    newDockArea.RemoveTab(oldWindow);
                    UnityObject.DestroyImmediate(oldWindow);

                    foreach (UnityObject o in newWindows)
                    {
                        EditorWindow curWin = o as EditorWindow;
                        if (curWin != null)
                            curWin.MakeParentsSettingsMatchMe();
                    }

                    parent.Initialize(parent.window);

                    //If parent window had to be resized, call this to make sure new size gets propagated
                    parent.position = parent.position;
                    oldRoot.Reflow();
                }
                else
                {
                    throw new LayoutException("No parent view");
                }

                // Kill the maximizedMainView
                UnityObject.DestroyImmediate(maximizedHostView);

                win.Focus();

                var gv = win as GameView;
                if (gv != null)
                    gv.m_Parent.EnableVSync(gv.vSyncEnabled);

                parentWindow.DisplayAllViews();
                win.m_Parent.MakeVistaDWMHappyDance();
            }
            catch (Exception ex)
            {
                Debug.Log("Maximization failed: " + ex);
                ResetAllLayouts();
            }

            try
            {
                // Weird bug on AMD graphic cards under OSX Lion: Sometimes when unmaximizing we get stray white rectangles.
                // work around that by issuing an extra repaint (case 438764)
                if (Application.platform == RuntimePlatform.OSXEditor && SystemInfo.operatingSystem.Contains("10.7") && SystemInfo.graphicsDeviceVendor.Contains("ATI"))
                {
                    foreach (GUIView v in Resources.FindObjectsOfTypeAll(typeof(GUIView)))
                        v.Repaint();
                }
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }
        }

        internal static void MaximizeGestureHandler()
        {
            if (Event.current.type != EditorGUIUtility.magnifyGestureEventType || GUIUtility.hotControl != 0)
                return;

            var mouseOverWindow = EditorWindow.mouseOverWindow;
            if (mouseOverWindow == null)
                return;

            var args = new ShortcutArguments { stage = ShortcutStage.End };
            if (IsMaximized(mouseOverWindow))
                args.context = Event.current.delta.x < -0.05f ? mouseOverWindow : null;
            else
                args.context = Event.current.delta.x > 0.05f ? mouseOverWindow : null;

            if (args.context != null)
                MaximizeKeyHandler(args);
        }

        [Shortcut("Window/Maximize View", KeyCode.Space, ShortcutModifiers.Shift)]
        [FormerlyPrefKeyAs("Window/Maximize View", "# ")]
        internal static void MaximizeKeyHandler(ShortcutArguments args)
        {
            if (args.context is PreviewWindow)
                return;

            var mouseOverWindow = EditorWindow.mouseOverWindow;

            if (mouseOverWindow != null)
            {
                if (IsMaximized(mouseOverWindow))
                    Unmaximize(mouseOverWindow);
                else
                    Maximize(mouseOverWindow);
            }
        }

        private static View FindRootSplitView(EditorWindow win)
        {
            View itor = win.m_Parent.parent;
            View rootSplit = itor;
            while (itor is SplitView)
            {
                rootSplit = itor;
                itor = itor.parent;
            }

            return rootSplit;
        }

        public static void AddSplitViewAndChildrenRecurse(View splitview, ArrayList list)
        {
            list.Add(splitview);
            DockArea dock = splitview as DockArea;
            if (dock != null)
            {
                list.AddRange(dock.m_Panes);
                list.Add(dock.actualView);
            }

            foreach (View child in splitview.children)
                AddSplitViewAndChildrenRecurse(child, list);
        }

        public static void SaveSplitViewAndChildren(View splitview, EditorWindow win, string path)
        {
            ArrayList all = new ArrayList();

            AddSplitViewAndChildrenRecurse(splitview, all);
            all.Remove(splitview);
            all.Remove(win);
            all.Insert(0, splitview);
            all.Insert(1, win);

            if (EnsureDirectoryCreated(path))
                InternalEditorUtility.SaveToSerializedFileAndForget(all.ToArray(typeof(UnityObject)) as UnityObject[], path, true);
        }

        static bool EnsureDirectoryCreated(string path)
        {
            var parentFolderPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentFolderPath))
                return false;

            if (!Directory.Exists(parentFolderPath))
                Directory.CreateDirectory(parentFolderPath);
            return true;
        }

        public static void Maximize(EditorWindow win)
        {
            bool maximizePending = MaximizePrepare(win);
            if (maximizePending)
                MaximizePresent(win);
        }

        public static bool MaximizePrepare(EditorWindow win)
        {
            View rootSplit = FindRootSplitView(win);
            //Some windows such as pop up windows might not have a split view
            if (rootSplit == null)
                return false;
            View itor = rootSplit.parent;

            // Make sure it has a dockarea
            DockArea dockArea = win.m_Parent as DockArea;
            if (dockArea == null)
                return false;

            if (itor == null)
                return false;

            var mainView = rootSplit.parent as MainView;
            if (mainView == null)
                return false;

            ContainerWindow parentWindow = win.m_Parent.window;
            if (parentWindow == null)
                return false;

            int oldDockIndex = dockArea.m_Panes.IndexOf(win);
            if (oldDockIndex == -1)
                return false;

            if (!parentWindow.CanCloseAllExcept(win))
                return false;

            dockArea.selected = oldDockIndex;

            // Save current state to disk
            SaveSplitViewAndChildren(rootSplit, win, Path.Combine(layoutsProjectPath, kMaximizeRestoreFile));

            // Remove the window from the HostView now in order to invoke OnBecameInvisible before OnBecameVisible
            dockArea.actualView = null;

            dockArea.m_Panes[oldDockIndex] = null;

            MaximizedHostView maximizedHostView = ScriptableObject.CreateInstance<MaximizedHostView>();

            int i = itor.IndexOfChild(rootSplit);
            Rect p = rootSplit.position;
            itor.RemoveChild(rootSplit);
            itor.AddChild(maximizedHostView, i);

            maximizedHostView.actualView = win;
            maximizedHostView.position = p; // Must be set after actualView so that value is propagated

            var gv = win as GameView;
            if (gv != null)
                maximizedHostView.EnableVSync(gv.vSyncEnabled);

            UnityObject.DestroyImmediate(rootSplit, true);
            return true;
        }

        public static void MaximizePresent(EditorWindow win)
        {
            ContainerWindow.SetFreezeDisplay(true);

            win.Focus();

            CheckWindowConsistency();

            ContainerWindow parentWindow = win.m_Parent.window;
            parentWindow.DisplayAllViews();

            win.m_Parent.MakeVistaDWMHappyDance();

            ContainerWindow.SetFreezeDisplay(false);

            win.OnMaximized();
        }

        static void DeleteWindowLayout(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!EditorUtility.DisplayDialog("Delete Layout", $"Delete window layout '{name}'?", "Delete", "Cancel"))
                return;

            DeleteWindowLayoutImpl(name, path);
        }

        [UsedImplicitly] // used by SaveLayoutTests.cs
        internal static void DeleteNamedWindowLayoutNoDialog(string name)
        {
            var path = Path.Combine(layoutsModePreferencesPath, name + ".wlt");
            DeleteWindowLayoutImpl(name, path);
        }

        static void DeleteWindowLayoutImpl(string name, string path)
        {
            if (Toolbar.lastLoadedLayoutName == name)
                Toolbar.lastLoadedLayoutName = null;

            File.Delete(path);
            ReloadWindowLayoutMenu();
            EditorUtility.Internal_UpdateAllMenus();
            ShortcutIntegration.instance.RebuildShortcuts();
        }

        // Attempts to load a layout. If unsuccessful, restores the previous layout.
        public static bool TryLoadWindowLayout(string path, bool newProjectLayoutWasCreated)
        {
            var flags = GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, true, false, true);
            return TryLoadWindowLayout(path, flags);
        }

        // Attempts to load a layout. If unsuccessful, restores the previous layout.
        public static bool TryLoadWindowLayout(string path, bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow, bool logErrorsToConsole)
        {
            var flags = GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
            return TryLoadWindowLayout(path, flags);
        }

        // Attempts to load a layout. If unsuccessful, restores the previous layout.
        public static bool TryLoadWindowLayout(string path, LoadWindowLayoutFlags flags)
        {
            if (LoadWindowLayout_Internal(path, flags))
                return true;
            LoadCurrentModeLayout(FindMainWindow());
            return false;
        }

        [Flags]
        public enum LoadWindowLayoutFlags
        {
            None = 0,
            NewProjectCreated = 1 << 0,
            SetLastLoadedLayoutName = 1 << 1,
            KeepMainWindow = 1 << 2,
            LogsErrorToConsole = 1 << 3,
            NoMainWindowSupport = 1 << 4,
            SaveLayoutPreferences = 1 << 5
        }

        // This method is public only because some packages have internal access and use it.
        [Obsolete("Do not use this method. Use TryLoadWindowLayout instead.")]
        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow, bool logErrorsToConsole)
        {
            return TryLoadWindowLayout(path, newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
        }

        // This method is public only because some packages have internal access and use it.
        [Obsolete("Do not use this method. Use TryLoadWindowLayout instead.")]
        public static bool LoadWindowLayout(string path, LoadWindowLayoutFlags flags)
        {
            return TryLoadWindowLayout(path, flags);
        }

        // IMPORTANT: Do not expose this method outside WindowLayout. Do not use this method as the entry point to load a layout.
        // Use TryLoadWindowLayout instead. It can put us in a state that is unrecoverable where all windows are gone but Unity
        // still lives as a background process. Only use this method from another method that handles the recovery process in case
        // of failure.
        static bool LoadWindowLayout_Internal(string path, bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow, bool logErrorsToConsole)
        {
            var flags = GetLoadWindowLayoutFlags(newProjectLayoutWasCreated, setLastLoadedLayoutName, keepMainWindow, logErrorsToConsole);
            return LoadWindowLayout_Internal(path, flags);
        }

        static LoadWindowLayoutFlags GetLoadWindowLayoutFlags(bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow, bool logErrorsToConsole)
        {
            var flags = LoadWindowLayoutFlags.SaveLayoutPreferences;
            if (newProjectLayoutWasCreated)
                flags |= LoadWindowLayoutFlags.NewProjectCreated;
            if (setLastLoadedLayoutName)
                flags |= LoadWindowLayoutFlags.SetLastLoadedLayoutName;
            if (keepMainWindow)
                flags |= LoadWindowLayoutFlags.KeepMainWindow;
            if (logErrorsToConsole)
                flags |= LoadWindowLayoutFlags.LogsErrorToConsole;
            return flags;
        }

        // IMPORTANT: Do not expose this method outside WindowLayout. Do not use this method as the entry point to load a layout.
        // Use TryLoadWindowLayout instead. It can put us in a state that is unrecoverable where all windows are gone but Unity
        // still lives as a background process. Only use this method from another method that handles the recovery process in case
        // of failure.
        static bool LoadWindowLayout_Internal(string path, LoadWindowLayoutFlags flags)
        {
            Console.WriteLine($"[LAYOUT] About to load {path}, keepMainWindow={flags.HasFlag(LoadWindowLayoutFlags.KeepMainWindow)}");

            if (!Application.isTestRun && !ContainerWindow.CanCloseAll(flags.HasFlag(LoadWindowLayoutFlags.KeepMainWindow)))
                return false;

            bool mainWindowMaximized = ContainerWindow.mainWindow?.maximized ?? false;
            Rect mainWindowPosition = ContainerWindow.mainWindow?.position ?? new Rect();

            // Load new windows and show them
            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                CloseWindows(flags.HasFlag(LoadWindowLayoutFlags.KeepMainWindow));

                ContainerWindow mainWindowToSetSize = null;
                ContainerWindow mainWindow = null;

                UnityObject[] remainingContainers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
                foreach (ContainerWindow window in remainingContainers)
                {
                    if (mainWindow == null && window.showMode == ShowMode.MainWindow)
                        mainWindow = window;
                    else
                        window.Close();
                }

                // Load data
                UnityObject[] loadedWindows = InternalEditorUtility.LoadSerializedFileAndForget(path);

                if (loadedWindows == null || loadedWindows.Length == 0)
                    throw new LayoutException("No windows found in layout.");

                List<UnityObject> newWindows = new List<UnityObject>();

                // At this point, unparented editor windows are neither desired nor desirable.
                // This can be caused by (legacy) serialization of FallbackEditorWindows or
                // other serialization hiccups (note that unparented editor windows should not exist in theory).
                // Same goes for empty DockAreas (no panes).  Leave them behind.
                for (int i = 0; i < loadedWindows.Length; i++)
                {
                    UnityObject o = loadedWindows[i];

                    if (o is EditorWindow editorWin)
                    {
                        if (!editorWin || !editorWin.m_Parent || !editorWin.m_Parent.window)
                        {
                            Console.WriteLine($"[LAYOUT] Removed un-parented EditorWindow while reading window layout" +
                                              $" window #{i}, type={o.GetType()} instanceID={o.GetInstanceID()}");
                            UnityObject.DestroyImmediate(editorWin, true);
                            continue;
                        }
                    }
                    else
                    {
                        if (o is ContainerWindow cw && cw.rootView == null)
                        {
                            cw.Close();
                            UnityObject.DestroyImmediate(cw, true);
                            continue;
                        }
                        else if (o is DockArea dockArea && dockArea.m_Panes.Count == 0)
                        {
                            dockArea.Close(null);
                            UnityObject.DestroyImmediate(dockArea, true);
                            continue;
                        }
                        else if (o is HostView hostview && hostview.actualView == null)
                        {
                            UnityObject.DestroyImmediate(hostview, true);
                            continue;
                        }
                    }

                    newWindows.Add(o);
                }

                for (int i = 0; i < newWindows.Count; i++)
                {
                    ContainerWindow cur = newWindows[i] as ContainerWindow;
                    if (cur != null && cur.showMode == ShowMode.MainWindow)
                    {
                        if (mainWindow == null)
                        {
                            mainWindow = cur;
                        }
                        else
                        {
                            mainWindow.rootView = cur.rootView;
                            UnityObject.DestroyImmediate(cur, true);
                            cur = mainWindow;
                            newWindows[i] = null;
                        }

                        if (mainWindowPosition.width != 0.0)
                        {
                            mainWindowToSetSize = cur;
                            // This is the same reference as the mainwindow, so need to freeze it too on for Linux during reload.
                            mainWindowToSetSize.SetFreeze(true);
                            mainWindowToSetSize.position = mainWindowPosition;
                        }

                        break;
                    }
                }

                for (int i = 0; i < newWindows.Count; i++)
                {
                    UnityObject o = newWindows[i];
                    if (!o)
                        continue;

                    if (flags.HasFlag(LoadWindowLayoutFlags.NewProjectCreated))
                    {
                        MethodInfo method = o.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        method?.Invoke(o, null);
                    }
                }

                if (mainWindowToSetSize)
                {
                    mainWindowToSetSize.SetFreeze(true);
                    mainWindowToSetSize.position = mainWindowPosition;
                }
                // Always show main window before other windows. So that other windows can
                // get their parent/owner.
                if (mainWindow)
                {
                    // If at this point the mainWindow still doesn't have a rootView, it probably means we loaded a layout without a main window.
                    // Delete the current mainWindow so that we can load back the previous layout otherwise every attemp at loading a layout will fail.
                    if (mainWindow.rootView == null || !mainWindow.rootView)
                    {
                        mainWindow.Close();
                        UnityObject.DestroyImmediate(mainWindow, true);
                        throw new LayoutException("Error while reading window layout: no root view on main window.");
                    }

                    // Don't adjust height to prevent main window shrink during layout on Linux.
                    mainWindow.SetFreeze(true);
                    mainWindow.Show(mainWindow.showMode, loadPosition: true, displayImmediately: true, setFocus: true);
                    if (mainWindowToSetSize && mainWindow.maximized != mainWindowMaximized)
                        mainWindow.ToggleMaximize();
                    // Unfreeze to make sure resize work properly.
                    mainWindow.SetFreeze(false);

                    // Make sure to restore the save to layout flag when loading a layout from a file.
                    if (flags.HasFlag(LoadWindowLayoutFlags.KeepMainWindow))
                        mainWindow.m_DontSaveToLayout = false;
                }
                else if (!flags.HasFlag(LoadWindowLayoutFlags.NoMainWindowSupport))
                {
                    throw new LayoutException("Error while reading window layout: no main window found");
                }

                // Show other windows
                for (int i = 0; i < newWindows.Count; i++)
                {
                    if (newWindows[i] == null)
                        continue;

                    EditorWindow win = newWindows[i] as EditorWindow;
                    if (win)
                        win.minSize = win.minSize; // Causes minSize to be propagated upwards to parents!

                    ContainerWindow containerWindow = newWindows[i] as ContainerWindow;
                    if (containerWindow && containerWindow != mainWindow)
                    {
                        containerWindow.Show(containerWindow.showMode, loadPosition: false, displayImmediately: true, setFocus: true);
                        if (flags.HasFlag(LoadWindowLayoutFlags.NoMainWindowSupport))
                        {
                            containerWindow.m_DontSaveToLayout = mainWindow != null;
                        }
                    }
                }

                // Un-maximize maximized PlayModeView window if maximize on play is enabled
                PlayModeView playModeView = GetMaximizedWindow() as PlayModeView;
                if (playModeView != null && playModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
                    Unmaximize(playModeView);
            }
            catch (Exception ex)
            {
                // When loading a new project we don't want to log an error if one of the last saved layouts throws.
                // There isn't anything useful a user can do about it, and we can gracefully recover. However when a
                // layout is loaded from the menu or a mode change, we do want to let the user know about layout loading
                // problems because they can act on it by either deleting the layout or importing whatever asset is
                // missing.
                var error = $"Failed to load window layout \"{path}\": {ex}";
                if(flags.HasFlag(LoadWindowLayoutFlags.LogsErrorToConsole))
                    Debug.LogError(error);
                else
                    Console.WriteLine($"[LAYOUT] {error}");
                return false;
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);

                if (flags.HasFlag(LoadWindowLayoutFlags.SetLastLoadedLayoutName) && Path.GetExtension(path) == ".wlt")
                    Toolbar.lastLoadedLayoutName = Path.GetFileNameWithoutExtension(path);
                else
                    Toolbar.lastLoadedLayoutName = null;
            }

            if (flags.HasFlag(LoadWindowLayoutFlags.SaveLayoutPreferences))
                SaveDefaultWindowPreferences();

            return true;
        }

        internal static void LoadDefaultLayout()
        {
            InitializeLayoutPreferencesFolder();

            FileUtil.DeleteFileOrDirectory(ProjectLayoutPath);

            if (EnsureDirectoryCreated(ProjectLayoutPath))
            {
                Console.WriteLine($"[LAYOUT] LoadDefaultLayout: Copying Project Current Layout: {ProjectLayoutPath} from {GetDefaultLayoutPath()}");
                FileUtil.CopyFileOrDirectory(GetDefaultLayoutPath(), ProjectLayoutPath);
            }
            Debug.Assert(File.Exists(ProjectLayoutPath));

            LoadWindowLayout_Internal(ProjectLayoutPath, true, true, false, false);
        }

        public static void CloseWindows()
        {
            CloseWindows(false);
        }

        private static void CloseWindows(bool keepMainWindow)
        {
            try
            {
                // ForceClose any existing tooltips
                TooltipView.ForceClose();
            }
            catch (Exception)
            {
                // ignored
            }

            // ForceClose all container windows
            ContainerWindow mainWindow = null;
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                try
                {
                    if (window.showMode != ShowMode.MainWindow || !keepMainWindow || mainWindow != null)
                    {
                        window.Close();
                        UnityObject.DestroyImmediate(window, true);
                    }
                    else
                    {
                        UnityObject.DestroyImmediate(window.rootView, true);
                        window.rootView = null;
                        mainWindow = window;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            // Double check correct closing
            UnityObject[] oldWindows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            if (oldWindows.Length != 0)
            {
                string output = "";
                foreach (EditorWindow killme in oldWindows)
                {
                    output += $"{killme.GetType().Name} {killme.name} {killme.titleContent.text} [{killme.GetInstanceID()}]\r\n";
                    UnityObject.DestroyImmediate(killme, true);
                }
            }

            UnityObject[] oldViews = Resources.FindObjectsOfTypeAll(typeof(View));
            if (oldViews.Length != 0)
            {
                foreach (View killme in oldViews)
                    UnityObject.DestroyImmediate(killme, true);
            }
        }

        internal static void SaveWindowLayout(string path)
        {
            SaveWindowLayout(path, true);
        }

        public static void SaveWindowLayout(string path, bool reportErrors)
        {
            if (!EnsureDirectoryCreated(path))
                return;

            Console.WriteLine($"[LAYOUT] About to save layout {path}");
            TooltipView.ForceClose();

            UnityObject[] windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            UnityObject[] containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            UnityObject[] views = Resources.FindObjectsOfTypeAll(typeof(View));

            var all = new List<UnityObject>();
            var ignoredViews = new List<ScriptableObject>();
            foreach (ContainerWindow w in containers)
            {
                // skip ContainerWindows that are "dont save me"
                if (!w || w.m_DontSaveToLayout)
                    ignoredViews.Add(w);
                else
                    all.Add(w);
            }

            foreach (View w in views)
            {
                // skip Views that belong to "dont save me" container
                if (!w || !w.window || ignoredViews.Contains(w.window))
                    ignoredViews.Add(w);
                else
                    all.Add(w);
            }

            foreach (EditorWindow w in windows)
            {
                // skip EditorWindows that belong to "dont save me" container
                if (!w || !w.m_Parent || ignoredViews.Contains(w.m_Parent))
                {
                    if (reportErrors && w)
                        Debug.LogWarning($"Cannot save invalid window {w.titleContent.text} {w} to layout.");
                    continue;
                }
                all.Add(w);
            }

            if (all.Any())
                InternalEditorUtility.SaveToSerializedFileAndForget(all.Where(o => o).ToArray(), path, true);
        }

        // ReSharper disable once MemberCanBePrivate.Global - used by SaveLayoutTests.cs
        internal static void SaveGUI()
        {
            UnityEditor.SaveWindowLayout.ShowWindow();
        }

        public static void LoadFromFile()
        {
            var layoutFilePath = EditorUtility.OpenFilePanelWithFilters("Load layout from disk...", "", new[] {"Layout", "wlt"});
            if (string.IsNullOrEmpty(layoutFilePath))
                return;
            TryLoadWindowLayout(layoutFilePath, false);
        }

        public static void SaveToFile()
        {
            var layoutFilePath = EditorUtility.SaveFilePanel("Save layout to disk...", "", "layout", "wlt");
            if (String.IsNullOrEmpty(layoutFilePath))
                return;

            SaveWindowLayout(layoutFilePath);
            EditorUtility.RevealInFinder(layoutFilePath);
        }

        private static void ResetUserLayouts()
        {
            // Copy installation layouts to user global layouts and overwrite any existing ones.
            var layoutPaths = Directory.GetFiles(layoutResourcesPath, "*.wlt");
            foreach (var installationLayoutPath in layoutPaths)
            {
                var layoutFilename = Path.GetFileName(installationLayoutPath);
                var userLayoutDstPath = FileUtil.CombinePaths(layoutsDefaultModePreferencesPath, layoutFilename);
                FileUtil.CopyFileIfExists(installationLayoutPath, userLayoutDstPath, overwrite: true);
            }

            // delete per-project layouts
            if (Directory.Exists(layoutsProjectPath))
                Directory.Delete(layoutsProjectPath, true);

            // delete per-user layouts
            if (Directory.Exists(layoutsCurrentModePreferencesPath))
                Directory.Delete(layoutsCurrentModePreferencesPath, true);
        }


        public static void ResetAllLayouts(bool quitOnCancel = true)
        {
            if (!Application.isTestRun && !EditorUtility.DisplayDialog("Revert All Window Layouts",
                "Unity is about to delete all window layouts and restore them to the default settings.",
                "Continue", quitOnCancel ? "Quit" : "Cancel"))
            {
                if (quitOnCancel)
                    EditorApplication.Exit(0);
                return;
            }

            if (!ContainerWindow.CanCloseAll(false))
                return;

            ResetFactorySettings();
        }

        public static void ResetFactorySettings()
        {
            // Reset user layouts
            ResetUserLayouts();

            // Reset mode settings
            ModeService.ChangeModeById("default");

            LoadCurrentModeLayout(keepMainWindow: false);
            ReloadWindowLayoutMenu();
            EditorUtility.Internal_UpdateAllMenus();
            ShortcutIntegration.instance.RebuildShortcuts();
        }
    }

    [EditorWindowTitle(title = "Save Layout")]
    internal class SaveWindowLayout : EditorWindow
    {
        bool m_DidFocus;
        const int k_Width = 200;
        const int k_Height = 48;
        const int k_HelpBoxHeight = 40;
        const int k_MaxLayoutNameLength = 128;

        static readonly string k_InvalidChars = EditorUtility.GetInvalidFilenameChars();
        static readonly string s_InvalidCharsFormatString = L10n.Tr("Invalid characters: {0}");
        string m_CurrentInvalidChars = "";
        string m_LayoutName = Toolbar.lastLoadedLayoutName;

        internal static SaveWindowLayout ShowWindow()
        {
            SaveWindowLayout w = GetWindowDontShow<SaveWindowLayout>();
            w.minSize = w.maxSize = new Vector2(k_Width, k_Height);
            w.m_Pos = new Rect(0, 0,k_Width, k_Height);
            w.ShowAuxWindow();
            return w;
        }

        void UpdateCurrentInvalidChars()
        {
            m_CurrentInvalidChars = new string(m_LayoutName.Intersect(k_InvalidChars).Distinct().ToArray());
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            Event evt = Event.current;
            bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
            bool hitEscape = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Escape);
            if (hitEscape)
            {
                Close();
                GUIUtility.ExitGUI();
            }
            GUI.SetNextControlName("m_PreferencesName");
            EditorGUI.BeginChangeCheck();
            m_LayoutName = EditorGUILayout.TextField(m_LayoutName);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_LayoutName.Length > k_MaxLayoutNameLength)
                {
                    m_LayoutName = m_LayoutName.Substring(0, k_MaxLayoutNameLength);
                }
                m_LayoutName = m_LayoutName.TrimEnd();
                UpdateCurrentInvalidChars();
            }

            if (!m_DidFocus)
            {
                m_DidFocus = true;
                EditorGUI.FocusTextInControl("m_PreferencesName");
            }

            if (m_CurrentInvalidChars.Length != 0)
            {
                EditorGUILayout.HelpBox(string.Format(s_InvalidCharsFormatString, m_CurrentInvalidChars), MessageType.Warning);
                minSize = new Vector2(k_Width, k_Height + k_HelpBoxHeight);
            }
            else
            {
                minSize = new Vector2(k_Width, k_Height);
            }

            bool canSaveLayout = m_LayoutName.Length > 0 && m_CurrentInvalidChars.Length == 0;
            EditorGUI.BeginDisabled(!canSaveLayout);

            if (GUILayout.Button("Save") || hitEnter && canSaveLayout)
            {
                Close();

                if (!Directory.Exists(WindowLayout.layoutsModePreferencesPath))
                    Directory.CreateDirectory(WindowLayout.layoutsModePreferencesPath);

                string path = Path.Combine(WindowLayout.layoutsModePreferencesPath, m_LayoutName + ".wlt");
                if (File.Exists(path))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite layout?",
                        "Do you want to overwrite '" + m_LayoutName + "' layout?",
                        "Overwrite", "Cancel"))
                        GUIUtility.ExitGUI();
                }

                Toolbar.lastLoadedLayoutName = m_LayoutName;
                WindowLayout.SaveWindowLayout(path);
                WindowLayout.ReloadWindowLayoutMenu();
                EditorUtility.Internal_UpdateAllMenus();
                ShortcutIntegration.instance.RebuildShortcuts();
                GUIUtility.ExitGUI();
            }
            else
            {
                m_DidFocus = false;
            }

            EditorGUI.EndDisabled();
        }
    }

    internal static class CreateBuiltinWindows
    {
        [MenuItem("Window/General/Scene %1", false, 1)]
        static void ShowSceneView()
        {
            EditorWindow.GetWindowWithExactType<SceneView>();
        }

        [MenuItem("Window/General/Game %2", false, 2)]
        static void ShowGameView()
        {
            EditorWindow.GetWindow<GameView>();
        }

        [MenuItem("Window/General/Inspector %3", false, 3)]
        static void ShowInspector()
        {
            EditorWindow.GetWindow<InspectorWindow>();
        }

        [MenuItem("Window/General/Hierarchy %4", false, 4)]
        static void ShowNewHierarchy()
        {
            EditorWindow.GetWindow<SceneHierarchyWindow>();
        }

        [MenuItem("Window/General/Project %5", false, 5)]
        static void ShowProject()
        {
            EditorWindow.GetWindow<ProjectBrowser>();
        }

        [MenuItem("Window/Animation/Animation %6", false, 1)]
        static void ShowAnimationWindow()
        {
            EditorWindow.GetWindow<AnimationWindow>();
        }

        [MenuItem("Window/Audio/Audio Random Container", false, 1)]
        static void ShowAudioRandomContainerWindow()
        {
            AudioContainerWindow.CreateAudioRandomContainerWindow();
        }

        [MenuItem("Window/Audio/Audio Mixer %8", false, 2)]
        static void ShowAudioMixer()
        {
            AudioMixerWindow.CreateAudioMixerWindow();
        }

        // Version Control is registered from native code (EditorWindowController.cpp), for license check
        [RequiredByNativeCode]
        static void ShowVersionControl()
        {
            EditorWindow.GetWindow<WindowPending>();
        }

        [MenuItem("Window/General/Console %#c", false, 6)]
        static void ShowConsole()
        {
            EditorWindow.GetWindow<ConsoleWindow>();
        }
    }

    internal class WindowFocusState : ScriptableObject
    {
        private static WindowFocusState m_Instance;

        internal string m_LastWindowTypeInSameDock = "";
        internal bool m_WasMaximizedBeforePlay = false;
        internal bool m_CurrentlyInPlayMode = false;

        internal static WindowFocusState instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindFirstObjectByType(typeof(WindowFocusState)) as WindowFocusState;
                if (m_Instance == null)
                    m_Instance = CreateInstance<WindowFocusState>();
                return m_Instance;
            }
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            m_Instance = this;
        }
    }
} // namespace
