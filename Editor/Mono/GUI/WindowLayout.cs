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

        private const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";
        private const string kDefaultLayoutName = "Default.wlt";
        internal static string layoutsPreferencesPath => FileUtil.CombinePaths(InternalEditorUtility.unityPreferencesFolder, "Layouts", Application.unityVersionVer.ToString());
        internal static string layoutsModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, ModeService.currentId);
        internal static string layoutsDefaultModePreferencesPath => FileUtil.CombinePaths(layoutsPreferencesPath, "default");
        internal static string layoutsProjectPath => FileUtil.CombinePaths("Library", "Layouts", Application.unityVersionVer.ToString());
        internal static string ProjectLayoutPath => GetProjectLayoutPerMode(ModeService.currentId);

        [UsedImplicitly, RequiredByNativeCode]
        public static void LoadDefaultWindowPreferences()
        {
            LoadCurrentModeLayout(keepMainWindow: FindMainWindow());
            ModeService.InitializeCurrentMode();
        }

        public static void LoadCurrentModeLayout(bool keepMainWindow)
        {
            InitializeLayoutPreferencesFolder();

            var layoutData = ModeService.GetModeDataSection(ModeDescriptor.LayoutKey) as JSONObject;
            if (layoutData == null)
                LoadProjectLayout(keepMainWindow);
            else
            {
                var projectLayoutExists = File.Exists(ProjectLayoutPath);
                if ((projectLayoutExists && Convert.ToBoolean(layoutData["restore_saved_layout"]))
                    || !LoadModeDynamicLayout(keepMainWindow, layoutData))
                    LoadProjectLayout(keepMainWindow);
            }
        }

        private static bool LoadModeDynamicLayout(bool keepMainWindow, JSONObject layoutData)
        {
            const string k_TopViewClassName = "top_view";
            const string k_CenterViewClassName = "center_view";
            const string k_BottomViewClassName = "bottom_view";

            LayoutViewInfo topViewInfo = new LayoutViewInfo(k_TopViewClassName, MainView.kToolbarHeight, true);
            LayoutViewInfo bottomViewInfo = new LayoutViewInfo(k_BottomViewClassName, MainView.kStatusbarHeight, true);
            LayoutViewInfo centerViewInfo = new LayoutViewInfo(k_CenterViewClassName, 0, true);

            var availableEditorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>().ToArray();

            if (!GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref centerViewInfo))
                return false;

            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref topViewInfo);
            GetLayoutViewInfo(layoutData, availableEditorWindowTypes, ref bottomViewInfo);

            var mainWindow = GenerateLayout(keepMainWindow, availableEditorWindowTypes, centerViewInfo, topViewInfo, bottomViewInfo, layoutData);
            if (mainWindow)
                mainWindow.m_DontSaveToLayout = !Convert.ToBoolean(layoutData["restore_saved_layout"]);

            return mainWindow != null;
        }

        private static View LoadLayoutView<T>(Type[] availableEditorWindowTypes, LayoutViewInfo viewInfo, float width, float height) where T : View
        {
            if (!viewInfo.used)
                return null;
            View view = null;
            if (viewInfo.isContainer)
            {
                bool useTabs = viewInfo.extendedData.Contains("tabs") && Convert.ToBoolean(viewInfo.extendedData["tabs"]);
                bool useSplitter = viewInfo.extendedData.Contains("vertical") || viewInfo.extendedData.Contains("horizontal");
                bool isVertical = viewInfo.extendedData.Contains("vertical") && Convert.ToBoolean(viewInfo.extendedData["vertical"]);

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
                    if (useTabs)
                    {
                        (view as DockArea).AddTab((EditorWindow)ScriptableObject.CreateInstance(lvi.type));
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

        internal static ContainerWindow FindMainWindow()
        {
            var containers = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
            foreach (ContainerWindow window in containers)
            {
                if (window.showMode == ShowMode.MainWindow)
                    return window;
            }

            return null;
        }

        private static ContainerWindow GenerateLayout(bool keepMainWindow, Type[] availableEditorWindowTypes, LayoutViewInfo center, LayoutViewInfo top, LayoutViewInfo bottom, JSONObject layoutData)
        {
            ContainerWindow mainContainerWindow = FindMainWindow();
            if (keepMainWindow && mainContainerWindow == null)
            {
                Debug.LogWarning($"No main window to restore layout from while loading dynamic layout for mode {ModeService.currentId}");
                return null;
            }

            try
            {
                ContainerWindow.SetFreezeDisplay(true);
                if (!mainContainerWindow)
                {
                    mainContainerWindow = ScriptableObject.CreateInstance<ContainerWindow>();
                    var mainWindowMinSize = new Vector2(120, 80);
                    var mainWindowMaxSize = new Vector2(8192, 8192);
                    if (layoutData.Contains("min_width"))
                    {
                        mainWindowMinSize.x = Convert.ToSingle(layoutData["min_width"]);
                    }
                    if (layoutData.Contains("min_height"))
                    {
                        mainWindowMinSize.y = Convert.ToSingle(layoutData["min_height"]);
                    }
                    if (layoutData.Contains("max_width"))
                    {
                        mainWindowMaxSize.x = Convert.ToSingle(layoutData["max_width"]);
                    }
                    if (layoutData.Contains("max_height"))
                    {
                        mainWindowMaxSize.y = Convert.ToSingle(layoutData["max_height"]);
                    }
                    mainContainerWindow.SetMinMaxSizes(mainWindowMinSize, mainWindowMaxSize);
                }

                mainContainerWindow.windowID = $"MainView_{ModeService.currentId}";
                mainContainerWindow.LoadGeometry(true);

                var width = mainContainerWindow.position.width;
                var height = mainContainerWindow.position.height;

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

                if (mainContainerWindow.rootView)
                    ScriptableObject.DestroyImmediate(mainContainerWindow.rootView, true);

                mainContainerWindow.rootView = main;
                mainContainerWindow.rootView.position = new Rect(0, 0, width, height);
                mainContainerWindow.Show(ShowMode.MainWindow, true, true, true);
                mainContainerWindow.DisplayAllViews();
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);
            }

            return mainContainerWindow;
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
                viewInfo.used = !String.IsNullOrEmpty(viewInfo.className);
                if (!viewInfo.used)
                    return true;
            }
            else if (viewData is IDictionary)
            {
                var viewExpandedData = viewData as IDictionary;

                if (viewExpandedData.Contains("children") || viewExpandedData.Contains("vertical") || viewExpandedData.Contains("horizontal") || viewExpandedData.Contains("tabs"))
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

        private static void LoadProjectLayout(bool keepMainWindow)
        {
            var projectLayoutExists = File.Exists(ProjectLayoutPath);
            if (!projectLayoutExists)
            {
                var currentLayoutPath = GetCurrentLayoutPath();
                if (EnsureDirectoryCreated(ProjectLayoutPath))
                    FileUtil.CopyFileOrDirectory(currentLayoutPath, ProjectLayoutPath);
            }

            Debug.Assert(File.Exists(ProjectLayoutPath));

            // Load the current project layout
            LoadWindowLayout(ProjectLayoutPath, !projectLayoutExists, false, keepMainWindow);
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
            // Save Project Current Layout
            SaveWindowLayout(FileUtil.CombinePaths(Directory.GetCurrentDirectory(), GetProjectLayoutPerMode(modeId)));
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
            return FileUtil.CombinePaths(layoutsProjectPath, $"CurrentLayout-{modeId}.dwlt");
        }

        private static void InitializeLayoutPreferencesFolder()
        {
            string defaultLayoutPath = GetDefaultLayoutPath();
            string layoutResourcesPath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Layouts");

            if (!Directory.Exists(layoutsPreferencesPath))
                Directory.CreateDirectory(layoutsPreferencesPath);

            if (!Directory.Exists(layoutsModePreferencesPath))
            {
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

            int layoutMenuItemPriority = 20;

            // Get user saved layouts
            string[] layoutPaths = new string[0];
            if (Directory.Exists(layoutsModePreferencesPath))
            {
                layoutPaths = Directory.GetFiles(layoutsModePreferencesPath).Where(path => path.EndsWith(".wlt")).ToArray();
                foreach (var layoutPath in layoutPaths)
                {
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", false, layoutMenuItemPriority++, () => LoadWindowLayout(layoutPath, false, true, true), null);
                }

                layoutMenuItemPriority += 500;
            }

            // Get mode layouts
            var modeLayoutPaths = ModeService.GetModeDataSection(ModeService.currentIndex, ModeDescriptor.LayoutsKey) as IList<object>;
            if (modeLayoutPaths != null)
            {
                foreach (var layoutPath in modeLayoutPaths.Cast<string>())
                {
                    if (!File.Exists(layoutPath))
                        continue;
                    var name = Path.GetFileNameWithoutExtension(layoutPath);
                    Menu.AddMenuItem("Window/Layouts/" + name, "", Toolbar.lastLoadedLayoutName == name, layoutMenuItemPriority++, () => LoadWindowLayout(layoutPath, false), null);
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
                    Debug.LogError("Invalid editor window " + win.GetType());
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

            UnityObject[] newWindows = InternalEditorUtility.LoadSerializedFileAndForget(Path.Combine(layoutsProjectPath, kMaximizeRestoreFile));

            if (newWindows.Length < 2)
            {
                Debug.Log("Maximized serialized file backup not found");
                ResetAllLayouts();
                return;
            }

            SplitView oldRoot = newWindows[0] as SplitView;
            EditorWindow oldWindow = newWindows[1] as EditorWindow;

            if (oldRoot == null)
            {
                Debug.Log("Maximization failed because the root split view was not found");
                ResetAllLayouts();
                return;
            }

            ContainerWindow parentWindow = win.m_Parent.window;
            if (parentWindow == null)
            {
                Debug.Log("Maximization failed because the root split view has no container window");
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
            // Find Root SplitView
            View itor = win.m_Parent.parent;
            View rootSplit = itor;
            while (itor != null && itor is SplitView)
            {
                rootSplit = itor;
                itor = itor.parent;
            }

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

            if (!parentWindow.InternalRequestCloseAllExcept(win))
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

        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated)
        {
            return LoadWindowLayout(path, newProjectLayoutWasCreated, true, false);
        }

        public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated, bool setLastLoadedLayoutName, bool keepMainWindow)
        {
            Console.WriteLine($"[LAYOUT] About to load {path}, keepMainWindow={keepMainWindow}");

            if (!Application.isTestRun && Application.isHumanControllingUs && !ContainerWindow.InternalRequestCloseAll(keepMainWindow))
                return false;

            bool mainWindowMaximized = ContainerWindow.mainWindow?.maximized ?? false;
            Rect mainWindowPosition = ContainerWindow.mainWindow?.position ?? new Rect();

            bool layoutLoadingIssue = false;

            // Load new windows and show them
            try
            {
                ContainerWindow.SetFreezeDisplay(true);

                CloseWindows(keepMainWindow);

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
                    throw new LayoutException($"Window layout at {path} could not be loaded.");

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
                            Console.WriteLine("[LAYOUT] Removed unparented EditorWindow while reading window layout: window #" + i + ", type=" +
                                o.GetType() + ", instanceID=" + o.GetInstanceID());
                            UnityObject.DestroyImmediate(editorWin, true);
                            layoutLoadingIssue = true;
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

                    if (newProjectLayoutWasCreated)
                    {
                        MethodInfo method = o.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        method?.Invoke(o, null);
                    }
                }

                if (mainWindowToSetSize)
                    mainWindowToSetSize.position = mainWindowPosition;

                // Always show main window before other windows. So that other windows can
                // get their parent/owner.
                if (!mainWindow)
                    throw new LayoutException("Error while reading window layout: no main window found");

                mainWindow.Show(mainWindow.showMode, loadPosition: true, displayImmediately: true, setFocus: true);
                if (mainWindowToSetSize && mainWindow.maximized != mainWindowMaximized)
                    mainWindow.ToggleMaximize();

                // Make sure to restore the save to layout flag when loading a layout from a file.
                if (keepMainWindow)
                    mainWindow.m_DontSaveToLayout = false;

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
                        containerWindow.Show(containerWindow.showMode, loadPosition: false, displayImmediately: true, setFocus: true);
                }

                // Unmaximize maximized PlayModeView window if maximize on play is enabled
                PlayModeView playModeView = GetMaximizedWindow() as PlayModeView;
                if (playModeView != null && playModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
                    Unmaximize(playModeView);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load window layout: " + ex);

                int option = 0;
                if (!Application.isTestRun && Application.isHumanControllingUs)
                {
                    option = EditorUtility.DisplayDialogComplex("Failed to load window layout",
                        $"This can happen if layout contains custom windows and there are compile errors in the project.\r\n\r\n{ex.Message}",
                        "Load Default Layout", "Quit", "Revert Factory Settings");
                }
                else
                {
                    ResetUserLayouts();
                }

                switch (option)
                {
                    case 0:
                        LoadDefaultLayout();
                        break;
                    case 1:
                        EditorApplication.Exit(0);
                        break;
                    case 2:
                        ResetFactorySettings();
                        break;
                }

                return false;
            }
            finally
            {
                ContainerWindow.SetFreezeDisplay(false);

                if (setLastLoadedLayoutName && Path.GetExtension(path) == ".wlt")
                    Toolbar.lastLoadedLayoutName = Path.GetFileNameWithoutExtension(path);
                else
                    Toolbar.lastLoadedLayoutName = null;
            }

            if (layoutLoadingIssue)
                Debug.Log("The editor layout could not be fully loaded, this can happen when the layout contains EditorWindows not available in this project");

            return true;
        }

        internal static void LoadDefaultLayout()
        {
            InitializeLayoutPreferencesFolder();

            FileUtil.DeleteFileOrDirectory(ProjectLayoutPath);

            if (EnsureDirectoryCreated(ProjectLayoutPath))
                FileUtil.CopyFileOrDirectory(GetDefaultLayoutPath(), ProjectLayoutPath);
            Debug.Assert(File.Exists(ProjectLayoutPath));

            LoadWindowLayout(ProjectLayoutPath, true);
        }

        public static void CloseWindows()
        {
            CloseWindows(false);
        }

        private static void CloseWindows(bool keepMainWindow)
        {
            try
            {
                // Close any existing tooltips
                TooltipView.Close();
            }
            catch (Exception)
            {
                // ignored
            }

            // Close all container windows
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
                Debug.LogWarning($"Failed to destroy editor windows: #{oldWindows.Length}\r\n{output}");
            }

            UnityObject[] oldViews = Resources.FindObjectsOfTypeAll(typeof(View));
            if (oldViews.Length != 0)
            {
                foreach (View killme in oldViews)
                    UnityObject.DestroyImmediate(killme, true);
            }
        }

        public static void SaveWindowLayout(string path)
        {
            if (!EnsureDirectoryCreated(path))
                return;

            Console.WriteLine($"[LAYOUT] About to save layout {path}");
            TooltipView.Close();

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
                    if (w)
                        Debug.LogWarning($"Cannot save invalid window {w.titleContent.text} {w} to layout.");
                    continue;
                }
                all.Add(w);
            }

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
            if (String.IsNullOrEmpty(layoutFilePath))
                return;

            if (LoadWindowLayout(layoutFilePath, false))
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Loaded layout from " + layoutFilePath);
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
            FileUtil.DeleteFileOrDirectory(layoutsPreferencesPath);

            if (Directory.Exists(layoutsProjectPath))
                Directory.Delete(layoutsProjectPath, true);
        }

        public static void ResetAllLayouts(bool quitOnCancel = true)
        {
            if (!EditorUtility.DisplayDialog("Revert All Window Layouts",
                "Unity is about to delete all window layouts and restore them to the default settings.",
                "Continue", quitOnCancel ? "Quit" : "Cancel"))
            {
                if (quitOnCancel)
                    EditorApplication.Exit(0);
                return;
            }

            ResetFactorySettings();
        }

        public static void ResetFactorySettings()
        {
            // Reset user layouts
            ResetUserLayouts();

            // Reset mode settings
            ModeService.ChangeModeById("default");

            LoadCurrentModeLayout(false);
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

        static readonly string k_InvalidChars = EditorUtility.GetInvalidFilenameChars();
        static readonly string s_InvalidCharsFormatString = L10n.Tr("Invalid characters: {0}");
        string m_CurrentInvalidChars = "";
        string m_LayoutName = Toolbar.lastLoadedLayoutName;

        internal static SaveWindowLayout ShowWindow()
        {
            SaveWindowLayout w = GetWindowDontShow<SaveWindowLayout>();
            w.minSize = w.maxSize = new Vector2(k_Width, k_Height);
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
            m_LayoutName = m_LayoutName.TrimEnd();
            if (EditorGUI.EndChangeCheck())
            {
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
            EditorWindow.GetWindow<SceneView>();
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

        [MenuItem("Window/Audio/Audio Mixer %8", false, 1)]
        static void ShowAudioMixer()
        {
            AudioMixerWindow.CreateAudioMixerWindow();
        }

        // Version Control is registered from native code (EditorWindowController.cpp), for license check
        // [MenuItem ("Window/Version Control", false, 2010)]
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
                    m_Instance = FindObjectOfType(typeof(WindowFocusState)) as WindowFocusState;
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
